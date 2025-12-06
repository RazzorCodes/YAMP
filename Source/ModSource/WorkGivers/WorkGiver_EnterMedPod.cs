using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace YAMP
{
    public class WorkGiver_EnterMedPod : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerThings.ThingsOfDef(InternalDefOf.YAMP_MedPod);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Building_MedPod medPod = t as Building_MedPod;
            if (medPod == null) return false;

            if (medPod.IsForbidden(pawn)) return false;

            if (!pawn.CanReserve(medPod, 1, -1, null, forced)) return false;

            // Check if MedPod is usable (not occupied, etc)
            if (medPod.GetCurOccupant(0) != null) return false;

            // Health checks - Check if we SHOULD utilize it
            return HealthAIUtility.ShouldSeekMedPodRest(pawn, medPod);
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(InternalDefOf.YAMP_EnterMedPod, t);
        }
    }
}
