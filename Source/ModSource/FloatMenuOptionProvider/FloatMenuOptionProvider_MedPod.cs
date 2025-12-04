using System;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

#nullable disable
namespace YAMP;

public class FloatMenuOptionProvider_MedPod : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool RequiresManipulation => true;

    protected override FloatMenuOption GetSingleOptionFor(Pawn clickedPawn, FloatMenuContext context)
    {
        if (!clickedPawn.IsPrisonerOfColony && !clickedPawn.IsColonist || clickedPawn == context.FirstSelectedPawn)
        {
            return null;
        }

        var medPod = clickedPawn.Map.spawnedThings.FirstOrDefault(p => p is Building_MedPod);
        if (medPod == null)
        {
            return null;
        }
        var taker = context.FirstSelectedPawn;
        if (taker == null)
        {
            return null;
        }

        var menuOption = new FloatMenuOption($"Take {clickedPawn.Name} to med pod", (Action)(() =>
        {
            Job job = JobMaker.MakeJob(
                InternalDefOf.YAMP_CarryToMedPod,
                taker,
                clickedPawn,
                medPod);
            job.count = 1;
            if (!taker.jobs.TryTakeOrderedJob(job))
            {
                Logger.Error($"Failed to take job {job.def.defName} for {taker.Name}");
            }
        }));

        return menuOption;
    }
}
