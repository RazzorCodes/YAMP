using System.Linq;
using RimWorld;
using Verse;

namespace YAMP.OperationSystem
{
    // ==================== INSTALL OPERATIONS ====================

    public class InstallPartOperation : BaseOperation, ISurgery
    {
        public override string Name => "Install Body Part";

        public bool CanPerform(Pawn patient, BodyPartRecord part)
        {
            return true;
        }

        public float GetBaseSuccessChance(Pawn patient, BodyPartRecord part)
        {
            return 1f;
        }

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            result.Products = GenerateProducts(context.Patient, context.BodyPart);

            var hediffDef = context.Bill?.recipe?.addsHediff;
            if (hediffDef != null)
            {
                // First remove MissingBodyPart if present (crucial for natural part installation)
                var missingPartHediff = context.Patient.health.hediffSet.hediffs
                    .FirstOrDefault(h => h.def == HediffDefOf.MissingBodyPart && h.Part == context.BodyPart);
                if (missingPartHediff != null)
                {
                    context.Patient.health.RemoveHediff(missingPartHediff);
                }

                var hediff = context.Patient.health.AddHediff(hediffDef, context.BodyPart);
                result.AppliedHediffs.Add(hediff);
                Log.Message($"[YAMP] Installed {hediffDef.label} on {context.Patient.LabelShort}");
            }

            // Spawn products at facility location
            if (context.Facility != null)
            {
                foreach (var product in result.Products)
                {
                    GenPlace.TryPlaceThing(product, context.Facility.Position, context.Facility.Map,
                        ThingPlaceMode.Near);
                }
            }
        }
    }

    /// <summary>
    /// Install artificial body part (bionics) - adds hediff from recipe
    /// </summary>
    public class InstallArtificialPartOperation : InstallPartOperation
    {
        public override string Name => "Install Artificial Body Part";
    }

    /// <summary>
    /// Install natural body part (organs) - adds hediff from recipe
    /// </summary>
    public class InstallNaturalPartOperation : InstallPartOperation
    {
        public override string Name => "Install Natural Body Part";
    }

    /// <summary>
    /// Install implant - adds hediff from recipe
    /// </summary>
    public class InstallImplantOperation : InstallPartOperation
    {
        public override string Name => "Install Implant";
    }

    /// <summary>
    /// Install IUD - adds hediff from recipe
    /// </summary>
    public class InstallIUDOperation : InstallPartOperation
    {
        public override string Name => "Install IUD";
    }
}
