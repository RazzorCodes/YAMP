using RimWorld;
using Verse;
using YAMP.ConditionalOperations;

namespace YAMP
{
    public class CompProp_PodConditionals : CompProperties
    {
        public CompProp_PodConditionals()
        {
            compClass = typeof(Comp_PodConditionals);
        }
    }

    /// <summary>
    /// Component that manages conditional operations and checks them periodically
    /// </summary>
    public class Comp_PodConditionals : ThingComp
    {
        private ConditionalOperationManager _manager;
        public ConditionalOperationManager Manager => _manager ??= new ConditionalOperationManager();

        public CompProp_PodConditionals Props => (CompProp_PodConditionals)props;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref _manager, "conditionalOperationManager");

            if (Scribe.mode == LoadSaveMode.LoadingVars && _manager == null)
            {
                _manager = new ConditionalOperationManager();
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            CheckConditionals();
        }

        /// <summary>
        /// Checks all conditional operations for the current patient
        /// </summary>
        public void CheckConditionals()
        {
            var medPod = parent as Building_MedPod;
            if (medPod == null) return;

            var patient = medPod.GetCurOccupant(0);
            if (patient == null) return;

            // Check and enqueue operations
            Manager.CheckAndEnqueueOperations(patient);
        }
    }
}
