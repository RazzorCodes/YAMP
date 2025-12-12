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
        /// Checks all conditional operations for pawns of this pod's faction on the map
        /// </summary>
        public void CheckConditionals()
        {
            var medPod = parent as Building_MedPod;
            if (medPod == null) return;

            var map = medPod.Map;
            if (map == null) return;

            var faction = medPod.Faction;
            if (faction == null) return;

            var pawns = map.mapPawns?.SpawnedPawnsInFaction(faction);
            if (pawns == null || pawns.Count == 0) return;

            foreach (var pawn in pawns)
            {
                Manager.CheckAndEnqueueOperations(pawn);
            }
        }
    }
}
