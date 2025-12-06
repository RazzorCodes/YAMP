using RimWorld;
using UnityEngine;
using Verse;

namespace YAMP
{
    public class CompProp_ProgressBar : CompProperties
    {
        public CompProp_ProgressBar() => compClass = typeof(Comp_ProgressBar);
    }

    public class Comp_ProgressBar : ThingComp
    {
        private static readonly Vector2 BarSize = new Vector2(0.7f, 0.12f);  // Vanilla solar-like size

        private static Material _filledMatGreen;
        private static Material _filledMatYellow;
        private static Material _filledMatRed;
        private static Material _unfilledMat;

        static Comp_ProgressBar()
        {
            _filledMatGreen = YAMP_Assets.ProgressBarFilledMatGreen;
            _filledMatYellow = YAMP_Assets.ProgressBarFilledMatYellow;
            _filledMatRed = YAMP_Assets.ProgressBarFilledMatRed;
            _unfilledMat = YAMP_Assets.ProgressBarUnfilledMat;
        }

        public override void PostDraw()
        {
            base.PostDraw();
            if (_filledMatYellow == null || _filledMatGreen == null || _unfilledMat == null)
            {
                Logger.Warning("Comp_ProgressBar.PostDraw: Shader fail safeguard");
                return;
            }

            var operateComp = parent.GetComp<Comp_PodOperate>();
            if (operateComp?.Progress > 0f)
            {
                DrawBar(_filledMatYellow, operateComp.Progress, Vector3.up * 0.1f + Vector3.forward * 0.3f);  // Lower bar
            }

            var tendComp = parent.GetComp<Comp_PodTend>();
            if (tendComp?.Progress > 0f)
            {
                DrawBar(_filledMatGreen, tendComp.Progress, Vector3.up * 0.3f + Vector3.forward * 0.3f);  // Upper bar
            }
        }

        private void DrawBar(Material filledMat, float fillPercent, Vector3 offset)
        {
            GenDraw.FillableBarRequest r = default;
            r.center = parent.DrawPos + offset;
            r.size = BarSize;
            r.fillPercent = fillPercent;
            r.filledMat = filledMat;
            r.unfilledMat = _unfilledMat;
            r.margin = 0.15f;
            r.rotation = parent.Rotation;
            GenDraw.DrawFillableBar(r);
        }
    }
}