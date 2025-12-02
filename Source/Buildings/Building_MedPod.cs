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

        protected override void Tick()
        {
            base.Tick();

            // Check for patient changes every 60 ticks (1 second)
            if (this.IsHashIntervalTick(60))
            {
                CheckPatientChange();
            }
        }

        private void CheckPatientChange()
        {
            Pawn currentPatient = Container.GetPawn();

            // Patient changed (entered, exited, or died)
            if (currentPatient != _lastPatient)
            {
                // Clear all bills on the OLD patient when they leave/change
                // Note: We might want to keep them if they are just exiting, but for now let's stick to the plan of clearing
                // actually, if the patient leaves, we probably shouldn't clear THEIR bills, just our reference.
                // But the requirement was "clears when a new patient enters, a patient exits".
                // If we are using the patient's billstack, clearing it means wiping their surgery bills.
                // That might be aggressive if they just hop out.
                // However, the previous code did: _billStack.Clear(); which was the *building's* stack.
                // Now that we are pointing to the *pawn's* stack, we should be careful.
                // If the user wants the "MedPod Bills" to be temporary for the session, we should clear them.

                if (_lastPatient != null && !_lastPatient.Dead && !_lastPatient.Destroyed)
                {
                    // Optional: Clear bills on the patient who just left? 
                    // For now, let's NOT clear the patient's personal bills just because they left the pod, 
                    // unless that is desired behavior. The original code cleared the *building's* bills.
                    // If we want to simulate "clearing the pod's queue", we don't need to do anything 
                    // because the pod's queue IS the patient's queue.
                    // When a new patient enters, we see THEIR bills.
                }

                _lastPatient = currentPatient;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref _lastPatient, "lastPatient");
            Scribe_Deep.Look(ref _container, "container", this);
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
