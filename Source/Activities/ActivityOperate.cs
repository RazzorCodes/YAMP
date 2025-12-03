using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using YAMP;
using YAMP.OperationSystem;

namespace YAMP.Activities
{
    class ActivityOperate : BaseActivity
    {
        private const string ActivityType = "Operate";
        private string _currentOperation = "";
        public override string Name => ActivityType + "/" + _currentOperation;

        public float stockMultiplier = 1f;

        Building_MedPod _facility = null;
        Bill_Medical _bill = null;

        List<Thing> _parts = null;
        float _stockCost = 0f;

        public ActivityOperate(Building_MedPod facility, Bill_Medical bill)
        {
            _currentOperation = bill.recipe.label;
            if (facility == null || bill == null)
            {
                Logger.Debug($"Activity {Name} missing facility or bill");
                End();
                return;
            }

            _facility = facility;
            _bill = bill;
        }

        public void Execute()
        {
            var handler =
                OperationRegistry.GetHandler(
                    _bill.recipe.Worker.GetType()
                );
            if (handler == null)
            {
                Logger.Debug($"Activity {Name} missing handler for {_bill.recipe.Worker.GetType()}");
                ReturnParts(_facility.Container, _parts, _bill);
                End();
                return;
            }

            var context = new OperationContext
            {
                Patient = _facility.Container.GetPawn(),
                Bill = _bill,
                BodyPart = _bill.Part,
                Ingredients = _parts,
                Facility = _facility,
                Surgeon = null, // Automated surgery
                SuccessChance = 1f, // todo: use vanilla success chance

                // Pre-operation: Consume operational stock
                PreOperationHook = PreOperationAction,

                // Post-operation: Collect products into pod container
                PostOperationHook = PostOperationAction,
            };

            var result = handler.Perform(context);
            if (!result.Success)
            {
                Logger.Debug($"Activity {Name} failed to execute operation");
            }

            End();
        }

        private void PreOperationAction(OperationContext ctx)
        {
            if (!_facility.Stock.TryConsumeStock(_stockCost))
            {
                Logger.Debug($"Failed to consume stock during pre-op");
            }
        }

        private void PostOperationAction(OperationContext ctx, OperationResult result)
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
                    if (!_facility.Container.GetDirectlyHeldThings().TryAdd(product))
                    {
                        // If container is full, spawn it near the facility
                        GenPlace.TryPlaceThing(
                            product,
                            _facility.Position,
                            _facility.Map,
                            ThingPlaceMode.Near
                        );
                        Logger.Debug(
                            $"[YAMP] Container full or not available, dropped {product.Label} on ground"
                        );
                    }
                    else
                    {
                        Logger.Trace($"Collected product: {product.Label}");
                    }
                }
            }
        }
        public override void Start()
        {
            base.Start(_bill.GetWorkAmount() > 100 ? _bill.GetWorkAmount() : 100);

            _parts = ReserveParts(_facility.Container, _bill);
            if (_parts == null)
            {
                Logger.Debug($"Activity {Name} missing parts for {_bill.recipe.label}");
                End();
                return;
            }

            _stockCost = OperationalStock.CalculateStockCost(_bill.recipe);
            if (_stockCost > _facility.Stock.TotalStock)
            {
                Logger.Debug($"Activity {Name} missing stock for {_bill.recipe.label}. Needed {_stockCost}, have {_facility.Stock.TotalStock}");
                End();
                return;
            }
        }

        bool IsGenericMedicine(IngredientCount ingredient)
        {
            return
                !ingredient.IsFixedIngredient &&
                ingredient.filter.Allows(ThingDefOf.MedicineHerbal);
        }

        List<Thing> ReservePart(PodContainer container, IngredientCount ingredient)
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

        public override void End()
        {
            base.End();
        }

        void ReturnParts(PodContainer container, List<Thing> parts, Bill_Medical fallback)
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

        List<Thing> ReserveParts(PodContainer container, Bill_Medical bill)
        {
            var parts = new List<Thing>();
            var recepie = bill.recipe;

            foreach (var ingredient in recepie.ingredients)
            {
                // We want to skip generic "medicine" as those are retreived from the OperationalStock
                if (IsGenericMedicine(ingredient))
                {
                    continue;
                }

                var part = ReservePart(container, ingredient);
                Logger.Debug($"Activity {Name} reserved {part.Count} of {ingredient.filter.Summary} for {bill.recipe.label}");

                if (part != null)
                {
                    parts.AddRange(part);
                }
                else
                {
                    Logger.Trace($"Activity {Name} missing {ingredient.filter.Summary} for {bill.recipe.label}");
                    // return them
                    ReturnParts(container, parts, bill);
                    Logger.Debug($"Activity {Name} returned {parts.Count} of {ingredient.filter.Summary} for {bill.recipe.label}");
                    return null;
                }
            }

            Logger.Debug($"Activity {Name} reserved all parts for {bill.recipe.label}");
            return parts;
        }


    }
}