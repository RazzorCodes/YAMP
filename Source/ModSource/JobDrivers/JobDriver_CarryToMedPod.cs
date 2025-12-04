using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace YAMP
{
    public class JobDriver_CarryToMedPod : JobDriver
    {
        private const TargetIndex PatientInd = TargetIndex.A;
        private const TargetIndex PodInd = TargetIndex.B;

        protected Pawn Patient => (Pawn)job.GetTarget(PatientInd).Thing;
        protected Building_MedPod Pod => (Building_MedPod)job.GetTarget(PodInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed) &&
                   pawn.Reserve(Pod, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(PatientInd);
            this.FailOnDestroyedOrNull(PodInd);
            this.FailOnAggroMentalStateAndHostile(PatientInd);

            // 1. Go to patient
            yield return Toils_Goto.GotoThing(PatientInd, PathEndMode.ClosestTouch)
                .FailOnDespawnedNullOrForbidden(PatientInd)
                .FailOn(() => Pod.Container.GetPawn() != null); // Fail if pod becomes occupied

            // 2. Pick up patient
            yield return Toils_Haul.StartCarryThing(PatientInd);

            // 3. Go to MedPod
            yield return Toils_Goto.GotoThing(PodInd, PathEndMode.InteractionCell);

            // 4. Place patient in MedPod
            yield return new Toil
            {
                initAction = () =>
                {
                    Pawn carriedPawn = (Pawn)pawn.carryTracker.CarriedThing;
                    if (carriedPawn == null) return;

                    PodContainer podContainer = Pod.Container;
                    if (podContainer == null) return;

                    // Check if pod is still empty
                    if (podContainer.GetPawn() != null)
                    {
                        Logger.Warning("MedPod is already occupied");
                        return;
                    }

                    // Transfer pawn to container
                    pawn.carryTracker.innerContainer.TryTransferToContainer(
                        carriedPawn,
                        podContainer.GetDirectlyHeldThings(),
                        1
                    );

                    // Notify components
                    Pod.GetComp<Comp_PodTend>()?.CheckTend();
                    Pod.GetComp<Comp_PodOperate>()?.CheckOperation();

                    Logger.Debug($"Placed {carriedPawn.Name} in MedPod");
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
