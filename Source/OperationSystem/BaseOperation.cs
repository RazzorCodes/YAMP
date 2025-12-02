using System;
using RimWorld;
using Verse;

namespace YAMP.OperationSystem
{
    /// <summary>
    /// Abstract base class for all operations to reduce code duplication
    /// </summary>
    public abstract class BaseOperation : IOperation
    {
        public abstract string Name { get; }

        public virtual OperationResult Perform(OperationContext context)
        {
            var result = new OperationResult();

            try
            {
                // Pre-operation hook
                context.PreOperationHook?.Invoke(context);

                // Calculate success chance
                float successChance = context.SuccessChanceCalculator?.Invoke(context)
                    ?? context.SuccessChance;

                result.Success = Rand.Value <= successChance;

                if (result.Success)
                {
                    // Execute the actual operation logic
                    ExecuteOperation(context, result);
                }
                else
                {
                    // Handle failure
                    HandleFailure(context, result);
                }

                // Post-operation hook
                context.PostOperationHook?.Invoke(context, result);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex;
                result.FailureReason = $"Exception: {ex.Message}";
                Log.Error($"[YAMP] {Name} failed: {ex.Message}\n{ex.StackTrace}");
            }

            return result;
        }

        /// <summary>
        /// Execute the actual operation logic when successful
        /// </summary>
        protected abstract void ExecuteOperation(OperationContext context, OperationResult result);

        /// <summary>
        /// Handle operation failure - can be overridden for custom failure logic
        /// </summary>
        protected virtual void HandleFailure(OperationContext context, OperationResult result)
        {
            result.FailureReason = $"{Name} failed due to complications";
            // Light injury on failure
            context.Patient.TakeDamage(new DamageInfo(DamageDefOf.Cut, 3, 0, -1, null, context.BodyPart));
        }
    }
}
