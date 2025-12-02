using RimWorld;
using Verse;

namespace YAMP.OperationSystem
{
    // ==================== EXECUTE OPERATIONS ====================

    /// <summary>
    /// Execute pawn by cutting (euthanasia)
    /// </summary>
    public class ExecuteByCutOperation : BaseOperation
    {
        public override string Name => "Execute by Cutting";

        public bool CanExecute(Pawn patient)
        {
            return patient.RaceProps.IsFlesh;
        }

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            // Execute instantly kills the pawn
            context.Patient.Kill(new DamageInfo(DamageDefOf.ExecutionCut, 99999, 999f, -1, null, null));
            Logger.Log("YAMP", $"Executed {context.Patient.LabelShort}");
        }

        protected override void HandleFailure(OperationContext context, OperationResult result)
        {
            result.FailureReason = "Execution failed, pawn survived but is severely wounded";
            // Severe damage but not lethal
            context.Patient.TakeDamage(new DamageInfo(DamageDefOf.Cut, 50, 5f, -1, null, null));
        }
    }

    /// <summary>
    /// Terminate pregnancy - removes pregnancy hediff
    /// </summary>
    public class TerminatePregnancyOperation : BaseOperation
    {
        public override string Name => "Terminate Pregnancy";

        public bool CanExecute(Pawn patient)
        {
            return patient.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnantHuman) != null;
        }

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            // Remove pregnancy hediff
            var pregnancyHediff = context.Patient.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnantHuman);
            if (pregnancyHediff != null)
            {
                context.Patient.health.RemoveHediff(pregnancyHediff);
                Logger.Log("YAMP", $"Terminated pregnancy for {context.Patient.LabelShort}");
            }
        }
    }
}
