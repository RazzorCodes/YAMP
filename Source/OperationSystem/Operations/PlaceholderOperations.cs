using System.Linq;
using RimWorld;
using Verse;

namespace YAMP.OperationSystem
{
    // ==================== ADMINISTER OPERATIONS ====================

    /// <summary>
    /// Administer ingestible items (drugs, anesthesia, etc.)
    /// </summary>
    public class AdministerIngestibleOperation : BaseOperation, IAdminister
    {
        public override string Name => "Administer Ingestible";
        public ThingDef ItemDef => null; // Determined from recipe
        public int RequiredCount => 1;

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            // Administration is handled by vanilla
            Log.Message($"[YAMP] Successfully administered ingestible to {context.Patient.LabelShort}");
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
            Log.Message($"[YAMP] Successfully administered usable item to {context.Patient.LabelShort}");
        }
    }

    /// <summary>
    /// Blood transfusion
    /// </summary>
    public class BloodTransfusionOperation : BaseOperation, IAdminister
    {
        public override string Name => "Blood Transfusion";
        public ThingDef ItemDef => ThingDefOf.MedicineIndustrial;
        public int RequiredCount => 1;

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            // Transfusion restores blood loss
            var bloodLossHediff = context.Patient.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
            if (bloodLossHediff != null)
            {
                context.Patient.health.RemoveHediff(bloodLossHediff);
            }

            Log.Message($"[YAMP] Successfully performed blood transfusion on {context.Patient.LabelShort}");
        }
    }

    /// <summary>
    /// Implant embryo
    /// </summary>
    public class ImplantEmbryoOperation : BaseOperation, IAdminister
    {
        public override string Name => "Implant Embryo";
        public ThingDef ItemDef => null; // Uses embryo from ingredients
        public int RequiredCount => 1;

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            // Embryo implantation is handled by vanilla
            Log.Message($"[YAMP] Successfully implanted embryo in {context.Patient.LabelShort}");
        }

        protected override void HandleFailure(OperationContext context, OperationResult result)
        {
            result.FailureReason = "Embryo implantation failed";
            // Embryo is lost on failure
            context.Patient.TakeDamage(new DamageInfo(DamageDefOf.Cut, 5, 0, -1, null, null));
        }
    }
}
