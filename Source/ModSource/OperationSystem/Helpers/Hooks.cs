using YAMP.OperationSystem.Core;

namespace YAMP.OperationSystem.RimWorld
{
    /// <summary>
    /// Reusable hooks library for common operation patterns.
    /// </summary>
    public static class Hooks
    {
        // ==================== REUSABLE PREHOOKS ====================

        /// <summary>
        /// Validate that patient is alive and valid.
        /// </summary>
        public static bool ValidatePatient(ref OperationContext context)
        {
            var patient = context.GetArgument<object>(0);
            if (!HealthHelper.IsValidPatient(patient))
            {
                context.SetState("FailureReason", "Invalid or dead patient");
                return false; // ABORT
            }
            return true;
        }

        /// <summary>
        /// Validate that body part exists and is not missing.
        /// </summary>
        public static bool ValidateBodyPart(ref OperationContext context)
        {
            var patient = context.GetArgument<object>(0);
            var bodyPart = context.GetArgument<object>(1);

            if (HealthHelper.PartIsMissing(patient, bodyPart))
            {
                context.SetState("FailureReason", "Body part is missing");
                return false; // ABORT
            }
            return true;
        }

        /// <summary>
        /// Validate that patient is flesh-based (not mechanoid).
        /// </summary>
        public static bool ValidateFleshPatient(ref OperationContext context)
        {
            var patient = context.GetArgument<object>(0);
            if (!HealthHelper.IsFleshPawn(patient))
            {
                context.SetState("FailureReason", "Patient is not flesh-based");
                return false; // ABORT
            }
            return true;
        }

        // ==================== REUSABLE CLEANUP HOOKS ====================

        /// <summary>
        /// Log operation result with execution trace.
        /// </summary>
        public static void LogResult(
            ref OperationContext context,
            ref OperationResult result,
            System.Collections.Generic.List<(string hookName, bool success)> trace)
        {
            Logger.Log("YAMP", $"Operation '{context.OperationName}' completed: {result.Success}");

            if (result.FailureReason != null)
            {
                Logger.Log("YAMP", $"Failure reason: {result.FailureReason}");
            }

            // Debug trace
            var traceStr = string.Join(", ", trace.ConvertAll(t => $"{t.hookName}={t.success}"));
            Logger.Debug($"Execution trace: {traceStr}");
        }
    }
}
