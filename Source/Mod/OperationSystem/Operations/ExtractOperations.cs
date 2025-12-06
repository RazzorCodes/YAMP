using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using YAMP.OperationSystem.Core;
using YAMP.OperationSystem.RimWorld;

namespace YAMP.OperationSystem
{
    // ==================== EXTRACT OPERATIONS ====================

    /// <summary>
    /// Extract hemogen from pawn
    /// </summary>
    public class ExtractHemogenOperation : Core.IOperation
    {
        public string Name => "Extract Hemogen";
        public float hemogenLossAmount = 0.45f;

        public List<(string name, PreHook hook)> PreHooks => new List<(string name, PreHook hook)>
        {
            ("ValidatePatient", Hooks.ValidatePatient),
            ("ValidateFlesh", Hooks.ValidateFleshPatient),
            ("CheckHemogenGene", CheckHemogenGene),
            ("CheckStock", CheckStock)
        };

        public List<(string name, PostHook hook)> PostHooks => new List<(string name, PostHook hook)>
        {
            ("StoreProducts", StoreProducts)
        };

        public List<(string name, CleanupHook hook)> Cleanup => new List<(string name, CleanupHook hook)>
        {
            ("Log", Hooks.LogResult)
        };

        // ==================== OPERATION-SPECIFIC PREHOOKS ====================

        private bool CheckHemogenGene(ref OperationContext context)
        {
            var patient = context.GetArgument<Pawn>(0);
            if (patient?.genes?.GetGene(GeneDefOf.Hemogenic) != null)
            {
                context.SetState("FailureReason", "Patient has hemogenic gene");
                return false;
            }
            return true;
        }

        private bool CheckStock(ref OperationContext context)
        {
            var facility = context.GetArgument<object>(3);
            var recipe = context.GetArgument<object>(2);

            if (!FacilityHelper.HasRequiredStock(facility, recipe))
            {
                context.SetState("FailureReason", "AwaitingMaterials");
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

                // Hemogen extraction creates hemogen pack as product
                var hemogenPack = ThingMaker.MakeThing(ThingDefOf.HemogenPack);
                result.Products.Add(hemogenPack);

                // Apply blood loss
                if (HealthHelper.AddHediff(patient, HediffDefOf.BloodLoss, null) is Hediff bloodLoss)
                {
                    bloodLoss.Severity = hemogenLossAmount;
                }

                // Consume stock
                FacilityHelper.ConsumeStock(facility, recipe);

                result.Success = true;
                Logger.Log("YAMP", $"Successfully extracted hemogen from {patient.LabelShort}");
                return true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex;
                result.FailureReason = $"Exception: {ex.Message}";
                Logger.Log("YAMP", $"ExtractHemogenOperation failed: {ex.Message}");
                return false;
            }
        }

        // ==================== OPERATION-SPECIFIC POSTHOOKS ====================

        private bool StoreProducts(ref OperationContext context, ref OperationResult result)
        {
            var facility = context.GetArgument<ThingWithComps>(3);

            if (result.Products != null && result.Products.Count > 0)
            {
                if (FacilityHelper.CanStoreInContainer(facility, result.Products.ToArray()))
                {
                    FacilityHelper.StoreInContainer(facility, result.Products.ToArray());
                }
                else
                {
                    FacilityHelper.DumpNearFacility(facility, result.Products.ToArray());
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Extract ovum from female pawn
    /// </summary>
    public class ExtractOvumOperation : Core.IOperation
    {
        public string Name => "Extract Ovum";

        public List<(string name, PreHook hook)> PreHooks => new List<(string name, PreHook hook)>
        {
            ("ValidatePatient", Hooks.ValidatePatient),
            ("ValidateFemale", ValidateFemale),
            ("CheckStock", CheckStock)
        };

        public List<(string name, PostHook hook)> PostHooks => new List<(string name, PostHook hook)>
        {
            ("StoreProducts", StoreProducts)
        };

        public List<(string name, CleanupHook hook)> Cleanup => new List<(string name, CleanupHook hook)>
        {
            ("Log", Hooks.LogResult)
        };

        // ==================== OPERATION-SPECIFIC PREHOOKS ====================

        private bool ValidateFemale(ref OperationContext context)
        {
            var patient = context.GetArgument<Pawn>(0);
            if (patient?.gender != Gender.Female || patient.ageTracker.AgeBiologicalYears < 18)
            {
                context.SetState("FailureReason", "Patient must be adult female");
                return false;
            }
            return true;
        }

        private bool CheckStock(ref OperationContext context)
        {
            var facility = context.GetArgument<object>(3);
            var recipe = context.GetArgument<object>(2);

            if (!FacilityHelper.HasRequiredStock(facility, recipe))
            {
                context.SetState("FailureReason", "AwaitingMaterials");
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

                // Ovum extraction creates ovum as product
                var ovum = ThingMaker.MakeThing(ThingDefOf.HumanOvum) as HumanOvum;
                ovum?.TryGetComp<CompHasPawnSources>()?.AddSource(patient);
                result.Products.Add(ovum);

                // Consume stock
                FacilityHelper.ConsumeStock(facility, recipe);

                result.Success = true;
                Logger.Log("YAMP", $"Successfully extracted ovum from {patient.LabelShort}");
                return true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex;
                result.FailureReason = $"Exception: {ex.Message}";
                Logger.Log("YAMP", $"ExtractOvumOperation failed: {ex.Message}");
                return false;
            }
        }

        // ==================== OPERATION-SPECIFIC POSTHOOKS ====================

        private bool StoreProducts(ref OperationContext context, ref OperationResult result)
        {
            var facility = context.GetArgument<ThingWithComps>(3);

            if (result.Products != null && result.Products.Count > 0)
            {
                if (FacilityHelper.CanStoreInContainer(facility, result.Products.ToArray()))
                {
                    FacilityHelper.StoreInContainer(facility, result.Products.ToArray());
                }
                else
                {
                    FacilityHelper.DumpNearFacility(facility, result.Products.ToArray());
                }
            }

            return true;
        }
    }
}
