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
        
        // Define the offset relative to the building's local space
        IntVec3 offset = new IntVec3(1, 0, 1);
        
        // Rotate the offset according to the building's rotation
        IntVec3 rotatedOffset = offset.RotatedBy(__instance.Rotation);
        
        // Add the rotated offset to the building's position
        __result = __instance.Position + rotatedOffset;
    }
}