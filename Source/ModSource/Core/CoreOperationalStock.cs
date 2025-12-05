using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using YAMP.OperationSystem.Core;

namespace YAMP
{
    /// <summary>
    /// Operational stock management with periodic buffering and part reservation.
    /// This partial class handles RimWorld-specific integrations.
    /// </summary>
    public partial class OperationalStock : IExposable
    {
        // ==================== CONSTANTS ====================

        public const float MAX_BUFFER = 500f;
        private const int BUFFER_TICK_INTERVAL = 300;

        // ==================== FIELDS ====================

        private PodContainer _container;
        private float _buffer = 0f;
        private float _unusedStock = 0f;
        private int _lastBufferTick = 0;
        private List<Thing> _reservedParts = new List<Thing>();

        // Fuel values for medicine types
        private readonly float fuelValueHerbal = 10f;
        private readonly float fuelValueIndustrial = 30f;
        private readonly float fuelValueGlitter = 100f;

        // ==================== PROPERTIES ====================

        public float Buffer => _buffer;
        public float UnusedStock => _unusedStock;
        public float TotalStock => CoreOperationalStock.ComputeTotalStock(_buffer, _unusedStock);

        // ==================== CONSTRUCTOR ====================

        public OperationalStock(PodContainer container)
        {
            _container = container;
        }

        // ==================== ISAVE/LOAD ====================

        public void ExposeData()
        {
            Scribe_Values.Look(ref _buffer, "buffer", 0f);
            Scribe_Values.Look(ref _lastBufferTick, "lastBufferTick", 0);
            Scribe_Collections.Look(ref _reservedParts, "reservedParts", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _reservedParts ??= new List<Thing>();
                ComputeStock();
            }
        }

        // ==================== STOCK VALUE CALCULATION ====================

        /// <summary>
        /// Get stock value for a RimWorld ThingDef using properties directly.
        /// </summary>
        public float GetFuelValue(ThingDef def, int amount = 1)
        {
            if (def == ThingDefOf.MedicineHerbal)
                return fuelValueHerbal * amount;

            if (def == ThingDefOf.MedicineIndustrial)
                return fuelValueIndustrial * amount;

            if (def == ThingDefOf.MedicineUltratech)
                return fuelValueGlitter * amount;

            Logger.Log("Info", $"YAMP: Did not parse as medicine: {def.defName}");
            return -1f;
        }

        // ==================== STOCK COMPUTATION ====================

        /// <summary>
        /// Compute unused stock from container medicines.
        /// </summary>
        public void ComputeStock()
        {
            _unusedStock = _container.Get()
                .Where(t => t.def.IsMedicine && !_reservedParts.Contains(t))
                .Aggregate(0f, (acc, thing) =>
                {
                    float value = GetFuelValue(thing.def, thing.stackCount);
                    if (value > 0)
                        acc += value;
                    return acc;
                });
        }

        /// <summary>
        /// Calculate stock cost for a recipe (static for backwards compatibility).
        /// </summary>
        public static float CalculateStockCost(RecipeDef recipe)
        {
            var ingredients = new Dictionary<string, (int count, bool isFixed, bool isMedicine)>();

            foreach (IngredientCount ingredient in recipe.ingredients)
            {
                bool isFixed = ingredient.IsFixedIngredient;
                bool isMedicine = ingredient.filter.Allows(ThingDefOf.MedicineHerbal);
                int count = (int)ingredient.GetBaseCount();

                ingredients[ingredient.filter.Summary] = (count, isFixed, isMedicine);
            }

            return CoreOperationalStock.CalculateRequiredStock(ingredients);
        }

        // ==================== PERIODIC BUFFERING ====================

        /// <summary>
        /// Called every tick - buffers one medicine every 300 ticks.
        /// </summary>
        /// <param name="currentTick">Current game tick</param>
        /// <param name="targetBufferLevel">Target buffer level from gizmo (default: MAX_BUFFER)</param>
        public void TickBuffering(int currentTick, float targetBufferLevel = MAX_BUFFER)
        {
            if (currentTick - _lastBufferTick >= BUFFER_TICK_INTERVAL)
            {
                _lastBufferTick = currentTick;
                BufferOneMedicine(targetBufferLevel);
            }
        }

        /// <summary>
        /// Buffer one medicine (lowest value) if space available.
        /// </summary>
        /// <param name="targetBufferLevel">Target buffer level from gizmo</param>
        private void BufferOneMedicine(float targetBufferLevel)
        {
            var candidates = _container.Get()
                .Where(thing => thing.def.IsMedicine && !_reservedParts.Contains(thing))
                .OrderBy(thing => thing.def.BaseMarketValue)
                .ToList();

            foreach (var candidate in candidates)
            {
                float perItemValue = GetFuelValue(candidate.def, 1);

                if (perItemValue <= 0)
                    continue;

                // Check if we can buffer without overflow (using target level from gizmo)
                if (CoreOperationalStock.CanBuffer(perItemValue, _buffer, targetBufferLevel))
                {
                    // Consume one item from stack
                    var item = candidate.SplitOff(1);
                    _buffer += perItemValue;
                    item.Destroy();

                    ComputeStock();
                    Logger.Debug($"YAMP: Buffered 1 {candidate.def.defName} (+{perItemValue}f), buffer now: {_buffer}f / {targetBufferLevel}f");
                    return; // Only buffer one per tick
                }
            }
        }

