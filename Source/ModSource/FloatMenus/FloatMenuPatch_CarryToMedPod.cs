using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace YAMP
{
    /// <summary>
    /// Provides "Carry to MedPod" float menu option when right-clicking downed pawns or prisoners
    /// </summary>
    public static class FloatMenuPatch_CarryToMedPod
    {
        public static FloatMenuOption AddCarryToMedPodOption(Pawn pawn, Pawn targetPawn)
        {
            string label = "";
            Action action = null;

            // Only show for downed pawns or prisoners
            if (!targetPawn.Downed && !targetPawn.IsPrisonerOfColony)
            {
                return null;
            }

            // Don't show if target is the selector themselves
            if (pawn == targetPawn)
            {
                return null;
            }

            // Find available MedPods
            Building_MedPod bestPod = FindBestMedPod(pawn, targetPawn);

            if (bestPod == null)
            {
                label = "CarryToMedPod".Translate(targetPawn.LabelShort) + " (" + "NoEmptyMedPod".Translate() + ")";
                return new FloatMenuOption(label, null);
            }

            if (!pawn.CanReach(targetPawn, PathEndMode.ClosestTouch, Danger.Deadly))
            {
                label = "CarryToMedPod".Translate(targetPawn.LabelShort) + " (" + "NoPath".Translate().CapitalizeFirst() + ")";
                return new FloatMenuOption(label, null);
            }

            label = "CarryToMedPod".Translate(targetPawn.LabelShort);
            Building_MedPod pod = bestPod;

            action = () =>
            {
                Job job = JobMaker.MakeJob(
                    DefDatabase<JobDef>.GetNamed("YAMP_CarryToMedPod"),
                    targetPawn,
                    pod
                );
                job.count = 1;
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            };

            return FloatMenuUtility.DecoratePrioritizedTask(
                new FloatMenuOption(label, action),
                pawn,
                targetPawn
            );
        }

        private static Building_MedPod FindBestMedPod(Pawn hauler, Pawn patient)
        {
            if (hauler?.Map == null) return null;

            // Find all MedPods on map
            List<Building_MedPod> medPods = hauler.Map.listerBuildings
                .AllBuildingsColonistOfClass<Building_MedPod>()
                .ToList();

            Building_MedPod bestPod = null;
            float bestDist = float.MaxValue;

            foreach (Building_MedPod pod in medPods)
            {
                // Must be empty
                if (pod.Container?.GetPawn() != null) continue;

                // Must be reachable
                if (!hauler.CanReach(pod, PathEndMode.InteractionCell, Danger.Deadly)) continue;

                // Check if patient can be reserved
                if (!hauler.CanReserve(patient)) continue;
                if (!hauler.CanReserve(pod)) continue;

                float dist = (hauler.Position - pod.Position).LengthHorizontalSquared;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestPod = pod;
                }
            }

            return bestPod;
        }
    }
}
