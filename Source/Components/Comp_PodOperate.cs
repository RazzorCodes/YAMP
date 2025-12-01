using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace YAMP
{
    public class CompProp_PodOperate : CompProperties
    {
        public float surgerySuccessChance = 0.98f; // High success chance
        public float stockConsumption = 15f; // Multiplier for stock consumption

        public CompProp_PodOperate()
        {
            compClass = typeof(Comp_PodOperate);
        }
    }

    public class Comp_PodOperate : ThingComp
    {
        public CompProp_PodOperate Props => (CompProp_PodOperate)props;

        private OperationalStock _operationalStock;
        private OperationalStock OperationalStock => _operationalStock ??= parent.GetComp<OperationalStock>();

        private Comp_PodContainer _podConatiner;
        private Comp_PodContainer PodConatiner => _podConatiner ??= parent.GetComp<Comp_PodContainer>();

        public override void CompTick()
        {
            if (PodConatiner.GetPawn() == null)
            {
                Logger.Log("[Operate]", "No patient found in pod");
                return;
            }

            if (parent.IsHashIntervalTick(250))
            {
                TryPerformOperation();
            }
        }

        private bool CheckParts(Bill_Medical bill)
        {
            foreach (var part in bill.recipe.ingredients)
            {
                if (part.filter.Allows(ThingDefOf.MedicineHerbal))
                {
                    continue;
                }

                if (part.GetBaseCount() > PodConatiner.Get().Where(t => part.filter.Allows(t)).Sum(t => t.stackCount))
                {
                    Logger.Log("[Operate]", $"YAMP: Missing {part.filter.Summary} for {bill.recipe.label}");
                    return false;
                }
            }

            return true;
        }

        List<Thing> ReserveParts(Bill_Medical bill)
        {
            List<Thing> ingredients = new List<Thing>();
            foreach (IngredientCount ing in bill.recipe.ingredients)
            {
                if (ing.filter.Allows(ThingDefOf.MedicineHerbal))
                {
                    continue;
                }

                int stillNeeded = (int)ing.GetBaseCount();
                var candidates = PodConatiner.Get()
                    .Where(t => ing.filter.Allows(t))
                    .OrderBy(t => t.MarketValue)
                    .ToList();

                foreach (var candidate in candidates)
                {
                    if (candidate.stackCount >= stillNeeded)
                    {
                        ingredients.Add(candidate.SplitOff(stillNeeded));
                        stillNeeded = 0;
                        break;
                    }
                    else
                    {
                        ingredients.Add(candidate);
                        stillNeeded -= candidate.stackCount;
                    }
                }

                if (stillNeeded > 0)
                {
                    Logger.Log("[Operate]", $"YAMP: Missing {ing.filter.Summary} for {bill.recipe.label}");
                    return null;
                }
            }

            return ingredients;
        }

        public void TryPerformOperation()
        {
            Pawn patient = PodConatiner.Get().OfType<Pawn>().FirstOrDefault();
            if (patient == null)
            {
                Logger.Log("[Operate]", "No patient found in pod");
                return;
            }

            // Get the first medical bill from the patient
            Bill_Medical bill = GetSurgeryBill(patient);
            if (bill == null)
            {
                return;
            }

            // Check if we have ingredients (EXCLUDING medicine - that comes from fuel)
            if (!CheckParts(bill))
            {
                // Log what we're missing for debugging
                Logger.Log("[Operate]", $"YAMP: Missing ingredients for {bill.recipe.label}");
                return;
            }

            // Check stock
            float stockCost = CalculateStockCost(bill);
            if (OperationalStock.Stock < stockCost)
            {
                Log.Warning($"YAMP: Not enough stock ({OperationalStock.Stock}/{stockCost}) for {bill.recipe.label}");
                return;
            }

            // Perform Operation
            Log.Message($"YAMP: Performing operation {bill.recipe.label} on {patient.LabelShort}");
            PerformOperation(patient, bill, stockCost);
        }

        private float CalculateStockCost(Bill_Medical bill)
        {
            float stockCost = 0;
            foreach (var ingredient in bill.recipe.ingredients)
            {
                if (ingredient.filter.Allows(ThingDefOf.MedicineHerbal))
                {
                    stockCost += ingredient.GetBaseCount() * Props.stockConsumption;
                }
            }

            return stockCost;
        }

        private void PerformOperation(Pawn patient, Bill_Medical bill, float stockCost)
        {
            RecipeDef recipe = bill.recipe;

            if (!OperationalStock.TryConsumeStock(stockCost))
            {
                Log.Warning($"YAMP: Not enough stock ({OperationalStock.Stock}/{stockCost}) for {recipe.label}");
                return;
            }

            var ingredients = ReserveParts(bill);
            if (ingredients == null)
            {
                Log.Warning($"YAMP: Not enough ingredients for {bill.recipe.label}");
                return;
            }

            // Apply Surgery/Medical Operation
            if (Rand.Value <= Props.surgerySuccessChance)
            {
                // Apply the recipe
                MedPodSurgery.Execute(patient, bill, ingredients, parent);
                Messages.Message($"Operation {recipe.label} on {patient.LabelShort} completed successfully.", parent,
                    MessageTypeDefOf.PositiveEvent);

                // Complete the bill
                patient.BillStack.Delete(bill);
            }
            else
            {
                Messages.Message($"Operation {recipe.label} on {patient.LabelShort} failed.", parent,
                    MessageTypeDefOf.NegativeEvent);
                patient.TakeDamage(new DamageInfo(DamageDefOf.Cut, 10, 0, -1, null, null));
                // Keep bill on failure so they can try again
            }

            // Destroy consumed ingredients
            foreach (Thing t in ingredients)
            {
                t.Destroy();
            }

            // Eject patient if dead
            if (patient.Dead)
            {
                PodConatiner.GetDirectlyHeldThings().TryDrop(
                    PodConatiner.GetPawn(),
                    parent.Position,
                    parent.Map,
                    ThingPlaceMode.Near,
                    out _);
            }
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (!selPawn.CanReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption(
                    "CannotEnter".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(), null);
                yield break;
            }

            if (PodConatiner.GetPawn() != null)
            {
                yield return new FloatMenuOption(
                    "CannotEnter".Translate() + ": " + "Full".Translate().CapitalizeFirst(), null);
                yield break;
            }

            yield return new FloatMenuOption("Enter Med Pod", () =>
            {
                Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("YAMP_EnterMedPod"), parent);
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });
        }

        private Bill_Medical GetSurgeryBill(Pawn patient)
        {
            if (patient.BillStack == null)
            {
                return null;
            }

            Bill_Medical bill = null;
            foreach (Bill b in patient.BillStack)
            {
                if (b is Bill_Medical bm && bm.ShouldDoNow())
                {
                    bill = bm;
                    if (CheckParts(bm))
                    {
                        return bm;
                    }
                }
            }

            return bill;
        }

        public override string CompInspectStringExtra()
        {
            Pawn patient = PodConatiner.GetPawn();
            if (patient != null)
            {
                Bill_Medical bill = GetSurgeryBill(patient);
                if (bill != null)
                {
                    return $"Operation: {bill.recipe.label}";
                }

                return "No pending operations";
            }

            return null;
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Pawn patient = PodConatiner.GetPawn();
            if (patient != null)
            {
                Vector3 drawPos = parent.DrawPos;
                drawPos.y += 0.04f;
                float angle = (Time.realtimeSinceStartup * 50f) % 360f;
                Matrix4x4 matrix = default(Matrix4x4);
                matrix.SetTRS(drawPos, Quaternion.AngleAxis(angle, Vector3.up), new Vector3(2f, 1f, 2f));
                Graphics.DrawMesh(MeshPool.plane10, matrix, YAMP_Assets.ActiveOverlayMat, 0);
            }
        }
    }
}
