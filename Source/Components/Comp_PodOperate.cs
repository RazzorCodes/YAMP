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
        private OperationalStock OperationalStock =>
            _operationalStock ??= parent.GetComp<OperationalStock>();

        private PodContainer _podConatiner;
        private PodContainer PodConatiner =>
            _podConatiner ??= ((Building_MedPod)parent).Container;

        private int currentTick = 0;
        private bool isOperating = false;
        private Bill_Medical currentBill = null;
        private List<Thing> currentParts = null;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentTick, "currentTick", 0);
            Scribe_Values.Look(ref isOperating, "isOperating", false);
        }

        public override void CompTickRare()
        {
            if (PodConatiner.GetPawn() == null)
            {
                if (isOperating)
                {
                    CancelOperation();
                }
                return;
            }

            if (isOperating)
            {
                currentTick++;
                if (currentTick >= currentBill.recipe.workAmount / 2f)
                {
                    CompleteOperation();
                }
            }
            else if (parent.IsHashIntervalTick(250))
            {
                TryStartOperation();
            }
        }

        List<Thing> GetParts(Bill_Medical bill)
        {
            List<Thing> ingredients = new List<Thing>();
            foreach (IngredientCount ing in bill.recipe.ingredients)
            {
                if (ing.filter.Allows(ThingDefOf.MedicineHerbal))
                {
                    continue;
                }

                int stillNeeded = (int)ing.GetBaseCount();
                var candidates = PodConatiner
                    .Get()
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
                    Logger.Log(
                        "[Operate]",
                        $"YAMP: Missing {ing.filter.Summary} for {bill.recipe.label}"
                    );
                    // return them
                    PodConatiner.Get().AddRange(ingredients);
                    return null;
                }
            }

            return ingredients;
        }

        public void TryStartOperation()
        {
            Pawn patient = PodConatiner.Get().OfType<Pawn>().FirstOrDefault();
            if (patient == null)
            {
                return;
            }

            // Get the first medical bill from the patient
            Bill_Medical bill = GetSurgeryBill(patient);
            if (bill == null)
            {
                return;
            }

            // Check stock
            float stockCost = CalculateStockCost(bill);
            if (OperationalStock.Stock < stockCost)
            {
                if (parent.IsHashIntervalTick(2500))
                {
                    Log.Warning($"YAMP: Not enough stock ({OperationalStock.Stock}/{stockCost}) for {bill.recipe.label}");
                }
                return;
            }

            currentParts = GetParts(bill);
            if (currentParts == null)
            {
                if (parent.IsHashIntervalTick(2500))
                {
                    Logger.Log("[Operate]", $"YAMP: Missing ingredients for {bill.recipe.label}");
                }
                return;
            }

            // Start Operation
            isOperating = true;
            currentTick = 0;
            currentBill = bill;

            PerformOperation(patient, bill, stockCost, currentParts);
        }

        private void CancelOperation()
        {
            isOperating = false;
            currentTick = 0;
            currentBill = null;
            Logger.Log("[Operate]", "Operation cancelled");
            ReturnParts(currentParts);
        }

        private void CompleteOperation()
        {
            isOperating = false;
            currentTick = 0;

            Pawn patient = PodConatiner.Get().OfType<Pawn>().FirstOrDefault();
            if (patient == null) return;

            Bill_Medical bill = currentBill;
            currentBill = null;

            if (bill == null)
            {
                // Try to find the bill again if we lost reference (e.g. after load)
                bill = GetSurgeryBill(patient);
                if (bill == null) return;
            }

            Log.Message($"YAMP: Performing operation {bill.recipe.label} on {patient.LabelShort}");
        }

        private void ReturnParts(List<Thing> parts)
        {
            if (parts == null) return;
            foreach (var part in parts)
            {
                if (!PodConatiner.GetDirectlyHeldThings().TryAdd(part))
                {
                    GenPlace.TryPlaceThing(part, parent.Position, parent.Map, ThingPlaceMode.Near);
                }
            }
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

        private void PerformOperation(
            Pawn patient,
            Bill_Medical bill,
            float stockCost,
            List<Thing> parts
        )
        {
            RecipeDef recipe = bill.recipe;

            // Check stock availability
            if (OperationalStock.Stock < stockCost)
            {
                Log.Warning(
                    $"YAMP: Not enough stock ({OperationalStock.Stock}/{stockCost}) for {recipe.label}"
                );
                ReturnParts(parts);
                return;
            }

            // Get operation handler
            var handler = YAMP.OperationSystem.OperationRegistry.GetHandler(
                recipe.Worker.GetType()
            );
            if (handler == null)
            {
                Log.Error(
                    $"[YAMP] No operation handler found for {recipe.Worker.GetType().Name} ({recipe.label})"
                );
                Messages.Message(
                    $"Med pod cannot perform: {recipe.label}",
                    parent,
                    MessageTypeDefOf.RejectInput
                );
                ((Building_MedPod)parent).BillStack.Delete(bill); // Delete unsupported bill
                ReturnParts(parts);
                return;
            }

            // Create operation context with med pod customizations
            var context = new YAMP.OperationSystem.OperationContext
            {
                Patient = patient,
                Bill = bill,
                BodyPart = bill.Part,
                Ingredients = parts,
                Facility = parent,
                Surgeon = null, // Automated surgery
                SuccessChance = Props.surgerySuccessChance,

                // Pre-operation: Consume operational stock
                PreOperationHook = (ctx) =>
                {
                    if (!OperationalStock.TryConsumeStock(stockCost))
                    {
                        Logger.Log("YAMP", $"Failed to consume stock during pre-op");
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
                                    GenPlace.TryPlaceThing(
                                        product,
                                        parent.Position,
                                        parent.Map,
                                        ThingPlaceMode.Near
                                    );
                                    Log.Warning(
                                        $"[YAMP] Container full, dropped {product.Label} on ground"
                                    );
                                }
                                else
                                {
                                    Logger.Log("YAMP", $"Collected product: {product.Label}");
                                }
                            }
                        }
                    }
                },
            };

            // Perform the operation
            var result = handler.Perform(context);

            // ALWAYS delete the bill to prevent infinite retries
            ((Building_MedPod)parent).BillStack.Delete(bill);

            if (result.Success)
            {
                Messages.Message(
                    $"Operation {recipe.label} on {patient.LabelShort} completed successfully.",
                    parent,
                    MessageTypeDefOf.PositiveEvent
                );
            }
            else
            {
                Messages.Message(
                    $"Operation {recipe.label} on {patient.LabelShort} failed: {result.FailureReason}",
                    parent,
                    MessageTypeDefOf.NegativeEvent
                );

                // Light injury on failure
                patient.TakeDamage(new DamageInfo(DamageDefOf.Cut, 5, 0, -1, null, null));
            }

            // Eject patient if dead
            if (patient.Dead)
            {
                PodConatiner
                    .GetDirectlyHeldThings()
                    .TryDrop(
                        PodConatiner.GetPawn(),
                        parent.Position,
                        parent.Map,
                        ThingPlaceMode.Near,
                        out _
                    );
            }
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (!selPawn.CanReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
            {
                yield return new FloatMenuOption(
                    "CannotEnter".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(),
                    null
                );
                yield break;
            }

            if (PodConatiner.GetPawn() != null)
            {
                yield return new FloatMenuOption(
                    "CannotEnter".Translate() + ": " + "Full".Translate().CapitalizeFirst(),
                    null
                );
                yield break;
            }

            yield return new FloatMenuOption(
                "Enter Med Pod",
                () =>
                {
                    Job job = JobMaker.MakeJob(
                        DefDatabase<JobDef>.GetNamed("YAMP_EnterMedPod"),
                        parent
                    );
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                }
            );
        }

        private Bill_Medical GetSurgeryBill(Pawn patient)
        {
            var medPod = parent as Building_MedPod;
            if (medPod?.BillStack == null)
            {
                return null;
            }

            Bill_Medical bill = null;
            foreach (Bill b in medPod.BillStack)
            {
                if (b is Bill_Medical bm && bm.ShouldDoNow())
                {
                    bill = bm;
                    if (GetParts(bm) != null)
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
                if (isOperating && currentBill != null)
                {
                    return $"Operating: {currentBill.recipe.label} ({(float)currentTick / (currentBill.recipe.workAmount / 2f):P0})";
                }

                Bill_Medical bill = GetSurgeryBill(patient);
                if (bill != null)
                {
                    return $"Pending Operation: {bill.recipe.label}";
                }

                return "No pending operations";
            }

            return null;
        }

        public override void PostDraw()
        {
            base.PostDraw();

            Pawn patient = PodConatiner.GetPawn();
            if (patient == null)
            {
                return;
            }

            if (isOperating)
            {
                Vector3 barPos = parent.DrawPos;
                barPos.y = AltitudeLayer.MetaOverlays.AltitudeFor();
                barPos += Vector3.forward * 0.25f;

                GenDraw.DrawFillableBar(new GenDraw.FillableBarRequest
                {
                    center = barPos,
                    size = new Vector2(0.8f, 0.14f),
                    fillPercent = (float)currentTick / (currentBill.recipe.workAmount / 2f),
                    filledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.9f, 0.85f, 0.2f)),
                    unfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f)),
                    margin = 0.15f,
                    rotation = Rot4.North
                });
            }

            Vector3 drawPos = parent.DrawPos;
            drawPos.y += 0.04f;
            float angle = (Time.realtimeSinceStartup * 50f) % 360f;
            Matrix4x4 matrix = default(Matrix4x4);
            matrix.SetTRS(
                drawPos,
                Quaternion.AngleAxis(angle, Vector3.up),
                new Vector3(2f, 1f, 2f)
            );
            Graphics.DrawMesh(MeshPool.plane10, matrix, YAMP_Assets.ActiveOverlayMat, 0);
        }
    }
}
