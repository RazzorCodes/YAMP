using System.Threading;
using HarmonyLib;
using RimWorld;
using Verse;

namespace YAMP;

[HarmonyPatch(typeof(PawnRenderer), "BodyAngle")]
public static class HarmonyPatch_PawnRenderer_BodyAngle
{
    [HarmonyPostfix]
    public static void Postfix(ref float __result, Pawn ___pawn)
    {
        if (___pawn == null) return;

        Building_Bed bed = ___pawn.CurrentBed();
        if (bed is not Building_MedPod medPod)
        {
            return;
        }

        // Set body angle based on MedPod rotation
        Rot4 rotation = medPod.Rotation;
        if (rotation == Rot4.North)
        {
            __result = 0f;
        }
        else if (rotation == Rot4.South)
        {
            __result = 180f;
        }
        else if (rotation == Rot4.East)
        {
            __result = 90f;
        }
        else if (rotation == Rot4.West)
        {
            __result = -90f;
        }
    }
}
