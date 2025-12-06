using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace YAMP
{
    [HarmonyPatch(typeof(WorkGiver_DoBill), "JobOnThing")]
    public static class HarmonyPatch_WorkGiver_DoBill
    {
        public static void Postfix(ref Job __result, Pawn pawn, Thing thing)
        {
            if (__result == null) return;

            if (thing is Pawn patient)
            {
                if (patient.CurrentBed() is Building_MedPod)
                {
                    // Prevent doctors from performing operations/bills on pawns in the MedPod
                    // The MedPod itself handles operations via its own mechanics
                    __result = null;
                }
            }
        }
    }
}
