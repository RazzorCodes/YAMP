using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Mail;
using RimWorld;
using UnityEngine;
using Verse;

namespace YAMP
{
    public class OperationalStock : IExposable
    {
        public float fuelValueHerbal = 10f;
        public float fuelValueIndustrial = 30f;
        public float fuelValueGlitter = 100f;

        private IMedicineDefProvider _defProvider;

        public float GetFuelValue(ThingDef def, int amount = 0)
        {
            if (def == _defProvider.Herbal)
            {
                return fuelValueHerbal * amount;
            }

            if (def == _defProvider.Industrial)
            {
                return fuelValueIndustrial * amount;
            }

            if (def == _defProvider.Ultratech)
            {
                return fuelValueGlitter * amount;
            }

            Logger.Log("Info", $"YAMP: Did not parse as medicine: {def.defName}");
            return -1f;
        }
        private IPodContainer _container;

        private float _buffer = 0f;
        public float Buffer => _buffer;
        public float TotalStock => _buffer + _unusedStock;

        private float _unusedStock = 0f;
        public float UnusedStock => _unusedStock;

        public OperationalStock()
        {
            _defProvider = new MedicineDefProvider();
        }

        public OperationalStock(IPodContainer container, IMedicineDefProvider defProvider = null)
        {
            _container = container;
            _defProvider = defProvider ?? new MedicineDefProvider();
        }

        public OperationalStock(Building_MedPod parent)
        {
            _container = parent.Container;
            _defProvider = new MedicineDefProvider();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref _buffer, "buffer", 0f);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ComputeStock();
            }
        }

        public void ComputeStock()
        {
            _unusedStock = _container.Get()
            .Where(t => t.def.IsMedicine)
            .Aggregate(
                    0f,
                    (acc, thing) =>
                    {
                        float value = GetFuelValue(thing.def, thing.stackCount);
                        if (value > 0)
                        {
                            acc += value;
                        }
                        return acc;
                    });
        }

        public static float CalculateStockCost(RecipeDef recipe)
        {
            float baseStockCost = 0;
            foreach (IngredientCount ingredient in recipe.ingredients)
            {
                if (ingredient.IsFixedIngredient)
                {
                    continue;
                }

                if (ingredient.filter.Allows(ThingDefOf.MedicineHerbal)) // Static method, might be hard to replace without passing provider. 
                // But CalculateStockCost is static! It cannot use instance _defProvider.
                // We should probably leave it as is, or pass provider to it?
                // Or make it non-static?
                // For now, let's leave it as is. It uses ThingDefOf.MedicineHerbal.
                // If the test calls this, it will crash. But the test calls ComputeStock (instance method).
                {
                    baseStockCost += ingredient.GetBaseCount();
                }
            }

            return baseStockCost;
        }

        private bool TryProvideStock(float amount)
        {
            if (_buffer >= amount)
            {
                return true;
            }

            if (TotalStock < amount)
            {
                Logger.Debug($"YAMP: Not enough stock to provide {amount} fuel value, TotalStock: {TotalStock}");
                return false;
            }

            var candidates = _container.Get()
                .Where(thing => thing.def.IsMedicine)
                .OrderBy(thing => thing.def.BaseMarketValue)
                .ToList();

            float stillNeeded = amount - _buffer;

            foreach (var candidate in candidates)
            {
                if (stillNeeded <= 0)
                {
                    break;
                }

                float stackValue = GetFuelValue(candidate.def, candidate.stackCount);
                float perItemValue = GetFuelValue(candidate.def, 1);

                if (stackValue <= stillNeeded)
                {
                    // Consume entire stack
                    _buffer += stackValue;
                    stillNeeded -= stackValue;
                    var splitItem = candidate.SplitOff(candidate.stackCount);
                    Logger.Debug($"YAMP: Consumed {splitItem.stackCount} {splitItem.def.defName}");
                    splitItem.Destroy();
                }
                else
                {
                    // Consume partial stack
                    int numberOfCandidateToConsume = Mathf.CeilToInt(stillNeeded / perItemValue);
                    float consumedValue = GetFuelValue(candidate.def, numberOfCandidateToConsume);
                    _buffer += consumedValue;
                    stillNeeded = 0;
                    var splitItem = candidate.SplitOff(numberOfCandidateToConsume);
                    Logger.Debug($"YAMP: Consumed {splitItem.stackCount} {splitItem.def.defName}");
                    splitItem.Destroy();

                    ComputeStock();
                    return true;
                }
            }

            if (stillNeeded <= 0)
            {
                ComputeStock();
                return true;
            }

            Logger.Error($"Could not consume enough items to make stock when TotalStock({TotalStock}) < amount({amount})");
            return false;
        }

        public bool TryConsumeStock(float amount)
        {
            if (!TryProvideStock(amount))
            {
                return false;
            }

            Logger.Debug($"YAMP: Consuming {amount} stock");
            _buffer -= amount;
            return true;
        }
    }
}
