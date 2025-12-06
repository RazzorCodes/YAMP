using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using YAMP.OperationSystem.Core;
using YAMP.OperationSystem.RimWorld;

namespace YAMP.OperationSystem
{
    // ==================== REMOVE OPERATIONS ====================

    /// <summary>
    /// Remove body part (surgical amputation)
    /// </summary>
    public class RemovePartOperation : Core.IOperation
    {
        public string Name => "Remove Body Part";

        public List<(string name, PreHook hook)> PreHooks => new List<(string name, PreHook hook)>
        {
            ("ValidatePatient", Hooks.ValidatePatient),
            ("ValidateBodyPart", Hooks.ValidateBodyPart),
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

        private bool CheckStock(ref OperationContext context)
        {
            var facility = context.GetArgument<object>(3);
            var recipe = context.GetArgument<object>(2);

            if (!FacilityHelper.HasRequiredStock(facility, recipe))
            {
                context.SetState("FailureReason", "AwaitingMaterials");
                return false; // ABORT
            }

            return true;
        }

        // ==================== EXECUTE ====================

        public bool Execute(ref OperationContext context, ref OperationResult result)
        {
            try
            {
                var patient = context.GetArgument<Pawn>(0);
                var bodyPart = context.GetArgument<BodyPartRecord>(1);
                var recipe = context.GetArgument<RecipeDef>(2);
                var facility = context.GetArgument<ThingWithComps>(3);

                // Calculate success chance
                float vanillaChance = HealthHelper.GetVanillaSuccessChance(recipe, patient, bodyPart);
                bool success = Rand.Value <= vanillaChance;

                if (success)
                {
                    // Generate products (removed organs/parts)
                    result.Products = HealthHelper.GenerateProductsFromPart(patient, bodyPart);

                    // Apply missing body part hediff
                    var hediff = HealthHelper.AddHediff(patient, HediffDefOf.MissingBodyPart, bodyPart);
                    result.AppliedEffects.Add(hediff);

                    // Consume stock
                    FacilityHelper.ConsumeStock(facility, recipe);

                    result.Success = true;
                    Logger.Debug($"Successfully removed {bodyPart.Label} from {patient.LabelShort}");
                }
                else
                {
                    // Apply damage on failure
                    HealthHelper.ApplyDamage(patient, DamageDefOf.Cut, 15f, bodyPart);
                    result.Success = false;
                    result.FailureReason = "Removal failed due to complications";
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex;
                result.FailureReason = $"Exception: {ex.Message}";
                Logger.Log("YAMP", $"RemovePartOperation failed: {ex.Message}");
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
