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
            yield return this.SelectPatient();
            yield return this.FuelGizmo();

            foreach (Gizmo debugGizmo in GetDebugGizmos())
                yield return debugGizmo;
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

        private Gizmo SelectPatient()
        {
            Pawn pawn = Container.GetPawn();
            return new Command_Action
            {
                defaultLabel = "Select Patient",
                defaultDesc = "Select the pawn inside the med pod.",
                icon = TexCommand.Draft,
                action = () =>
                {
                    if (pawn != null)
                    {
                        Find.Selector.ClearSelection();
                        Find.Selector.Select(pawn);
                        // Jump camera to the pawn for better UX
                        Find.CameraDriver.JumpToCurrentMapLoc(pawn.Position);
                    }
                }
            };
        }

        private Gizmo FuelGizmo()
        {
            return new Command_Action
            {
                defaultLabel = "Fuel: " + Stock.Buffer.ToString("F0") + "/" + targetFuelLevel.ToString("F0"),
                defaultDesc = "Medicine fuel buffer. Click to set target level.\n\nCurrent: " + Stock.Buffer.ToString("F0") + "\nTarget: " + targetFuelLevel.ToString("F0"),
                icon = TexCommand.ForbidOff,
                action = () =>
                {
                    Find.WindowStack.Add(new Dialog_Slider(val => $"Set target fuel level: {val}", 0, 500, val => targetFuelLevel = val, (int)targetFuelLevel));
                }
            };
        }

    }
}