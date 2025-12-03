using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Mail;
using RimWorld;
using UnityEngine;
using Verse;

namespace YAMP
{
    public class CompProp_OperationalStock
    {
        public float fuelValueHerbal = 10f;
        public float fuelValueIndustrial = 30f;
        public float fuelValueGlitter = 100f;

        public float GetFuelValue(ThingDef def, int amount = 0)
        {
            if (def == ThingDefOf.MedicineHerbal)
            {
                return fuelValueHerbal * amount;
            }

            if (def == ThingDefOf.MedicineIndustrial)
            {
                return fuelValueIndustrial * amount;
            }

            if (def == ThingDefOf.MedicineUltratech)
            {
                return fuelValueGlitter * amount;
            }

            Logger.Log("Info", $"YAMP: Did not parse as medicine: {def.defName}");
            return -1f;
        }
    }

    public class OperationalStock
    {
        private PodContainer _container;

        private float _buffer = 0f;
        public float Buffer => _buffer;
        public float TotalStock => _buffer + _unusedStock;

        private float _unusedStock = 0f;
        public float UnusedStock => _unusedStock;

        private CompProp_OperationalStock _props;

        public OperationalStock(PodContainer container, CompProp_OperationalStock props)
        {
            _container = container;
            _props = props;
        }

        public void ComputeStock()
        {
            _unusedStock = _container.Get()
            .Where(t => t.def.IsMedicine)
            .Aggregate(
                    0f,
                    (acc, thing) =>
                    {
                        float value = _props.GetFuelValue(thing.def, thing.stackCount);
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

                if (ingredient.filter.Allows(ThingDefOf.MedicineHerbal))
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

            var itemsToConsume = new List<Thing>();
            foreach (var candidate in candidates)
            {
                float stackValue = _props.GetFuelValue(candidate.def, candidate.stackCount);
                if (_buffer + stackValue <= amount)
                {
                    _buffer += stackValue;
                    itemsToConsume.Add(candidate.SplitOff(candidate.stackCount));
                }
                else // explicit: if (_buffer + stackValue > amount)
                {
                    int numberOfCandidateToConsume = Mathf.CeilToInt(amount / stackValue);
                    itemsToConsume.Add(candidate.SplitOff(numberOfCandidateToConsume));
                    _buffer += _props.GetFuelValue(candidate.def, numberOfCandidateToConsume);

                    Logger.Debug($"YAMP: Consuming {itemsToConsume.Count} items for {amount} fuel value");
                    foreach (var item in itemsToConsume)
                    {
                        Logger.Debug($"YAMP: Consumed {item.stackCount} {item.def.defName}");
                        item.Destroy();
                    }

                    ComputeStock();
                    return true;
                }
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
