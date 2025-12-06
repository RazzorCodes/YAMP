using HarmonyLib;
using RimWorld;
using Verse;

namespace YAMP;

[HarmonyPatch(typeof(PawnRenderer), "LayingFacing")]
public static class HarmonyPatch_PawnRenderer_LayingFacing
{
    [HarmonyPostfix]
    public static void Postfix(ref Rot4 __result, Pawn ___pawn)
    {
        if (___pawn == null) return;

        Building_Bed bed = ___pawn.CurrentBed();
        if (bed is not Building_MedPod medPod)
        {
            return;
        }

        if (___pawn.RaceProps.Humanlike)
        {
            __result = Rot4.South;
        }
        else
        {
            __result = medPod.Rotation == Rot4.West ? Rot4.East : Rot4.West;
        }
    }
}
