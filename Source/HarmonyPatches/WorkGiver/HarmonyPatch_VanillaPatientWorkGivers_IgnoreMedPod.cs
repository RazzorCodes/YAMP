using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace YAMP
{
    // Patches the base class for patient bed seeking (Recuperate).
    // This also covers Treatment and EmergencyTreatment because they call base.NonScanJob.
    [HarmonyPatch(typeof(WorkGiver_PatientGoToBedRecuperate), "NonScanJob")]
    public static class HarmonyPatch_VanillaPatientWorkGivers_IgnoreMedPod
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            // If vanilla logic didn't give a job, we don't care.
            if (__result == null) return;

            // If the job targets a MedPod, we cancel it.
            // We want our custom WorkGiver_EnterMedPod to handle this.
            if (__result.targetA.Thing is Building_MedPod)
            {
                __result = null;
            }
        }
    }
}
