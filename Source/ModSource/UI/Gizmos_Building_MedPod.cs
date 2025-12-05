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
            return new Gizmo_SetLevel
            {
                // Core value accessors
                GetCurrentValue = () => Stock.Buffer,
                GetMaxCapacity = () => YAMP.OperationalStock.MAX_BUFFER,
                GetTargetValue = () => targetFuelLevel,
                SetTargetValue = (val) => targetFuelLevel = val,

                // Display properties
                GizmoTitle = "Medicine Fuel",
                GetBarLabel = () => $"{Stock.Buffer.ToStringDecimalIfSmall()} / {targetFuelLevel.ToStringDecimalIfSmall()}",

                // Behavior
                IsTargetConfigurable = true,

                // Optional: Enable auto-refill toggle if you want
                // ShowAutoRefillToggle = true,
                // GetAutoRefillEnabled = () => someAutoRefillField,
                // SetAutoRefillEnabled = (val) => someAutoRefillField = val,
                // AutoRefillIcon = TexCommand.ForbidOff,
                // GetAutoRefillTooltip = () => "Allow automatic refilling of medicine fuel"
            };
        }

    }
}