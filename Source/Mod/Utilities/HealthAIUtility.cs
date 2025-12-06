namespace YAMP;
using RimWorld;
using Verse;

public static class HealthAIUtility
{
    public static bool ShouldSeekMedPodRest(Pawn pawn, Building_MedPod medPod)
    {
        var hediffs = pawn.health.hediffSet.hediffs;

        //bool downed = pawn.Downed && !LifeStageUtility.AlwaysDowned(pawn);
        bool hasTendable = hediffs.Any<Hediff>(x => x.Visible && x.TendableNow());
        bool hasQueuedOperation = pawn.health.surgeryBills.AnyShouldDoNow;
        bool shouldDoConditional = medPod.GetComp<Comp_PodConditionals>().Manager.ShouldEnqueueOperation(pawn);

        return hasTendable || hasQueuedOperation || shouldDoConditional;
    }
}