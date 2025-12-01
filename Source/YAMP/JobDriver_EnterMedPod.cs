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
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return new Toil
            {
                initAction = () =>
                {
                    Building pod = (Building)job.targetA.Thing;
                    CompMedPodOperations ops = pod.TryGetComp<CompMedPodOperations>();
                    if (ops != null)
                    {
                        pawn.DeSpawn();
                        ops.innerContainer.TryAdd(pawn);
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
