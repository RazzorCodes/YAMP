using HarmonyLib;
using RimWorld;
using Verse;

namespace YAMP
{
    [HarmonyPatch(typeof(WorkGiver_Tend), "HasJobOnThing")]
    public static class WorkGiver_Tend_HasJobOnThing_IgnoreMedPods
    {
        public static void Postfix(ref bool __result, Thing t)
        {
            if (__result == false) return; // If already false, no need to check

            if (t is Pawn patient)
            {
                if (patient.CurrentBed() is Building_MedPod medPod)
                {
                    __result = false;
                }
            }
        }
    }
}
