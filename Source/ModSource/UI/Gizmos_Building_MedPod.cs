using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace YAMP
{
    public partial class Building_MedPod
    {
        public bool CanOpen => Container.GetPawn() != null;
        public int OpenTicks => 10;
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

    }
}