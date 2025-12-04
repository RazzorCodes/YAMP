using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using YAMP.OperationSystem.Core;
using YAMP.OperationSystem.RimWorld;

namespace YAMP.OperationSystem
{
    // ==================== ADMINISTER OPERATIONS ====================

    /// <summary>
    /// Administer ingestible items (drugs, anesthesia, etc.) - applies recipe effects
    /// </summary>
    public class AdministerIngestibleOperation : Core.IOperation
    {
        public string Name => "Administer Ingestible";

        public List<(string name, PreHook hook)> PreHooks => new List<(string name, PreHook hook)>
        {
            ("ValidatePatient", Hooks.ValidatePatient)
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
                var ingredients = context.GetArgument<System.Collections.Generic.List<Thing>>(4);

                var item = ingredients?.FirstOrDefault();
                if (item?.def?.ingestible?.outcomeDoers != null)
                {
                    // Apply ingestible effects
                    item.def.ingestible.outcomeDoers?.ForEach(doer =>
                        doer.DoIngestionOutcome(patient, item, 1));

                    result.Success = true;
                    Logger.Log("YAMP", $"Administered {item.Label} to {patient.LabelShort}");
                }
                else
                {
                    result.Success = false;
                    result.FailureReason = "Item is not ingestible";
                    Logger.Log("YAMP", $"AdministerIngestibleOperation: Item {item?.Label} is not ingestible");
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex;
                result.FailureReason = $"Exception: {ex.Message}";
                Logger.Log("YAMP", $"AdministerIngestibleOperation failed: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Administer usable items
    /// </summary>
    public class AdministerUsableItemOperation : Core.IOperation
    {
        public string Name => "Administer Usable Item";

        public List<(string name, PreHook hook)> PreHooks => new List<(string name, PreHook hook)>
        {
            ("ValidatePatient", Hooks.ValidatePatient)
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
                var ingredients = context.GetArgument<System.Collections.Generic.List<Thing>>(4);

                CompUsable item = null;
                ingredients?.FirstOrDefault().TryGetComp(out item);

                if (item != null)
                {
                    item.UsedBy(patient);
                    result.Success = true;
                    Logger.Log("YAMP", $"Administered {item.parent.Label} to {patient.LabelShort}");
                }
                else
                {
                    result.Success = false;
                    result.FailureReason = "Item is not usable";
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex;
                result.FailureReason = $"Exception: {ex.Message}";
                Logger.Log("YAMP", $"AdministerUsableItemOperation failed: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Blood transfusion - removes blood loss hediff
    /// </summary>
    public class BloodTransfusionOperation : Core.IOperation
    {
        public string Name => "Blood Transfusion";

        public List<(string name, PreHook hook)> PreHooks => new List<(string name, PreHook hook)>
        {
            ("ValidatePatient", Hooks.ValidatePatient)
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

                // Remove blood loss
                var bloodLossHediff = patient.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
                if (bloodLossHediff != null)
                {
                    HealthHelper.RemoveHediff(patient, bloodLossHediff);

                    // Consume stock
                    FacilityHelper.ConsumeStock(facility, recipe);

                    result.Success = true;
                    Logger.Log("YAMP", $"Blood transfusion restored {patient.LabelShort}");
                }
                else
                {
                    result.Success = false;
                    result.FailureReason = "No blood loss to treat";
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex;
                result.FailureReason = $"Exception: {ex.Message}";
                Logger.Log("YAMP", $"BloodTransfusionOperation failed: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Implant embryo - makes pawn pregnant
    /// </summary>
    public class ImplantEmbryoOperation : Core.IOperation
    {
        public string Name => "Implant Embryo";

        public List<(string name, PreHook hook)> PreHooks => new List<(string name, PreHook hook)>
        {
            ("ValidatePatient", Hooks.ValidatePatient)
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
                var ingredients = context.GetArgument<System.Collections.Generic.List<Thing>>(4);

                // Calculate success chance
                float successChance = HealthHelper.GetVanillaSuccessChance(recipe, patient, null);
                bool success = Rand.Value <= successChance;

                if (success)
                {
                    // Get embryo from ingredients
                    var embryo = ingredients?.FirstOrDefault(t => t.def == ThingDefOf.HumanEmbryo) as HumanEmbryo;
                    if (embryo?.TryGetComp<CompHasPawnSources>() is CompHasPawnSources sources && sources.pawnSources != null)
                    {
                        // Apply pregnancy hediff
                        var hediff = HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, patient) as Hediff_Pregnant;
                        if (hediff != null)
                        {
                            // Set parents from embryo source
                            if (sources.pawnSources.Count >= 2)
                            {
                                hediff.SetParents(
                                    sources.pawnSources[0],
                                    sources.pawnSources[1],
                                    embryo.GeneSet);
                            }

                            patient.health.AddHediff(hediff);
                            result.AppliedEffects.Add(hediff);

                            // Consume stock
                            FacilityHelper.ConsumeStock(facility, recipe);

                            result.Success = true;
                            Logger.Log("YAMP", $"Successfully implanted embryo in {patient.LabelShort}");
                        }
                        else
                        {
                            result.Success = false;
                            result.FailureReason = "Failed to create pregnancy hediff";
                        }
                    }
                    else
                    {
                        result.Success = false;
                        result.FailureReason = "Invalid embryo";
                    }
                }
                else
                {
                    // Embryo is lost on failure
                    HealthHelper.ApplyDamage(patient, DamageDefOf.Cut, 5, null);
                    result.Success = false;
                    result.FailureReason = "Embryo implantation failed";
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex;
                result.FailureReason = $"Exception: {ex.Message}";
                Logger.Log("YAMP", $"ImplantEmbryoOperation failed: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Anesthetize pawn
    /// </summary>
    public class AnesthetizeOperation : Core.IOperation
    {
        public string Name => "Anesthetize";

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

                // Anesthetize pawn
                patient.health.forceDowned = true;
                var anesthetic = HealthHelper.AddHediff(patient, HediffDefOf.Anesthetic, null);
                patient.health.forceDowned = false;

                if (anesthetic != null)
                {
                    result.AppliedEffects.Add(anesthetic);

                    // Consume stock
                    FacilityHelper.ConsumeStock(facility, recipe);

                    result.Success = true;
                    Logger.Log("YAMP", $"Successfully anesthetized {patient.LabelShort}");
                }
                else
                {
                    result.Success = false;
                    result.FailureReason = "Failed to apply anesthetic";
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Error = ex;
                result.FailureReason = $"Exception: {ex.Message}";
                Logger.Log("YAMP", $"AnesthetizeOperation failed: {ex.Message}");
                return false;
            }
        }
    }
}
