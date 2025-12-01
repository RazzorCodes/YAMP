using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace YAMP
{
    [StaticConstructorOnStartup]
    public static class YAMP_Assets
    {
        public static Material ActiveOverlayMat;

        static YAMP_Assets()
        {
            // Use a vanilla texture or a placeholder if custom one is missing
            // "Things/Mote/SparkFlash" is usually available.
            // Or just use BaseContent.WhiteTex with a color.
            // Let's try to load the custom one, if fails, fallback.
            
            // Note: ContentFinder<Texture2D>.Get returns null if not found.
            // MaterialPool.MatFrom handles null gracefully? No, it errors.
            
            Texture2D tex = ContentFinder<Texture2D>.Get("Things/Building/MedPod/ActiveOverlay", false);
            if (tex == null)
            {
                tex = ContentFinder<Texture2D>.Get("Things/Mote/SparkFlash", false); // Fallback
            }
            
            if (tex != null)
            {
                ActiveOverlayMat = MaterialPool.MatFrom(tex, ShaderDatabase.Transparent, Color.cyan);
            }
            else
            {
                ActiveOverlayMat = BaseContent.BadMat;
            }
        }
    }
}
