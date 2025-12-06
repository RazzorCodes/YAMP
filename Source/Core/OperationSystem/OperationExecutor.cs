using System;
using System.Collections.Generic;

namespace YAMP.OperationSystem.Core
{
    /// <summary>
    /// Pipeline executor that orchestrates PreHooks → Execute → PostHooks → Cleanup.
    /// Maintains execution trace for rollback and debugging.
    /// </summary>
    public class OperationExecutor : IOperationExecutor
    {
        public OperationResult Execute(IOperation operation, OperationContext context)
        {
            var result = new OperationResult();
            var trace = new List<(string hookName, bool success)>();

            try
            {
                // ===== PHASE 1: PreHooks =====
                // Run all PreHooks, abort if any returns false
                foreach (var (name, hook) in operation.PreHooks)
                {
                    bool success = hook(ref context);
                    trace.Add((name, success));

                    if (!success)
                    {
                        // Abort to cleanup
                        goto Cleanup;
                    }
                }

                // ===== PHASE 2: Execute =====
                // Only runs if all PreHooks passed
                bool executeSuccess = operation.Execute(ref context, ref result);
                trace.Add(("Execute", executeSuccess));

                if (!executeSuccess)
                {
                    // Skip to cleanup on failure
                    goto Cleanup;
                }

                // ===== PHASE 3: PostHooks =====
                // Only runs if Execute succeeded
                foreach (var (name, hook) in operation.PostHooks)
                {
                    bool success = hook(ref context, ref result);
                    trace.Add((name, success));
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex;
                result.FailureReason = $"Exception during execution: {ex.Message}";
                trace.Add(("Exception", false));
            }

        // ===== PHASE 4: Cleanup =====
        // Always runs, receives full execution trace
        Cleanup:
            try
            {
                foreach (var (name, hook) in operation.Cleanup)
                {
                    hook(ref context, ref result, trace);
                }
            }
            catch (Exception ex)
            {
                // Log cleanup failures but don't propagate
                result.FailureReason = (result.FailureReason ?? "") + $" | Cleanup error: {ex.Message}";
            }

            return result;
        }
    }
}
