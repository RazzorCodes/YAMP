using System.Linq;
using RimWorld;
using Verse;

namespace YAMP.OperationSystem
{
    /// <summary>
    /// Administer ingestible items (drugs, anesthesia, etc.) - applies recipe effects
    /// </summary>
    public class AdministerIngestibleOperation : BaseOperation, IAdminister
    {
        public override string Name => "Administer Ingestible";

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            var item = context.Ingredients?.FirstOrDefault();
            if (item?.def?.ingestible?.outcomeDoers != null)
            {
                // Apply ingestible effects
                item.def.ingestible.outcomeDoers?.ForEach(doer =>
                    doer.DoIngestionOutcome(context.Patient, item, 1));

                Log.Message($"[YAMP] Administered {item.Label} to {context.Patient.LabelShort}");
            }
            else
            {
                result.FailureReason = "Item is not ingestible";
                Logger.Log("[YAMP] AdministerIngestibleOperation: ", $"Item {item?.Label} is not ingestible");
            }
        }

        protected override void HandleFailure(OperationContext context, OperationResult result)
        {
            result.FailureReason = "Failed to properly administer item";
            // No physical damage on administration failure
        }
    }

    /// <summary>
    /// Administer usable items
    /// </summary>
    public class AdministerUsableItemOperation : BaseOperation, IAdminister
    {
        public override string Name => "Administer Usable Item";
        public ThingDef ItemDef => null;
        public int RequiredCount => 1;

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            // Similar to ingestible but for usable items
            var item = context.Ingredients?.FirstOrDefault();
            if (item != null)
            {
                Log.Message($"[YAMP] Administered {item.Label} to {context.Patient.LabelShort}");
            }
        }
    }

    /// <summary>
    /// Blood transfusion - removes blood loss hediff
    /// </summary>
    public class BloodTransfusionOperation : BaseOperation, IAdminister
    {
        public override string Name => "Blood Transfusion";
        public ThingDef ItemDef => ThingDefOf.MedicineIndustrial;
        public int RequiredCount => 1;

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            // Remove blood loss
            var bloodLossHediff = context.Patient.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
            if (bloodLossHediff != null)
            {
                context.Patient.health.RemoveHediff(bloodLossHediff);
                Log.Message($"[YAMP] Blood transfusion restored {context.Patient.LabelShort}");
            }
        }
    }

    /// <summary>
    /// Implant embryo - makes pawn pregnant
    /// </summary>
    public class ImplantEmbryoOperation : BaseOperation, IAdminister
    {
        public override string Name => "Implant Embryo";
        public ThingDef ItemDef => null; // Uses embryo from ingredients
        public int RequiredCount => 1;

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            // Get embryo from ingredients - use simple LINQ to avoid Predicate conflicts
            var embryo = context.Ingredients?.FirstOrDefault(t => t.def == ThingDefOf.HumanEmbryo) as HumanEmbryo;
            if (embryo?.TryGetComp<CompHasPawnSources>() is CompHasPawnSources sources && sources.pawnSources != null)
            {
                // Apply pregnancy hediff
                var hediff = HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, context.Patient) as Hediff_Pregnant;
                if (hediff != null)
                {
                    // Set parents from embryo source
                    if (sources.pawnSources.Count >= 2)
                    {
                        // Pass null for geneset, vanilla will handle it from the embryo
                        hediff.SetParents(
                            sources.pawnSources[0],
                            sources.pawnSources[1],
                            embryo.GeneSet);
                    }

                    context.Patient.health.AddHediff(hediff);
                    result.AppliedHediffs.Add(hediff);
                    Log.Message($"[YAMP] Successfully implanted embryo in {context.Patient.LabelShort}");
                }
            }
        }

        protected override void HandleFailure(OperationContext context, OperationResult result)
        {
            result.FailureReason = "Embryo implantation failed";
            // Embryo is lost on failure
            context.Patient.TakeDamage(new DamageInfo(DamageDefOf.Cut, 5, 0, -1, null, null));
        }
    }

    /// <summary>
    /// Anesthetize pawn
    /// </summary>
    public class AnesthetizeOperation : BaseOperation, IAdminister
    {
        public override string Name => "Anesthetize";

        public ThingDef ItemDef => ThingDefOf.MedicineIndustrial;
        public int RequiredCount => 1;

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            // Anesthetize pawn
            if (!context.Patient.RaceProps.IsFlesh)
            {
                result.FailureReason = "Pawn is not a flesh-based organism";
                return;
            }

            context.Patient.health.forceDowned = true;
            context.Patient.health.AddHediff(HediffDefOf.Anesthetic);
            context.Patient.health.forceDowned = false;

            Log.Message($"[YAMP] Successfully anesthetized {context.Patient.LabelShort}");
        }

        protected override void HandleFailure(OperationContext context, OperationResult result)
        {
            result.FailureReason = "Anesthetization failed";
        }
    }
}
