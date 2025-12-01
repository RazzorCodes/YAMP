using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YAMP
{
    public class Building_MedPod : Building
    {
        // No longer implements IBillGiver.
        // Acts as a container and processor.

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
        }
    }
}
