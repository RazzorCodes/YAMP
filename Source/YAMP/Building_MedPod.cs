using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YAMP
{
    public class Building_MedPod : Building
    {
        // Reverted to standard Building.
        // Patient is stored in CompMedPodOperations.innerContainer.

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }
    }
}
