using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace YAMP
{
    public partial class Building_MedPod : Building, IThingHolder, IOpenable
    {
        private PodContainer _container;
        public PodContainer Container => _container ??= new PodContainer(this);

        private OperationalStock _operationalStock;
        public OperationalStock Stock => _operationalStock ??= new OperationalStock(Container);

        public BillStack BillStack => Container.GetPawn()?.health.surgeryBills;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref _container, "container", this);
            Scribe_Deep.Look(ref _operationalStock, "operationalStock", this);
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return Container.GetDirectlyHeldThings();
        }

    }
}
