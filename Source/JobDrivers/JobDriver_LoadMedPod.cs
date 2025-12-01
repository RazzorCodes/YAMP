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
        protected Building Pod => (Building)job.GetTarget(PodInd).Thing;

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
                    if (Item.def.IsMedicine)
                    {
                        OperationalStock operationalStock = Pod.TryGetComp<OperationalStock>();
                        Comp_PodContainer podContainer = Pod.TryGetComp<Comp_PodContainer>();
                        // Put in fuel container
                        if (operationalStock != null && podContainer != null)
                        {
                            pawn.carryTracker.TryDropCarriedThing(Pod.Position, ThingPlaceMode.Direct,
                                out Thing droppedItem);
                            if (droppedItem != null)
                            {
                                droppedItem.DeSpawn();
                                podContainer.GetDirectlyHeldThings().TryAdd(droppedItem);
                                operationalStock.ComputeStock();
                            }
                        }
                    }
                    else
                    {
                        Comp_PodContainer podContainer = Pod.TryGetComp<Comp_PodContainer>();
                        // Put in operations container
                        if (podContainer != null)
                        {
                            pawn.carryTracker.TryDropCarriedThing(Pod.Position, ThingPlaceMode.Direct,
                                out Thing droppedItem);
                            if (droppedItem != null)
                            {
                                droppedItem.DeSpawn();
                                podContainer.GetDirectlyHeldThings().TryAdd(droppedItem);
                            }
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}
