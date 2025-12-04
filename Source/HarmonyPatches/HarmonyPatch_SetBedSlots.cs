using HarmonyLib;
using RimWorld;

#nullable disable
namespace YAMP;

[HarmonyPatch(typeof(Building_Bed), "SleepingSlotsCount", MethodType.Getter)]
public static class HarmonyPatch_SetBedSlots
{
    [HarmonyPostfix]
    public static void Postfix(ref int __result, ref Building_Bed __instance)
    {
        Logger.Debug("SetBedSlots");
        if (!(__instance.def.thingClass == typeof(Building_MedPod)))
            return;
        __result = 1;
    }
}
