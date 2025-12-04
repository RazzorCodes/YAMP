using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using YAMP.OperationSystem;
using YAMP.OperationSystem.Core;

namespace YAMP.Activities
{
    /// <summary>
    /// Static handler for operation completion callbacks.
    /// Uses new pipeline-based operation system.
    /// </summary>
    public static class OperateHandler
    {
        /// <summary>
        /// Executed when an operation activity completes.
        /// Args: [Building_MedPod facility, Bill_Medical bill, List<Thing> parts, float stockCost]
        /// </summary>
        public static void OnCompleteHandler(object[] args)
        {
            if (args == null || args.Length < 4)
            {
                Logger.Error("OperateHandler: Invalid arguments for OnCompleteHandler");
                return;
            }

            var facility = args[0] as Building_MedPod;
            var bill = args[1] as Bill_Medical;
            var parts = args[2] as List<Thing>;
            var stockCost = (float)args[3];

            if (facility == null || bill == null)
            {
                Logger.Error("OperateHandler: Missing facility or bill");
                ReturnParts(facility?.Container, parts, bill);
                return;
            }

            ExecuteOperation(facility, bill, parts);
        }

        /// <summary>
        /// Executed when an operation activity is stopped early.
        /// Args: [Building_MedPod facility, Bill_Medical bill, List<Thing> parts, float stockCost]
        /// </summary>
        public static void OnStopHandler(object[] args)
        {
            if (args == null || args.Length < 4)
            {
                Logger.Error("OperateHandler: Invalid arguments for OnStopHandler");
                return;
            }

            var facility = args[0] as Building_MedPod;
            var bill = args[1] as Bill_Medical;
            var parts = args[2] as List<Thing>;

            Logger.Debug("OperateHandler: Activity stopped, returning parts");
            ReturnParts(facility?.Container, parts, bill);
        }

        private static void ExecuteOperation(Building_MedPod facility, Bill_Medical bill, List<Thing> parts)
        {
            var handler = OperationRegistry.GetHandler(bill.recipe.Worker.GetType());
            if (handler == null)
            {
                Logger.Debug($"OperateHandler: No custom handler for {bill.recipe.Worker.GetType()}, skipping");
                ReturnParts(facility.Container, parts, bill);
                facility.Stock.UnreserveParts(); // Cleanup medicine reservation
                return;
            }

            // Create new context with object[] arguments
            // Arguments: [0]=Patient, [1]=BodyPart, [2]=Recipe, [3]=Facility, [4]=Ingredients
            var context = new OperationContext
            {
                OperationName = handler.Name,
                Arguments = new object[]
                {
                    facility.Container.GetPawn(),  // 0: Patient (Pawn)
                    bill.Part,                      // 1: BodyPart (BodyPartRecord)
                    bill.recipe,                    // 2: Recipe (RecipeDef)
                    facility,                       // 3: Facility (ThingWithComps)
                    parts                           // 4: Ingredients (List<Thing>)
                }
            };

            // Execute through pipeline
            var result = OperationRegistry.ExecuteOperation(handler, context);

            if (!result.Success)
            {
                Logger.Debug($"OperateHandler: Operation '{handler.Name}' failed: {result.FailureReason}");

                // Return unused parts on failure
                ReturnParts(facility.Container, parts, bill);
                facility.Stock.UnreserveParts(); // Return reserved medicines

                // Notify components after failed operation
                facility.GetComp<Comp_PodTend>()?.CheckTend();
                facility.GetComp<Comp_PodOperate>()?.CheckOperation();
            }
            else
            {
                Logger.Debug($"OperateHandler: Operation '{handler.Name}' succeeded");

                // Consume reserved parts on success
                facility.Stock.ConsumeParts();

                // Notify components after successful operation
                facility.GetComp<Comp_PodTend>()?.CheckTend();
                facility.GetComp<Comp_PodOperate>()?.CheckOperation();
            }
        }

        public static bool IsGenericMedicine(IngredientCount ingredient)
        {
            return
                !ingredient.IsFixedIngredient &&
                ingredient.filter.Allows(ThingDefOf.MedicineHerbal);
        }

        public static List<Thing> ReserveParts(PodContainer container, Bill_Medical bill)
        {
            var parts = new List<Thing>();
            var recipe = bill.recipe;

            foreach (var ingredient in recipe.ingredients)
            {
                // We want to skip generic "medicine" as those are retrieved from the OperationalStock
                if (IsGenericMedicine(ingredient))
                {
                    continue;
                }

                var part = ReservePart(container, ingredient);

                if (part != null)
                {
                    Logger.Debug($"OperateHandler: Reserved {part.Count} of {ingredient.filter.Summary} for {bill.recipe.label}");
                    parts.AddRange(part);
                }
                else
                {
                    Logger.Trace($"OperateHandler: Missing {ingredient.filter.Summary} for {bill.recipe.label}");
                    // return them
                    ReturnParts(container, parts, bill);
                    Logger.Debug($"OperateHandler: Returned {parts.Count} items for {bill.recipe.label}");
                    return null;
                }
            }

            Logger.Debug($"OperateHandler: Reserved all parts for {bill.recipe.label}");
            return parts;
        }

        private static List<Thing> ReservePart(PodContainer container, IngredientCount ingredient)
        {
            // we could need 2 stacks of something
            var part = new List<Thing>();

            var stillNeeded = ingredient.GetBaseCount();
            var candidates = container
                .Get()
                .Where(t => ingredient.filter.Allows(t))
                .OrderBy(t => t.MarketValue)
                .ToList();

            foreach (var candidate in candidates)
            {
                // generally we will have enough in one stack
                if (candidate.stackCount >= stillNeeded)
                {
                    part.Add(candidate.SplitOff((int)stillNeeded));
                    return part;
                }
                else
                {
                    stillNeeded -= candidate.stackCount;
                }
            }

            return null;
        }

        public static void ReturnParts(PodContainer container, List<Thing> parts, Bill_Medical fallback)
        {
            if (parts == null) return;
            foreach (var part in parts)
            {
                if (!container.GetDirectlyHeldThings().TryAdd(part))
                {
                    GenPlace.TryPlaceThing(
                        part,
                        fallback.GiverPawn.Position,
                        fallback.GiverPawn.Map,
                        ThingPlaceMode.Near);
                }
            }
        }
    }
}
