using HarmonyLib;
using RimWorld;
using Verse;

namespace YAMP
{
    [StaticConstructorOnStartup]
    public static class YAMPHarmony
    {
        static YAMPHarmony()
        {
            var harmony = new Harmony("YAMP");
            harmony.PatchAll();
            Logger.Info("YAMP Harmony patches applied successfully.");
        }
    }
}