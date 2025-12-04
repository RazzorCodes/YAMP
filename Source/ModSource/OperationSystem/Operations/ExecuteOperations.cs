using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using YAMP.OperationSystem.Core;
using YAMP.OperationSystem.RimWorld;

namespace YAMP.OperationSystem
{
    // ==================== EXECUTE OPERATIONS ====================

    /// <summary>
    /// Execute pawn by cutting (euthanasia)
    /// </summary>
    public class ExecuteByCutOperation : Core.IOperation
    {
        public string Name => "Execute by Cutting";

        public List<(string name, PreHook hook)> PreHooks => new List<(string name, PreHook hook)>
        {
            ("ValidatePatient", Hooks.ValidatePatient),
            ("ValidateFlesh", Hooks.ValidateFleshPatient)
        };

        public List<(string name, PostHook hook)> PostHooks => new List<(string name, PostHook hook)>();

        public List<(string name, CleanupHook hook)> Cleanup => new List<(string name, CleanupHook hook)>
        {
            ("Log", Hooks.LogResult)
        };

        // ==================== EXECUTE ====================

        public bool Execute(ref OperationContext context, ref OperationResult result)
        {
            try
            {
                var patient = context.GetArgument<Pawn>(0);
                var recipe = context.GetArgument<RecipeDef>(2);
                var facility = context.GetArgument<ThingWithComps>(3);

                // Calculate success (execution should almost always succeed)
                bool success = Rand.Value <= 0.99f;

                if (success)
                {
                    // Execute instantly kills the pawn
                    patient.Kill(new DamageInfo(DamageDefOf.ExecutionCut, 99999, 999f, -1, null, null));

                    // Consume stock
                    FacilityHelper.ConsumeStock(facility, recipe);

                    result.Success = true;
                    Logger.Log("YAMP", $"Executed {patient.LabelShort}");
                }
                else
                {
                    // Severe damage but not lethal
                    HealthHelper.ApplyDamage(patient, DamageDefOf.Cut, 50, null);
                    result.Success = false;
                    result.FailureReason = "Execution failed, pawn survived but is severely wounded";
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex;
                result.FailureReason = $"Exception: {ex.Message}";
                Logger.Log("YAMP", $"ExecuteByCutOperation failed: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Terminate pregnancy - removes pregnancy hediff
    /// </summary>
    public class TerminatePregnancyOperation : Core.IOperation
    {
        public string Name => "Terminate Pregnancy";

        public List<(string name, PreHook hook)> PreHooks => new List<(string name, PreHook hook)>
        {
            ("ValidatePatient", Hooks.ValidatePatient),
            ("ValidatePregnancy", ValidatePregnancy)
        };

        public List<(string name, PostHook hook)> PostHooks => new List<(string name, PostHook hook)>();

        public List<(string name, CleanupHook hook)> Cleanup => new List<(string name, CleanupHook hook)>
        {
            ("Log", Hooks.LogResult)
        };

        // ==================== OPERATION-SPECIFIC PREHOOKS ====================

        private bool ValidatePregnancy(ref OperationContext context)
        {
            var patient = context.GetArgument<Pawn>(0);
            if (patient?.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnantHuman) == null)
            {
                context.SetState("FailureReason", "Patient is not pregnant");
                return false;
            }
            return true;
        }

        // ==================== EXECUTE ====================

        public bool Execute(ref OperationContext context, ref OperationResult result)
        {
            try
            {
                var patient = context.GetArgument<Pawn>(0);
                var recipe = context.GetArgument<RecipeDef>(2);
                var facility = context.GetArgument<ThingWithComps>(3);

                // Remove pregnancy hediff
                var pregnancyHediff = patient.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.PregnantHuman);
                if (pregnancyHediff != null)
                {
                    HealthHelper.RemoveHediff(patient, pregnancyHediff);

                    // Consume stock
                    FacilityHelper.ConsumeStock(facility, recipe);

                    result.Success = true;
                    Logger.Log("YAMP", $"Terminated pregnancy for {patient.LabelShort}");
                }
                else
                {
                    result.Success = false;
                    result.FailureReason = "No pregnancy found";
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex;
                result.FailureReason = $"Exception: {ex.Message}";
                Logger.Log("YAMP", $"TerminatePregnancyOperation failed: {ex.Message}");
                return false;
            }
        }
    }
}
