using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
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

    [HarmonyPatch(typeof(WorkGiver_Tend), "HasJobOnThing")]
    public static class WorkGiver_Tend_HasJobOnThing_Patch
    {
        public static void Postfix(Pawn pawn, Thing t, bool forced, ref bool __result)
        {
            if (!__result) return;
            if (t is Pawn patient && patient.CurrentBed() is Building_MedPod medPod)
            {
                if (medPod.TryGetComp<CompPowerTrader>() is CompPowerTrader powerComp && !powerComp.PowerOn)
                {
                    __result = false;
                    JobFailReason.Is("NoPower".Translate());
                }
            }
        }
    }

    [HarmonyPatch(typeof(WorkGiver_DoBill), "HasJobOnThingForBill")]
    public static class WorkGiver_DoBill_HasJobOnThingForBill_Patch
    {
        public static void Postfix(Pawn pawn, IBillGiver giver, ref bool __result)
        {
            if (!__result) return;
            if (giver is Building_MedPod medPod && medPod.TryGetComp<CompPowerTrader>() is CompPowerTrader powerComp && !powerComp.PowerOn)
            {
                __result = false;
                JobFailReason.Is("NoPower".Translate());
            }
        }
    }

    [HarmonyPatch(typeof(RestUtility), "IsValidBedFor")]
    public static class RestUtility_IsValidBedFor_Patch
    {
        public static void Postfix(Thing bedThing, Pawn sleeper, ref bool __result)
        {
            if (!__result) return;
            if (bedThing is Building_MedPod medPod)
            {
                if (medPod.TryGetComp<CompPowerTrader>() is CompPowerTrader powerComp && !powerComp.PowerOn)
                {
                    __result = false;
                }
            }
        }
    }
}
