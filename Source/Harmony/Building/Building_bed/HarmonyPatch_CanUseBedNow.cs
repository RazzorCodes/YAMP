using HarmonyLib;
using RimWorld;
using Verse;

#nullable disable
namespace YAMP;

[HarmonyPatch(typeof(RestUtility), "CanUseBedNow")]
public static class HarmonyPatch_CanUseBedNow
{
    public static void Postfix(bool __result, Thing bedThing, Pawn sleeper)
    {
        if (bedThing is Building_MedPod bedMedPod)
        {
            __result = YAMP.HealthAIUtility.ShouldSeekMedPodRest(sleeper, bedMedPod);
        }
    }
}
