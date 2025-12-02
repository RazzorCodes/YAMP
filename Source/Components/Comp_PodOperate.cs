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

            // Check stock availability
            if (OperationalStock.Stock < stockCost)
            {
                Log.Warning($"YAMP: Not enough stock ({OperationalStock.Stock}/{stockCost}) for {recipe.label}");
                return;
            }

            // Check ingredients availability
            if (!CheckParts(bill))
            {
                Log.Warning($"YAMP: Missing ingredients for {bill.recipe.label}");
                return;
            }

            // Get operation handler
            var handler = YAMP.OperationSystem.OperationRegistry.GetHandler(recipe.Worker.GetType());
            if (handler == null)
            {
                Log.Error($"[YAMP] No operation handler found for {recipe.Worker.GetType().Name} ({recipe.label})");
                Messages.Message($"Med pod cannot perform: {recipe.label}", parent, MessageTypeDefOf.RejectInput);
                patient.BillStack.Delete(bill); // Delete unsupported bill
                return;
            }

            // Create operation context with med pod customizations
            var context = new YAMP.OperationSystem.OperationContext
            {
                Patient = patient,
                BodyPart = bill.Part,
                Ingredients = new List<Thing>(), // Virtual ingredients - not used
                Facility = parent,
                Surgeon = null, // Automated surgery
                SuccessChance = Props.surgerySuccessChance,

                // Pre-operation: Consume operational stock
                PreOperationHook = (ctx) =>
                {
                    if (!OperationalStock.TryConsumeStock(stockCost))
                    {
                        Log.Warning($"[YAMP] Failed to consume stock during pre-op");
                    }
                },

                // Post-operation: Collect products into pod container
                PostOperationHook = (ctx, result) =>
                {
                    if (result.Success)
                    {
                        // Add any products to the pod container
                        foreach (var product in result.Products)
                        {
                            if (product.Spawned)
                            {
                                product.DeSpawn();
                                if (!PodConatiner.GetDirectlyHeldThings().TryAdd(product))
                                {
                                    // If container is full, spawn it back
                                    GenPlace.TryPlaceThing(product, parent.Position, parent.Map, ThingPlaceMode.Near);
                                    Log.Warning($"[YAMP] Container full, dropped {product.Label} on ground");
                                }
                                else
                                {
                                    Log.Message($"[YAMP] Collected product: {product.Label}");
                                }
                            }
                        }
                    }
                }
            };

            // Perform the operation
            var result = handler.Perform(context);

            // ALWAYS delete the bill to prevent infinite retries
            patient.BillStack.Delete(bill);

            if (result.Success)
            {
                Messages.Message($"Operation {recipe.label} on {patient.LabelShort} completed successfully.", parent,
                    MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message($"Operation {recipe.label} on {patient.LabelShort} failed: {result.FailureReason}",
                    parent,
                    MessageTypeDefOf.NegativeEvent);

                // Light injury on failure
                patient.TakeDamage(new DamageInfo(DamageDefOf.Cut, 5, 0, -1, null, null));
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