        // ==================== STOCK PROVISION & CONSUMPTION ====================

        /// <summary>
        /// Provide stock by drawing from buffer, then unused medicines.
        /// </summary>
        private bool TryProvideStock(float amount)
        {
            // Try allocating from buffer first
            var (allocated, remaining) = CoreOperationalStock.TryAllocateFromBuffer(amount, _buffer);
            _buffer -= allocated;

            if (remaining <= 0)
                return true;

            // Need to pull from unused stock
            if (CoreOperationalStock.ShouldWaitForStock(remaining, _unusedStock))
            {
                Logger.Debug($"YAMP: Not enough stock to provide {remaining}f, UnusedStock: {_unusedStock}f");
                return false;
            }

            // Consume medicines to cover remaining
            var candidates = _container.Get()
                .Where(thing => thing.def.IsMedicine && !_reservedParts.Contains(thing))
                .OrderBy(thing => thing.def.BaseMarketValue)
                .ToList();

            float stillNeeded = remaining;

            foreach (var candidate in candidates)
            {
                if (stillNeeded <= 0)
                    break;

                float perItemValue = GetFuelValue(candidate.def, 1);
                float stackValue = GetFuelValue(candidate.def, candidate.stackCount);

                if (perItemValue <= 0)
                    continue;

                int toConsume = CoreOperationalStock.CalculateItemsToConsume(
                    stillNeeded, perItemValue, candidate.stackCount);

                float consumedValue = GetFuelValue(candidate.def, toConsume);
                _buffer += consumedValue;
                stillNeeded -= consumedValue;

                var splitItem = candidate.SplitOff(toConsume);
                splitItem.Destroy();

                Logger.Debug($"YAMP: Consumed {toConsume} {candidate.def.defName} (+{consumedValue}f)");
            }

            ComputeStock();
            return stillNeeded <= 0;
        }

        /// <summary>
        /// Consume stock (public API).
        /// </summary>
        public bool TryConsumeStock(float amount)
        {
            if (!TryProvideStock(amount))
                return false;

            Logger.Debug($"YAMP: Consuming {amount}f stock, buffer: {_buffer}f");
            _buffer -= amount;
            return true;
        }

        // ==================== PART RESERVATION ====================

        /// <summary>
        /// Check if required parts for recipe are available.
        /// </summary>
        public bool HasRequiredParts(RecipeDef recipe)
        {
            foreach (var ingredient in recipe.ingredients)
            {
                if (!ingredient.IsFixedIngredient || !IsMedicineIngredient(ingredient))
                    continue;

                // Need specific medicine - check container
                var count = _container.Get()
                    .Where(t => ingredient.filter.Allows(t) && !_reservedParts.Contains(t))
                    .Sum(t => t.stackCount);

                if (count < ingredient.GetBaseCount())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Reserve parts for recipe (removes from container, stores in list).
        /// </summary>
        public bool ReserveParts(RecipeDef recipe)
        {
            var toReserve = new List<Thing>();

            foreach (var ingredient in recipe.ingredients)
            {
                if (!ingredient.IsFixedIngredient || !IsMedicineIngredient(ingredient))
                    continue;

                float stillNeeded = ingredient.GetBaseCount();
                var candidates = _container.Get()
                    .Where(t => ingredient.filter.Allows(t) && !_reservedParts.Contains(t))
                    .OrderBy(t => t.MarketValue)
                    .ToList();

                foreach (var candidate in candidates)
                {
                    if (stillNeeded <= 0)
                        break;

                    if (candidate.stackCount >= stillNeeded)
                    {
                        var split = candidate.SplitOff((int)stillNeeded);
                        toReserve.Add(split);
                        stillNeeded = 0;
                    }
                    else
                    {
                        toReserve.Add(candidate);
                        stillNeeded -= candidate.stackCount;
                    }
                }

                if (stillNeeded > 0)
                {
                    // Failed - return what we took
                    ReturnPartsInternal(toReserve);
                    return false;
                }
            }

            _reservedParts.AddRange(toReserve);
            ComputeStock(); // Recompute since we removed from available
            Logger.Debug($"YAMP: Reserved {toReserve.Count} parts for {recipe.label}");
            return true;
        }

        /// <summary>
        /// Unreserve parts (return to container).
        /// </summary>
        public void UnreserveParts()
        {
            ReturnPartsInternal(_reservedParts);
            _reservedParts.Clear();
            ComputeStock();
            Logger.Debug("YAMP: Unreserved all parts");
        }

        /// <summary>
        /// Consume reserved parts (destroy them).
        /// </summary>
        public void ConsumeParts()
        {
            foreach (var part in _reservedParts)
            {
                part.Destroy();
            }

            Logger.Debug($"YAMP: Consumed {_reservedParts.Count} reserved parts");
            _reservedParts.Clear();
        }

        private void ReturnPartsInternal(List<Thing> parts)
        {
            foreach (var part in parts)
            {
                _container.GetDirectlyHeldThings().TryAddOrTransfer(part);
            }
        }

        private bool IsMedicineIngredient(IngredientCount ingredient)
        {
            return ingredient.filter.Allows(ThingDefOf.MedicineHerbal) ||
                   ingredient.filter.Allows(ThingDefOf.MedicineIndustrial) ||
                   ingredient.filter.Allows(ThingDefOf.MedicineUltratech);
        }
    }
}
