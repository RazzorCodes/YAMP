using HarmonyLib;
using RimWorld;
using Verse;

#nullable disable
namespace YAMP;

[HarmonyPatch(typeof(Building_Bed), "GetFootSlotPos")]
public static class HarmonyPatch_GetFootPosition
{
    [HarmonyPostfix]
    public static void Postfix(ref IntVec3 __result, ref Building_Bed __instance, int index)
    {
        Logger.Debug($"GetFootPosition for {__instance.def.defName}, slot {index}");
        if (__instance.def.thingClass != typeof(Building_MedPod))
        {
            return;
        }

        __result = __instance.Position + __instance.Rotation.FacingCell;
    }
}