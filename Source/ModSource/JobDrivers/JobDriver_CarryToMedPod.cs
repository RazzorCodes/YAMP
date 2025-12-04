using System.Collections.Generic;
using System.Data;
using RimWorld;
using Verse;
using Verse.AI;

namespace YAMP
{
    public class JobDriver_CarryToMedPod : JobDriver
    {
        private const TargetIndex RescuerInd = TargetIndex.A;
        private const TargetIndex PatientInd = TargetIndex.B;
        private const TargetIndex PodInd = TargetIndex.C;

        protected Pawn Patient => (Pawn)job.GetTarget(PatientInd).Thing;
        protected Pawn Rescuer => (Pawn)job.GetTarget(RescuerInd).Thing;
        protected Building_MedPod Pod => (Building_MedPod)job.GetTarget(PodInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return
                pawn.Reserve(
                    Rescuer,
                    job,
                    errorOnFailed: errorOnFailed) &&
                pawn.Reserve(
                    Pod,
                    job,
                    errorOnFailed: errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Logger.Debug("Entered MakeNewToils JobDriver_CarryToMedPod");
            Logger.Debug($"Patient: {Rescuer}");
            Logger.Debug($"Pod: {Pod}");

            this.FailOnDestroyedOrNull(PatientInd);
            this.FailOnDestroyedOrNull(PodInd);
            this.FailOnAggroMentalStateAndHostile(PatientInd);

            // 1. Go to patient
            var gotoPatient =
                Toils_Goto.GotoThing(PatientInd, PathEndMode.OnCell)
                .FailOnDespawnedNullOrForbidden(PatientInd)
                .FailOnSomeonePhysicallyInteracting(PatientInd)
                .FailOn(() => Pod.Container.GetPawn() != null); // Fail if pod becomes occupied

            // 2. Pick up patient
            var pickUpPatient = Toils_Haul.StartCarryThing(PatientInd);

            // 3.Go to MedPod
            var gotoPod =
                Toils_Goto.GotoThing(PodInd, PathEndMode.InteractionCell)
                .FailOnDespawnedNullOrForbidden(PodInd)
                .FailOn(() => Pod.Container.GetPawn() != null); // Fail if pod becomes occupied

            // 4. Place patient in MedPod
            var placePatientInPod = new Toil
            {
                initAction = () =>
                {
                    // Check if pod is still empty
                    if (Pod.Container.GetPawn() != null)
                    {
                        Logger.Debug("MedPod is already occupied");
                        return;
                    }

                    // Transfer pawn to container
                    Rescuer.carryTracker.innerContainer.
                        TryTransferToContainer(
                            Patient,
                            Pod.Container.GetDirectlyHeldThings(),
                            1
                        );

                    // Notify components
                    Pod.GetComp<Comp_PodTend>()?.CheckTend();
                    Pod.GetComp<Comp_PodOperate>()?.CheckOperation();

                    Logger.Debug($"Placed {Patient.Name} in MedPod");
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };

            yield return gotoPatient;
            yield return pickUpPatient;
            yield return gotoPod;
            yield return placePatientInPod;
        }
    }
}
