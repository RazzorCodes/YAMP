using HarmonyLib;
using RimWorld;
using Verse;

namespace YAMP
{
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            var harmony = new Harmony("YAMP");
            harmony.PatchAll();
            Logger.Info("YAMP Harmony patches applied successfully.");
        }
    }
}