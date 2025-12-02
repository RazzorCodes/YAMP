using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace YAMP
{
    public class CompProp_OperationalStock : CompProperties
    {
        public float fuelValueHerbal = 10f;
        public float fuelValueIndustrial = 30f;
        public float fuelValueGlitter = 100f;

        public CompProp_OperationalStock()
        {
            compClass = typeof(OperationalStock);
        }

        public float GetFuelValue(ThingDef def)
        {
            if (def == ThingDefOf.MedicineHerbal)
            {
                return fuelValueHerbal;
            }

            if (def == ThingDefOf.MedicineIndustrial)
            {
                return fuelValueIndustrial;
            }

            if (def == ThingDefOf.MedicineUltratech)
            {
                return fuelValueGlitter;
            }

            Logger.Log("Info", $"YAMP: Did not parse as medicine: {def.defName}");
            return -1f;
        }
    }

    public class OperationalStock : ThingComp
    {
        public CompProp_OperationalStock Props => (CompProp_OperationalStock)props;

        private Comp_PodContainer _podConatiner;
        private Comp_PodContainer PodConatiner => _podConatiner ??= parent.GetComp<Comp_PodContainer>();

        private float buffer = 0f;
        private float stock = 0f;

        public float Stock
        {
            get { return stock; }
        }

        public void ComputeStock()
        {
            var availableStock = buffer;
            foreach (var thing in PodConatiner.Get())
            {
                availableStock += Props.GetFuelValue(thing.def) * thing.stackCount;
            }

            stock = availableStock;
        }

        public bool TryConsumeStock(float amount)
        {
            if (stock < amount)
            {
                return false;
            }

            // First, consume from the buffer
            if (buffer >= amount)
            {
                buffer -= amount;
                return true;
            }

            amount -= buffer; // Amount remaining to consume after buffer is depleted
            buffer = 0f;

            var thingsToConsume = PodConatiner.Get()
                .Where(thing => Props.GetFuelValue(thing.def) > 0)
                .OrderBy(thing => Props.GetFuelValue(thing.def))
                .ToList();

            foreach (var thing in thingsToConsume)
            {
                float fuelValuePerItem = Props.GetFuelValue(thing.def);
                float stackFuel = fuelValuePerItem * thing.stackCount;

                if (buffer + stackFuel >= amount)
                {
                    // This stack can satisfy the remaining amount
                    int itemsToConsume = Mathf.CeilToInt(amount / fuelValuePerItem);
                    // Ensure we don't try to consume more than available in the stack
                    itemsToConsume = Mathf.Min(itemsToConsume, thing.stackCount);

                    // Add excess fuel back to buffer before consuming
                    buffer += (stackFuel - (itemsToConsume * fuelValuePerItem)) - amount;

                    // Split off and destroy the consumed items.
                    thing.SplitOff(itemsToConsume).Destroy();

                    return true;
                }

                // Consume the entire stack and continue
                amount -= stackFuel;
                thing.Destroy();
            }

            // This point should not be reached if CanOperate returned true
            return false;
        }

        public override string CompInspectStringExtra()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // 1. Current operational buffer
            sb.AppendLine($"Buffer: {buffer:F1}");

            // 2. How many of each medicine we have
            var itemsInContainer = PodConatiner.Get()
                .GroupBy(thing => thing.def)
                .Select(group => new { Def = group.Key, Count = group.Sum(thing => thing.stackCount) })
                .OrderBy(item => item.Def.label);

            if (itemsInContainer.Where(item => Props.GetFuelValue(item.Def) > 0).Any())
            {
                sb.AppendLine("Stock Items:");
                foreach (var item in itemsInContainer)
                {
                    sb.AppendLine($"  - {item.Def.LabelCap}: {item.Count}");
                }
            }
            else
            {
                sb.AppendLine("Stock Items: None");
            }

            // 3. Total stock
            sb.AppendLine($"Stock: {stock:F1}");

            return sb.ToString().TrimEnd();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;

            // TODO: Add fuel filter configuration UI
            // For now, filter accepts all medicine by default

            yield return new Command_Action
            {
                defaultLabel = "Eject Unused",
                defaultDesc = "Eject all unused medicine.",
                icon = TexCommand.ForbidOff,
                action = () =>
                {
                    if (PodConatiner != null)
                    {
                        var items = PodConatiner
                            .Get(); // Get a list to avoid issues if items are removed during iteration
                        foreach (var item in items)
                        {
                            // Ensure the item is not a pawn and is actually in the container
                            if (item is Pawn)
                            {
                                continue;
                            }

                            PodConatiner.GetDirectlyHeldThings().TryDrop(item, parent.Position, parent.Map,
                                ThingPlaceMode.Near, out _);
                        }
                    }
                }
            };
        }
    }
}
