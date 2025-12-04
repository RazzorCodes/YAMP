using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using YAMP.OperationSystem.Core;
using YAMP.OperationSystem.RimWorld;

namespace YAMP.OperationSystem
{
    // ==================== INSTALL OPERATIONS ====================

    public class InstallPartOperation : Core.IOperation
    {
        public string Name => "Install Part";

        public List<(string name, PreHook hook)> PreHooks => new List<(string name, PreHook hook)>
        {
            ("ValidatePatient", Hooks.ValidatePatient),
            ("ValidateBodyPart", ValidateBodyPartForInstall),
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

        private bool ValidateBodyPartForInstall(ref OperationContext context)
        {
            // For installation, the part should exist (missing parts get MissingBodyPart hediff removed)
            // This is just basic validation that we have a body part reference
            var bodyPart = context.GetArgument<object>(1);
            if (bodyPart == null)
            {
                context.SetState("FailureReason", "No body part specified");
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
                context.SetState("StockAvailable", false);
                context.SetState("FailureReason", "AwaitingMaterials");
                return false; // ABORT - not enough stock
            }

            context.SetState("StockAvailable", true);
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
                float finalChance = vanillaChance;

                bool success = Rand.Value <= finalChance;

                if (success)
                {
                    // Generate products from removed parts
                    result.Products = HealthHelper.GenerateProductsFromPart(patient, bodyPart);

                    // Remove missing part hediff if present (crucial for natural part installation)
                    var missingHediff = patient.health.hediffSet.hediffs
                        .FirstOrDefault(h => h.def == HediffDefOf.MissingBodyPart && h.Part == bodyPart);
                    if (missingHediff != null)
                    {
                        HealthHelper.RemoveHediff(patient, missingHediff);
                    }

                    // Remove negative hediffs from the part
                    var hediffsToRemove = patient.health.hediffSet.hediffs
                        .Where(h => h.Part == bodyPart)
                        .ToList();
                    foreach (var hediff in hediffsToRemove)
                    {
                        HealthHelper.RemoveHediff(patient, hediff);
                    }


                    // Apply new hediff
                    var hediffDef = recipe?.addsHediff;
                    if (hediffDef != null)
                    {
                        var newHediff = HealthHelper.AddHediff(patient, hediffDef, bodyPart);
                        result.AppliedEffects.Add(newHediff);
                    }

                    // Consume stock
                    FacilityHelper.ConsumeStock(facility, recipe);

                    result.Success = true;
                }
                else
                {
                    // Apply damage on failure
                    HealthHelper.ApplyDamage(patient, DamageDefOf.Cut, 15f, bodyPart);
                    result.Success = false;
                    result.FailureReason = "Operation failed due to complications";
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex;
                result.FailureReason = $"Exception: {ex.Message}";
                Logger.Log("YAMP", $"InstallPartOperation failed: {ex.Message}");
                return false;
            }
        }

        // ==================== OPERATION-SPECIFIC POSTHOOKS ====================

        private bool StoreProducts(ref OperationContext context, ref OperationResult result)
        {
            var facility = context.GetArgument<ThingWithComps>(3);

            if (result.Products != null && result.Products.Count > 0)
            {
                // Try to store in container, otherwise dump near facility
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
