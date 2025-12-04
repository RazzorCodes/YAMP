using System.Collections.Generic;

namespace YAMP.OperationSystem.Core
{
    // ==================== DELEGATE TYPES ====================

    /// <summary>
    /// PreHook that modifies context before operation execution.
    /// Returns true to continue, false to abort to cleanup.
    /// </summary>
    public delegate bool PreHook(ref OperationContext context);

    /// <summary>
    /// PostHook that modifies result after successful operation execution.
    /// Returns true to continue, false is logged but doesn't affect execution.
    /// </summary>
    public delegate bool PostHook(ref OperationContext context, ref OperationResult result);

    /// <summary>
    /// CleanupHook that always runs at the end with full execution trace.
    /// Used for rollback, logging, and resource cleanup.
    /// </summary>
    public delegate void CleanupHook(
        ref OperationContext context,
        ref OperationResult result,
        List<(string hookName, bool success)> executionTrace
    );

    // ==================== OPERATION INTERFACE ====================

    /// <summary>
    /// Core operation interface with delegate-based hook chain.
    /// </summary>
    public interface IOperation
    {
        /// <summary>
        /// Name of the operation for logging and display.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// PreHooks run before Execute. If any returns false, abort to Cleanup.
        /// </summary>
        List<(string name, PreHook hook)> PreHooks { get; }

        /// <summary>
        /// PostHooks run after successful Execute to modify result.
        /// </summary>
        List<(string name, PostHook hook)> PostHooks { get; }

        /// <summary>
        /// Cleanup hooks always run at the end with full execution trace.
        /// </summary>
        List<(string name, CleanupHook hook)> Cleanup { get; }

        /// <summary>
        /// Execute the operation logic. Only runs if all PreHooks returned true.
        /// </summary>
        bool Execute(ref OperationContext context, ref OperationResult result);
    }
}
