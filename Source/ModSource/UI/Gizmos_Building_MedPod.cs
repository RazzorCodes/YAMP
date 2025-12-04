using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace YAMP
{
    public partial class Building_MedPod
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;

            yield return this.EjectProducts();
            yield return this.FuelGizmo();

            foreach (Gizmo debugGizmo in GetDebugGizmos())
                yield return debugGizmo;
        }

        private Gizmo EjectProducts()
        {
            return new Command_Action
            {
                defaultLabel = "Eject Contents",
                defaultDesc = "Eject everything from the internal storage (medicine/fuel).",
                icon = TexCommand.ForbidOff,
                action = () =>
                {
                    // Eject everything from container (medicine)
                    Container.GetDirectlyHeldThings().TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
                }
            };
        }

        public void DischargePatient(Pawn pawn)
        {
            if (pawn == null) return;

            // For a bed, discharging means making them stand up / leave
            // If they are sleeping, wake them up
            if (pawn.CurJobDef == JobDefOf.LayDown)
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }

            // If we want to force them out of the bed slot
            pawn.Position = this.InteractionCell; // Move to interaction cell
            pawn.Notify_Teleported();
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