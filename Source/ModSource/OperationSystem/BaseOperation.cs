using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace YAMP.OperationSystem
{
    /// <summary>
    /// Context passed to operations containing all necessary data and hooks
    /// </summary>
    public class OperationContext
    {
        public Pawn Patient { get; set; }
        public BodyPartRecord BodyPart { get; set; }
        public RimWorld.Bill_Medical Bill { get; set; }
        public List<Thing> Ingredients { get; set; }
        public ThingWithComps Facility { get; set; }
        public Pawn Surgeon { get; set; }

        // Customizable properties
        public float SuccessChance { get; set; } = 0.98f;

        // Hooks
        public Action<OperationContext> PreOperationHook { get; set; }
        public Action<OperationContext, OperationResult> PostOperationHook { get; set; }
        public Func<OperationContext, float> SuccessChanceCalculator { get; set; }
    }

    /// <summary>
    /// Result of an operation execution
    /// </summary>
    public class OperationResult
    {
        public bool Success { get; set; }
        public List<Thing> Products { get; set; } = new List<Thing>();
        public List<Hediff> AppliedHediffs { get; set; } = new List<Hediff>();
        public string FailureReason { get; set; }
        public Exception Error { get; set; }
    }

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
                float successChance =
                    context.SuccessChanceCalculator?.Invoke(context) ?? context.SuccessChance;

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
                Logger.Log("YAMP", $"{Name} failed: {ex.Message}\n{ex.StackTrace}");
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
            context.Patient.TakeDamage(
                new DamageInfo(DamageDefOf.Blunt, 1, 0, -1, null, context.BodyPart)
            );
        }

        protected virtual System.Collections.Generic.List<Thing> GenerateProducts(
            Pawn patient,
            BodyPartRecord part
        )
        {
            var products = new System.Collections.Generic.List<Thing>();

            // Installed bionics/implants
            foreach (Hediff hediff in patient.health.hediffSet.hediffs.Where(h => h.Part == part))
            {
                if (hediff.def.spawnThingOnRemoved != null)
                {
                    var product = ThingMaker.MakeThing(hediff.def.spawnThingOnRemoved);
                    products.Add(product);
                }
            }

            // Natural body part - only spawn if not missing
            if (
                part?.def.spawnThingOnRemoved != null
                && patient != null && !patient.health.hediffSet.PartIsMissing(part)
            )
            {
                var product = ThingMaker.MakeThing(part.def.spawnThingOnRemoved);
                products.Add(product);
            }

            return products;
        }
    }
}
