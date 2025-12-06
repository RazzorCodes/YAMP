using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace YAMP
{
    public class JobDriver_EnterMedPod : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);

            yield return Toils_Bed.GotoBed(TargetIndex.A);

            // LayDown(TargetIndex bedOrRestSpot, bool hasBed, bool lookForOtherJobs, bool canSleep = true, bool gainRest = true)
            // lookForOtherJobs = false ensures they don't wake up just to work
            Toil layDown = Toils_LayDown.LayDown(TargetIndex.A, true, false, true, true);

            // Custom loop condition to prevent premature exit
            layDown.AddPreTickAction(delegate
            {
                Building_MedPod medPod = TargetA.Thing as Building_MedPod;
                if (medPod != null && !medPod.IsBrokenDown())
                {
                    if (!HealthAIUtility.ShouldSeekMedPodRest(pawn, medPod))
                    {
                        EndJobWith(JobCondition.Succeeded);
                    }
                }
            });

            yield return layDown;
        }
    }
}
