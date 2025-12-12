using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace YAMP
{
    /// <summary>
    /// Contents tab for the MedPod, mirroring the vanilla storage UI.
    /// </summary>
    public class ITab_MedPodContents : ITab_ContentsBase
    {
        private static readonly List<Thing> EmptyThings = new();

        private Building_MedPod SelMedPod => SelThing as Building_MedPod;

        public ITab_MedPodContents()
        {
            labelKey = "TabCasketContents";
            containedItemsKey = "TabCasketContents";
        }

        public override bool IsVisible => SelMedPod != null && base.IsVisible;

        public override IList<Thing> container
        {
            get
            {
                Building_MedPod medPod = SelMedPod;
                if (medPod?.Container == null)
                {
                    return EmptyThings;
                }

                ThingOwner things = medPod.Container.GetDirectlyHeldThings();
                return things?.ToList() ?? EmptyThings;
            }
        }

        protected override void OnDropThing(Thing t, int count)
        {
            base.OnDropThing(t, count);

            Building_MedPod medPod = SelMedPod;
            if (medPod == null)
            {
                return;
            }

            medPod.Stock?.ComputeStock();
            medPod.GetComp<Comp_PodTend>()?.CheckTend();
            medPod.GetComp<Comp_PodOperate>()?.CheckOperation();
            medPod.GetComp<Comp_PodConditionals>()?.CheckConditionals();
        }
    }
}
