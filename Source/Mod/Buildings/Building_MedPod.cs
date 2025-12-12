using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;

namespace YAMP
{
    public partial class Building_MedPod : Building_Bed, IThingHolder, IStoreSettingsParent
    {
        private PodContainer _container;
        public PodContainer Container => _container ??= new PodContainer(this);

        private OperationalStock _operationalStock;
        public OperationalStock Stock => _operationalStock ??= new OperationalStock(Container);

        private StorageSettings _storageSettings;
        public float targetFuelLevel = 200f;

        public BillStack BillStack => GetCurOccupant(0)?.health.surgeryBills;

        // Override to prevent forced blue medical bed color
        public override Color DrawColor => Color.white;

        // Power component
        public new CompPowerTrader PowerComp;

        // IStoreSettingsParent implementation
        public StorageSettings GetStoreSettings() => _storageSettings;
        public StorageSettings GetParentStoreSettings() => def.building.fixedStorageSettings;
        public bool StorageTabVisible => true;
        public void Notify_SettingsChanged()
        {
            // Recompute stock when storage settings change
            Stock.ComputeStock();
        }


        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            // Initialize storage settings
            if (_storageSettings == null)
            {
                _storageSettings = new StorageSettings(this);
                if (def.building.defaultStorageSettings != null)
                {
                    _storageSettings.CopyFrom(def.building.defaultStorageSettings);
                }
                else
                {
                    // Default: accept all medicine
                    _storageSettings.filter.SetAllow(ThingCategoryDefOf.Medicine, true);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref _container, "container", this);
            Scribe_Deep.Look(ref _operationalStock, "operationalStock", this);
            Scribe_Deep.Look(ref _storageSettings, "storageSettings", this);
            Scribe_Values.Look(ref targetFuelLevel, "targetFuelLevel", 200f);
            Scribe_Collections.Look(ref medicineRanges, "medicineRanges", LookMode.Value, LookMode.Value);
        }

        public Dictionary<string, IntRange> medicineRanges = new Dictionary<string, IntRange>();

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            // todo: iterate once
            // Iterate comps to find any that are IThingHolder
            if (AllComps != null)
            {
                for (int i = 0; i < AllComps.Count; i++)
                {
                    if (AllComps[i] is IThingHolder holder)
                    {
                        outChildren.Add(holder);
                    }
                }
            }
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return Container.GetDirectlyHeldThings();
        }

    }
}
