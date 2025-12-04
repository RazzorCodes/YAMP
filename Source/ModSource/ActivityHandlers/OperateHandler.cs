using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using YAMP.OperationSystem;

namespace YAMP.Activities
{
    /// <summary>
    /// Static handler for operation completion callbacks
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

            ExecuteOperation(facility, bill, parts, stockCost);
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

        private static void ExecuteOperation(Building_MedPod facility, Bill_Medical bill, List<Thing> parts, float stockCost)
        {
            var handler = OperationRegistry.GetHandler(bill.recipe.Worker.GetType());
            if (handler == null)
            {
                Logger.Debug($"OperateHandler: Missing handler for {bill.recipe.Worker.GetType()}");
                ReturnParts(facility.Container, parts, bill);
                return;
            }

            var context = new OperationContext
            {
                Patient = facility.Container.GetPawn(),
                Bill = bill,
                BodyPart = bill.Part,
                Ingredients = parts,
                Facility = facility,
                Surgeon = null, // Automated surgery
                SuccessChance = 1f, // todo: use vanilla success chance

                // Pre-operation: Consume operational stock
                PreOperationHook = (ctx) => PreOperationAction(facility, stockCost),

                // Post-operation: Collect products into pod container
                PostOperationHook = (ctx, result) => PostOperationAction(facility, result),
            };

            var result = handler.Perform(context);
            if (!result.Success)
            {
                Logger.Debug($"OperateHandler: Failed to execute operation");

                // Notify components after failed operation
                facility.GetComp<Comp_PodTend>()?.CheckTend();
                facility.GetComp<Comp_PodOperate>()?.CheckOperation();
            }
        }

        private static void PreOperationAction(Building_MedPod facility, float stockCost)
        {
            if (!facility.Stock.TryConsumeStock(stockCost))
            {
                Logger.Debug($"OperateHandler: Failed to consume stock during pre-op");
            }
        }

        private static void PostOperationAction(Building_MedPod facility, OperationResult result)
        {
            if (result.Success)
            {
                // Add any products to the pod container
                foreach (var product in result.Products)
                {
                    // Despawn first if spawned to remove from map/container
                    if (product.Spawned)
                    {
                        product.DeSpawn();
                    }

                    // Now try to add to container
                    if (!facility.Container.GetDirectlyHeldThings().TryAdd(product))
                    {
                        // If container is full, spawn it near the facility
                        GenPlace.TryPlaceThing(
                            product,
                            facility.Position,
                            facility.Map,
                            ThingPlaceMode.Near
                        );
                        Logger.Debug(
                            $"[YAMP] Container full or not available, dropped {product.Label} on ground"
                        );
                    }
                    else
                    {
                        Logger.Trace($"OperateHandler: Collected product: {product.Label}");
                    }
                }
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
