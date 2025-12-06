using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace YAMP;

[HarmonyPatch(typeof(PawnRenderer), "GetBodyPos")]
public static class HarmonyPatch_PawnRenderer_GetBodyPos
{
    [HarmonyPostfix]
    public static void Postfix(ref Vector3 __result, Pawn ___pawn)
    {
        if (___pawn == null) return;

        Building_Bed bed = ___pawn.CurrentBed();
        if (bed is not Building_MedPod medPod)
        {
            return;
        }

        // MedPod is 2x2. DrawPos gives the exact center of the building (X and Z).
        // We use that to center the pawn.
        Vector3 center = medPod.DrawPos;

        // Preserve the Y altitude calculated by vanilla (important for layering)
        // Only override X and Z.
        __result.x = center.x;
        __result.z = center.z;
    }
}
