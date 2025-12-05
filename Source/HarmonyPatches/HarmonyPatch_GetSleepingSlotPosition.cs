using HarmonyLib;
using RimWorld;
using Verse;

#nullable disable
namespace YAMP;

[HarmonyPatch(typeof(Building_Bed), "GetSleepingSlotPos")]
public static class HarmonyPatch_GetSleepingSlotPos
{
    [HarmonyPostfix]
    public static void Postfix(ref IntVec3 __result, ref Building_Bed __instance, int index)
    {
        Logger.Debug($"GetSleepingSlotPosition for {__instance.def.defName}, slot {index}");
        if (__instance.def.thingClass != typeof(Building_MedPod))
        {
            return;
        }

        __result = __instance.Position;
    }
}