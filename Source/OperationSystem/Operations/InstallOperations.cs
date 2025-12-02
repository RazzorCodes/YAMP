using System.Linq;
using RimWorld;
using Verse;

namespace YAMP.OperationSystem
{
    // ==================== INSTALL OPERATIONS ====================

    /// <summary>
    /// Install artificial body part (bionics) - adds hediff from recipe
    /// </summary>
    public class InstallArtificialPartOperation : BaseOperation, ISurgery
    {
        public override string Name => "Install Artificial Body Part";

        public bool CanPerform(Pawn patient, BodyPartRecord part)
        {
            return part != null && !patient.health.hediffSet.PartIsMissing(part);
        }

        public float GetBaseSuccessChance(Pawn patient, BodyPartRecord part)
        {
            return 1f; // Uses recipe's surgerySuccessChanceFactor
        }

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            // Get the hediff to add from the recipe
            var hediffDef = context.Bill?.recipe?.addsHediff;
            if (hediffDef != null)
            {
                var hediff = context.Patient.health.AddHediff(hediffDef, context.BodyPart);
                result.AppliedHediffs.Add(hediff);
                Log.Message($"[YAMP] Installed {hediffDef.label} on {context.Patient.LabelShort}");
            }
        }
    }

    /// <summary>
    /// Install natural body part (organs) - adds hediff from recipe
    /// </summary>
    public class InstallNaturalPartOperation : BaseOperation, ISurgery
    {
        public override string Name => "Install Natural Body Part";

        public bool CanPerform(Pawn patient, BodyPartRecord part)
        {
            return part != null;
        }

        public float GetBaseSuccessChance(Pawn patient, BodyPartRecord part)
        {
            return 1f;
        }

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            var hediffDef = context.Bill?.recipe?.addsHediff;
            if (hediffDef != null)
            {
                // First remove MissingBodyPart if present
                var missingPartHediff = context.Patient.health.hediffSet.hediffs
                    .FirstOrDefault(h => h.def == HediffDefOf.MissingBodyPart && h.Part == context.BodyPart);
                if (missingPartHediff != null)
                {
                    context.Patient.health.RemoveHediff(missingPartHediff);
                }

                // Add the new natural part
                var hediff = context.Patient.health.AddHediff(hediffDef, context.BodyPart);
                result.AppliedHediffs.Add(hediff);
                Log.Message($"[YAMP] Installed {hediffDef.label} on {context.Patient.LabelShort}");
            }
        }
    }

    /// <summary>
    /// Install implant - adds hediff from recipe
    /// </summary>
    public class InstallImplantOperation : BaseOperation, ISurgery
    {
        public override string Name => "Install Implant";

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
            var hediffDef = context.Bill?.recipe?.addsHediff;
            if (hediffDef != null)
            {
                var hediff = context.Patient.health.AddHediff(hediffDef, context.BodyPart);
                result.AppliedHediffs.Add(hediff);
                Log.Message($"[YAMP] Installed {hediffDef.label} on {context.Patient.LabelShort}");
            }
        }
    }

    /// <summary>
    /// Install IUD - adds hediff from recipe
    /// </summary>
    public class InstallIUDOperation : BaseOperation, ISurgery
    {
        public override string Name => "Install IUD";

        public bool CanPerform(Pawn patient, BodyPartRecord part)
        {
            return patient.gender == Gender.Female;
        }

        public float GetBaseSuccessChance(Pawn patient, BodyPartRecord part)
        {
            return 1f;
        }

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            var hediffDef = context.Bill?.recipe?.addsHediff;
            if (hediffDef != null)
            {
                var hediff = context.Patient.health.AddHediff(hediffDef);
                result.AppliedHediffs.Add(hediff);
                Log.Message($"[YAMP] Installed IUD on {context.Patient.LabelShort}");
            }
        }
    }
}
