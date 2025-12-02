using RimWorld;
using Verse;

namespace YAMP.OperationSystem
{
    // ==================== EXECUTE OPERATIONS ====================

    /// <summary>
    /// Execute pawn by cutting (euthanasia)
    /// </summary>
    public class ExecuteByCutOperation : BaseOperation, IExecute
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
            Log.Message($"[YAMP] Executed {context.Patient.LabelShort}");
        }

        protected override void HandleFailure(OperationContext context, OperationResult result)
        {
            result.FailureReason = "Execution failed, pawn survived but is severely wounded";
            // Severe damage but not lethal
            context.Patient.TakeDamage(new DamageInfo(DamageDefOf.Cut, 50, 5f, -1, null, null));
        }
    }
}
