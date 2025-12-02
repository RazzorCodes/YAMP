using System.Linq;
using RimWorld;
using Verse;

namespace YAMP.OperationSystem
{
    // ==================== REMOVE OPERATIONS ====================

    /// <summary>
    /// Remove body part (surgical amputation)
    /// </summary>
    public class RemoveBodyPartOperation : BaseOperation, ISurgery
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
                    GenPlace.TryPlaceThing(product, context.Facility.Position, context.Facility.Map,
                        ThingPlaceMode.Near);
                }
            }

            Log.Message($"[YAMP] Successfully removed {context.BodyPart.Label} from {context.Patient.LabelShort}");
        }

        private System.Collections.Generic.List<Thing> GenerateProducts(Pawn patient, BodyPartRecord part)
        {
            var products = new System.Collections.Generic.List<Thing>();

            // Natural body part
            if (part.def.spawnThingOnRemoved != null)
            {
                var product = ThingMaker.MakeThing(part.def.spawnThingOnRemoved);
                products.Add(product);
            }

            // Installed bionics/implants
            foreach (Hediff hediff in patient.health.hediffSet.hediffs.Where(h => h.Part == part))
            {
                if (hediff.def.spawnThingOnRemoved != null)
                {
                    var product = ThingMaker.MakeThing(hediff.def.spawnThingOnRemoved);
                    products.Add(product);
                }
            }

            return products;
        }
    }

    /// <summary>
    /// Remove body part by cutting (more brutal removal)
    /// </summary>
    public class RemoveBodyPartCutOperation : BaseOperation, ISurgery
    {
        public override string Name => "Remove Body Part (Cut)";

        public bool CanPerform(Pawn patient, BodyPartRecord part)
        {
            return part != null && !patient.health.hediffSet.PartIsMissing(part);
        }

        public float GetBaseSuccessChance(Pawn patient, BodyPartRecord part)
        {
            return 1f;
        }

        protected override void ExecuteOperation(OperationContext context, OperationResult result)
        {
            // Similar to regular removal but might cause more damage
            context.Patient.health.AddHediff(HediffDefOf.MissingBodyPart, context.BodyPart);
            Log.Message($"[YAMP] Cut off {context.BodyPart.Label} from {context.Patient.LabelShort}");
        }

        protected override void HandleFailure(OperationContext context, OperationResult result)
        {
            result.FailureReason = "Cutting procedure failed, causing extra damage";
            // More damage than regular surgery failure
            context.Patient.TakeDamage(new DamageInfo(DamageDefOf.Cut, 8, 0, -1, null, context.BodyPart));
        }
    }

    /// <summary>
    /// Remove many body parts by cutting
    /// </summary>
    public class RemoveBodyPartCutManyOperation : BaseOperation, ISurgery
    {
        public override string Name => "Remove Many Body Parts (Cut)";

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
            context.Patient.health.AddHediff(HediffDefOf.MissingBodyPart, context.BodyPart);
            Log.Message($"[YAMP] Cut off multiple parts from {context.Patient.LabelShort}");
        }
    }

    /// <summary>
    /// Remove hediff (scar, ailment, etc.)
    /// </summary>
    public class RemoveHediffOperation : BaseOperation, ISurgery
    {
        public override string Name => "Remove Hediff";

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
            // Hediff removal is handled by vanilla
            Log.Message($"[YAMP] Successfully removed hediff from {context.Patient.LabelShort}");
        }
    }

    /// <summary>
    /// Remove implant
    /// </summary>
    public class RemoveImplantOperation : BaseOperation, ISurgery
    {
        public override string Name => "Remove Implant";

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
            // Generate implant as product
            if (context.BodyPart != null)
            {
                foreach (var hediff in context.Patient.health.hediffSet.hediffs.Where(h => h.Part == context.BodyPart))
                {
                    if (hediff.def.spawnThingOnRemoved != null)
                    {
                        var product = ThingMaker.MakeThing(hediff.def.spawnThingOnRemoved);
                        result.Products.Add(product);

                        if (context.Facility != null)
                        {
                            GenPlace.TryPlaceThing(product, context.Facility.Position, context.Facility.Map,
                                ThingPlaceMode.Near);
                        }
                    }
                }
            }

            Log.Message($"[YAMP] Successfully removed implant from {context.Patient.LabelShort}");
        }
    }
}
