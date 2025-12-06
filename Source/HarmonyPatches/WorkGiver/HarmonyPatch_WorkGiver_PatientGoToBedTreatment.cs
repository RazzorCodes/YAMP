using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;

namespace YAMP
{
    [HarmonyPatch(typeof(WorkGiver_PatientGoToBedTreatment), "AnyAvailableDoctorFor")]
    public static class HarmonyPatch_WorkGiver_PatientGoToBedTreatment
    {
        public static void Postfix(ref bool __result, Pawn pawn)
        {
            if (__result) return; // If true, a doctor is already available.

            // Check if there is a functional MedPod on the map
            // We check for definition and power
            if (pawn.Map != null)
            {
                var medPods = pawn.Map.listerThings.ThingsOfDef(InternalDefOf.YAMP_MedPod);
                foreach (Thing t in medPods)
                {
                    Building_MedPod medPod = t as Building_MedPod;
                    if (medPod != null && !medPod.IsBrokenDown())
                    {
                        var powerComp = medPod.GetComp<CompPowerTrader>();
                        if (powerComp != null && powerComp.PowerOn)
                        {
                            __result = true;
                            return;
                        }
                    }
                }
            }
        }
    }
}
