using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace YAMP
{
    [StaticConstructorOnStartup]
    public static class YAMP_Assets
    {
        public static Material ProgressBarFilledMatYellow;
        public static Material ProgressBarFilledMatGreen;
        public static Material ProgressBarFilledMatRed;
        public static Material ProgressBarUnfilledMat;

        static YAMP_Assets()
        {
            ProgressBarFilledMatYellow = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.475f, 0.1f));
            ProgressBarFilledMatRed = SolidColorMaterials.SimpleSolidColorMaterial(new Color(1f, 0.1f, 0.1f));
            ProgressBarFilledMatGreen = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.1f, 1f, 0.1f));
            ProgressBarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.15f, 0.15f));
        }
    }
}
