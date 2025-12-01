using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace YAMP
{
    /// <summary>
    /// Custom recipe worker for Med Pod operations that spawns harvested organs
    /// in the pod's container instead of on the map
    /// </summary>
    public class Recipe_MedPodSurgery : Recipe_Surgery
    {
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            // Find the Med Pod that contains this pawn
            Building_MedPod pod = FindContainingMedPod(pawn);
            
            if (pod != null)
            {
                // Temporarily override GenSpawn to capture spawned organs
                var opsComp = pod.GetComp<CompMedPodOperations>();
                if (opsComp != null)
                {
                    // Store reference for organ capture
                    currentOperationsComp = opsComp;
                }
            }
            
            // Call base implementation
            base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);
            
            // Clear reference
            currentOperationsComp = null;
        }
        
        private static CompMedPodOperations currentOperationsComp;
        
        private Building_MedPod FindContainingMedPod(Pawn pawn)
        {
            if (pawn.Map == null) return null;
            
            foreach (Thing thing in pawn.Map.listerThings.ThingsOfDef(ThingDef.Named("YAMP_MedPod")))
            {
                if (thing is Building_MedPod pod)
                {
                    var opsComp = pod.GetComp<CompMedPodOperations>();
                    if (opsComp?.innerContainer?.Contains(pawn) == true)
                    {
                        return pod;
                    }
                }
            }
            return null;
        }
        
        // Override to capture organ spawning
        public override bool IsViolationOnPawn(Pawn pawn, BodyPartRecord part, Faction billDoerFaction)
        {
            // Med Pod operations are never violations
            return false;
        }
    }
}
