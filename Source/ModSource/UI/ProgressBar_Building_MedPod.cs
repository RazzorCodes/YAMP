using RimWorld;
using UnityEngine;
using Verse;

namespace YAMP
{
    public class CompMedPodProgressBar : ThingComp
    {
        private static readonly Vector2 BarSize = new Vector2(1.5f, 0.14f);

        public override void PostDraw()
        {
            base.PostDraw();
            var operateComp = parent.GetComp<Comp_PodOperate>();
            if (operateComp == null || operateComp.Progress <= 0f)
            {
                return;
            }

            GenDraw.FillableBarRequest r = new GenDraw.FillableBarRequest();
            r.center = this.parent.DrawPos + Vector3.up * 0.1f + Vector3.forward * 0.25f;
            r.size = BarSize;
            r.fillPercent = operateComp.Progress;
            r.filledMat = YAMP_Assets.ProgressBarFilledMat;
            r.unfilledMat = YAMP_Assets.ProgressBarUnfilledMat;
            r.margin = 0.15f;
            Rot4 rotation = this.parent.Rotation;
            rotation.Rotate(RotationDirection.Clockwise);
            r.rotation = rotation;
            GenDraw.DrawFillableBar(r);
        }
    }
}
