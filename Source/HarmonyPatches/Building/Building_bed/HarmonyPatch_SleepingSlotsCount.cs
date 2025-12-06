using HarmonyLib;
using RimWorld;

#nullable disable
namespace YAMP;

[HarmonyPatch(typeof(Building_Bed), "SleepingSlotsCount", MethodType.Getter)]
public static class HarmonyPatch_SleepingSlotsCount
{
    [HarmonyPostfix]
    public static void Postfix(ref int __result, Building_Bed __instance)
    {
        if (__instance.def.thingClass != typeof(Building_MedPod))
        {
            return;
        }
        __result = 1;
    }
}

[HarmonyPatch(typeof(Building_Bed), "TotalSleepingSlots", MethodType.Getter)]
public static class HarmonyPatch_TotalSleepingSlots
{
    [HarmonyPostfix]
    public static void Postfix(ref int __result, Building_Bed __instance)
    {
        if (__instance.def.thingClass != typeof(Building_MedPod))
        {
            return;
        }
        __result = 1;
    }
}
