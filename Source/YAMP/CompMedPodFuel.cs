using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace YAMP
{
    public class CompProperties_MedPodFuel : CompProperties
    {
        public float fuelCapacity = 100f; // Max stock
        public float stockGainRate = 0.1f; // Stock gained per tick from buffer
        
        // Fuel Values (Stock amount per item)
        public float fuelValueHerbal = 10f;
        public float fuelValueIndustrial = 30f;
        public float fuelValueGlitter = 100f;
        public float fuelValueDefault = 20f;

        public CompProperties_MedPodFuel()
        {
            compClass = typeof(CompMedPodFuel);
        }
    }

    public class CompMedPodFuel : ThingComp, IThingHolder
    {
        public float stock; // Available stock for operations
        public float stockBuffer; // Stock currently being "processed" (burning)
        public ThingOwner innerContainer; // Stores the medicine waiting to be burnt
        public ThingFilter fuelFilter; // Filter for allowed fuel types

        public CompProperties_MedPodFuel Props => (CompProperties_MedPodFuel)props;

        public float StockPercent => stock / Props.fuelCapacity;

        public CompMedPodFuel()
        {
            innerContainer = new ThingOwner<Thing>(this);
            fuelFilter = new ThingFilter();
            fuelFilter.SetAllow(ThingCategoryDefOf.Medicine, true);
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return innerContainer;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref stock, "stock", 0f);
            Scribe_Values.Look(ref stockBuffer, "stockBuffer", 0f);
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
            Scribe_Deep.Look(ref fuelFilter, "fuelFilter");
            
            if (Scribe.mode == LoadSaveMode.PostLoadInit && fuelFilter == null)
            {
                fuelFilter = new ThingFilter();
                fuelFilter.SetAllow(ThingCategoryDefOf.Medicine, true);
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            
            // Process Buffer -> Stock
            if (stockBuffer > 0)
            {
                float transfer = Mathf.Min(stockBuffer, Props.stockGainRate);
                float space = Props.fuelCapacity - stock;
                if (space > 0)
                {
                    float actualTransfer = Mathf.Min(transfer, space);
                    stock += actualTransfer;
                    stockBuffer -= actualTransfer;
                }
            }

            // Burn new fuel if we have space
            if (innerContainer.Any && (stock + stockBuffer) < Props.fuelCapacity)
            {
                BurnFuel();
            }
        }

        private void BurnFuel()
        {
            Thing fuel = innerContainer[0];
            float value = GetFuelValue(fuel.def);
            
            // Consume 1 item
            fuel.SplitOff(1).Destroy();
            
            // Add to buffer
            stockBuffer += value;
        }

        private float GetFuelValue(ThingDef def)
        {
            if (def == ThingDefOf.MedicineHerbal) return Props.fuelValueHerbal;
            if (def == ThingDefOf.MedicineIndustrial) return Props.fuelValueIndustrial;
            if (def.defName == "MedicineUltratech") return Props.fuelValueGlitter;
            return Props.fuelValueDefault;
        }

        public override string CompInspectStringExtra()
        {
            return $"Stock: {stock:F1}/{Props.fuelCapacity} ({StockPercent:P0})\nProcessing: {stockBuffer:F1}\nFuel Items: {innerContainer.TotalStackCount}";
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo g in base.CompGetGizmosExtra()) yield return g;

            // TODO: Add fuel filter configuration UI
            // For now, filter accepts all medicine by default

            if (innerContainer.Any)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Eject Fuel",
                    defaultDesc = "Eject all unused medicine.",
                    icon = TexCommand.ForbidOff,
                    action = () => innerContainer.TryDropAll(parent.Position, parent.Map, ThingPlaceMode.Near)
                };
            }
        }
    }
}
