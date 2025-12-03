using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace YAMP
{
    public class Building_MedPod : Building, IThingHolder, IOpenable, ISearchableContents
    {
        private PodContainer _container;
        public PodContainer Container { get { if (_container == null) _container = new PodContainer(this); return _container; } }

        private OperationalStock _operationalStock;
        public OperationalStock OperationalStock =>
            _operationalStock ??= new OperationalStock(
                Container,
                new CompProp_OperationalStock()
            );

        // BillStack for managing surgery bills - delegates to patient
        public BillStack BillStack
        {
            get
            {
                return Container.GetPawn()?.health.surgeryBills;
            }
        }

        // Track the last patient to detect changes
        private Pawn _lastPatient;

        public bool CanOpen => Container.GetPawn() != null;

        public int OpenTicks => 10;
        public ThingOwner SearchableContents => Container.GetDirectlyHeldThings();

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

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;

            yield return this.EjectProducts();
        }

        private Gizmo EjectProducts()
        {
            return new Command_Action
            {
                defaultLabel = "Eject Products",
                defaultDesc = "Eject the products from the pod.",
                icon = TexCommand.ForbidOff,
                action = () =>
                {
                    var products = Container.GetDirectlyHeldThings().Where(t => !(t is Pawn) && !t.def.IsMedicine).ToList();
                    foreach (var product in products)
                    {
                        Container.GetDirectlyHeldThings().TryDrop(
                                product,
                                this.InteractionCell,
                                this.Map,
                                ThingPlaceMode.Near,
                                out _
                            );
                    }
                }
            };
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, (IList<Thing>)this.GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings()
        {
            return Container.GetDirectlyHeldThings();
        }

        public void Open()
        {
            var pawn = Container.GetPawn();
            if (pawn != null)
            {
                Container.GetDirectlyHeldThings().TryDrop(
                        pawn,
                        this.InteractionCell,
                        this.Map,
                        ThingPlaceMode.Near,
                        out _
                    );
            }
        }
    }
}
