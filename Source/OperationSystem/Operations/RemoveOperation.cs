using System.Linq;
using RimWorld;
using Verse;

namespace YAMP.OperationSystem
{
    // ==================== REMOVE OPERATIONS ====================

    /// <summary>
    /// Remove body part (surgical amputation)
    /// </summary>
    public class RemovePartOperation : BaseOperation
    {
        public override string Name => "Remove Body Part";

        public bool CanPerform(Pawn patient, BodyPartRecord part)
        {
            return part != null && !patient.health.hediffSet.PartIsMissing(part);
        }

        public float GetBaseSuccessChance(Pawn patient, BodyPartRecord part)
        {
            return 1f; // Uses vanilla surgery success
        }

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            // Generate products (removed organs/parts)
            result.Products = GenerateProducts(context.Patient, context.BodyPart);

            // Apply missing body part hediff
            var hediff = context.Patient.health.AddHediff(
                HediffDefOf.MissingBodyPart,
                context.BodyPart
            );
            result.AppliedHediffs.Add(hediff);

            // Spawn products at facility location
            if (context.Facility != null)
            {
                foreach (var product in result.Products)
                {
                    GenPlace.TryPlaceThing(
                        product,
                        context.Facility.Position,
                        context.Facility.Map,
                        ThingPlaceMode.Near
                    );
                }
            }

            Log.Message(
                $"[YAMP] Successfully removed {context.BodyPart.Label} from {context.Patient.LabelShort}"
            );
        }
    }
}
