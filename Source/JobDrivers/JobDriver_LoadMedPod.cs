using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace YAMP
{
    public class JobDriver_LoadMedPod : JobDriver
    {
        private const TargetIndex ItemInd = TargetIndex.A;
        private const TargetIndex PodInd = TargetIndex.B;

        protected Thing Item => job.GetTarget(ItemInd).Thing;
        protected Building_MedPod Pod => (Building_MedPod)job.GetTarget(PodInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Item, job, 1, -1, null, errorOnFailed) &&
                   pawn.Reserve(Pod, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // 1. Go to Item
            yield return Toils_Goto.GotoThing(ItemInd, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(ItemInd)
                .FailOnSomeonePhysicallyInteracting(ItemInd);

            // 2. Pick up Item
            yield return Toils_Haul.StartCarryThing(ItemInd, false, false, false);

            // 3. Go to Pod
            yield return Toils_Goto.GotoThing(PodInd, PathEndMode.Touch);

            // 4. Deposit Item
            yield return new Toil
            {
                initAction = () =>
                {
                    Thing carriedThing = pawn.carryTracker.CarriedThing;
                    if (carriedThing == null) return;

                    PodContainer podContainer = Pod.Container;
                    if (podContainer == null) return;

                    if (carriedThing.def.IsMedicine)
                    {
                        OperationalStock operationalStock = Pod.Stock;
                        if (operationalStock != null)
                        {
                            // Transfer directly to container
                            pawn.carryTracker.innerContainer.TryTransferToContainer(carriedThing, podContainer.GetDirectlyHeldThings(), carriedThing.stackCount);
                            operationalStock.ComputeStock();
                            
                            // Notify components that medicine was dropped
                            Pod.GetComp<Comp_PodTend>()?.CheckTend();
                            Pod.GetComp<Comp_PodOperate>()?.CheckOperation();
                        }
                    }
                    else
                    {
                        // Transfer directly to container
                        pawn.carryTracker.innerContainer.TryTransferToContainer(carriedThing, podContainer.GetDirectlyHeldThings(), carriedThing.stackCount);
                        
                        // Notify components that items were dropped
                        Pod.GetComp<Comp_PodOperate>()?.CheckOperation();
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
