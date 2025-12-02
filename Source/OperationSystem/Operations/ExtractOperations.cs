using RimWorld;
using Verse;

namespace YAMP.OperationSystem
{
    // ==================== EXTRACT OPERATIONS ====================

    /// <summary>
    /// Extract hemogen from pawn
    /// </summary>
    public class ExtractHemogenOperation : BaseOperation, IExtract
    {
        public override string Name => "Extract Hemogen";
        public float hemogenLossAmount = 0.45f;


        public bool CanExtract(Pawn patient)
        {
            // Check if pawn has hemogen gene
            return patient.genes?.GetGene(GeneDefOf.Hemogenic) != null;
        }

        public float GetBaseSuccessChance(Pawn patient)
        {
            return 1f; // Uses vanilla surgery success
        }

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            // Hemogen extraction creates hemogen pack as product
            var hemogenPack = ThingMaker.MakeThing(ThingDefOf.HemogenPack);
            result.Products.Add(hemogenPack);

            if (context.Facility != null)
            {
                GenPlace.TryPlaceThing(
                    hemogenPack,
                    context.Facility.Position,
                    context.Facility.Map,
                    ThingPlaceMode.Near);
            }

            context.Patient.health.AddHediff(HediffDefOf.BloodLoss).Severity = hemogenLossAmount;

            Log.Message($"[YAMP] Successfully extracted hemogen from {context.Patient.LabelShort}");
        }

        protected override void HandleFailure(OperationContext context, OperationResult result)
        {
            result.FailureReason = "Hemogen extraction failed";
            // Light damage on failure
            context.Patient.TakeDamage(new DamageInfo(DamageDefOf.Cut, 1, 0, -1, null, null));
        }
    }

    /// <summary>
    /// Extract ovum from female pawn
    /// </summary>
    public class ExtractOvumOperation : BaseOperation, IExtract
    {
        public override string Name => "Extract Ovum";

        public bool CanExtract(Pawn patient)
        {
            return patient.gender == Gender.Female && patient.ageTracker.AgeBiologicalYears >= 18;
        }

        public float GetBaseSuccessChance(Pawn patient)
        {
            return 1f;
        }

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            // Ovum extraction creates ovum as product - using HumanOvum ThingDef
            var ovum = ThingMaker.MakeThing(ThingDefOf.HumanOvum) as HumanOvum;
            ovum.TryGetComp<CompHasPawnSources>().AddSource(context.Patient);
            result.Products.Add(ovum);

            if (context.Facility != null)
            {
                GenPlace.TryPlaceThing(ovum, context.Facility.Position, context.Facility.Map, ThingPlaceMode.Near);
            }

            Log.Message($"[YAMP] Successfully extracted ovum from {context.Patient.LabelShort}");
        }
    }
}
