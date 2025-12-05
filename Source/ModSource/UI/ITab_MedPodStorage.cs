using RimWorld;
using UnityEngine;
using Verse;

namespace YAMP
{
    /// <summary>
    /// Storage tab for the med pod, allowing filtering of medicine types.
    /// </summary>
    public class ITab_MedPodStorage : ITab_Storage
    {
        private Building_MedPod SelMedPod => (Building_MedPod)SelThing;

        public ITab_MedPodStorage()
        {
            size = new Vector2(300f, 480f);
            labelKey = "TabStorage";
        }

        protected override IStoreSettingsParent SelStoreSettingsParent => SelMedPod;

        protected override void FillTab()
        {
            // Ensure only medicine can be stored
            var settings = SelMedPod.GetStoreSettings();
            if (settings?.filter != null)
            {
                // Clear all and set only medicine category
                settings.filter.SetDisallowAll();
                settings.filter.SetAllow(ThingCategoryDefOf.Medicine, true);
            }

            base.FillTab();
        }
    }
}
