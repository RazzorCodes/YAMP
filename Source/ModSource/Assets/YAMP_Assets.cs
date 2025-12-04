using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace YAMP
{
    [StaticConstructorOnStartup]
    public static class YAMP_Assets
    {
        public static Material ProgressBarFilledMat;
        public static Material ProgressBarUnfilledMat;

        static YAMP_Assets()
        {
            ProgressBarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.475f, 0.1f));
            ProgressBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.15f, 0.15f));
        }
    }
}
