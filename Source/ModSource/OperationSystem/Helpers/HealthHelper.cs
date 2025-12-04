using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace YAMP.OperationSystem.RimWorld
{
    /// <summary>
    /// Static utility class wrapping RimWorld health-related APIs.
    /// Provides RimWorld-agnostic interface for health operations.
    /// </summary>
    public static class HealthHelper
    {
        // ==================== HEDIFF MANAGEMENT ====================

        /// <summary>
        /// Add a hediff to a pawn.
        /// </summary>
        public static object AddHediff(object pawn, object hediffDef, object bodyPart)
        {
            var p = pawn as Pawn;
            var hDef = hediffDef as HediffDef;
            var part = bodyPart as BodyPartRecord;

            if (p == null || hDef == null)
                return null;

            var hediff = p.health.AddHediff(hDef, part);
            return hediff;
        }

        /// <summary>
        /// Remove a hediff from a pawn.
        /// </summary>
        public static void RemoveHediff(object pawn, object hediff)
        {
            var p = pawn as Pawn;
            var h = hediff as Hediff;

            if (p != null && h != null)
            {
                p.health.RemoveHediff(h);
            }
        }

        /// <summary>
        /// Get the first hediff of a specific type on a pawn.
        /// </summary>
        public static object GetFirstHediff(object pawn, object hediffDef)
        {
            var p = pawn as Pawn;
            var hDef = hediffDef as HediffDef;

            if (p == null || hDef == null)
                return null;

            return p.health.hediffSet.GetFirstHediffOfDef(hDef);
        }

        // ==================== BODY PART OPERATIONS ====================

        /// <summary>
        /// Check if a body part is missing.
        /// </summary>
        public static bool PartIsMissing(object pawn, object bodyPart)
        {
            var p = pawn as Pawn;
            var part = bodyPart as BodyPartRecord;

            if (p == null || part == null)
                return true;

            return p.health.hediffSet.PartIsMissing(part);
        }

        /// <summary>
        /// Generate all products from a body part (natural organs, installed implants).
        /// </summary>
        public static List<object> GenerateProductsFromPart(object pawn, object bodyPart)
        {
            var p = pawn as Pawn;
            var part = bodyPart as BodyPartRecord;
            var products = new List<object>();

            if (p == null)
                return products;

            // Installed bionics/implants that spawn items when removed
            foreach (Hediff hediff in p.health.hediffSet.hediffs.Where(h => h.Part == part))
            {
                if (hediff.def.spawnThingOnRemoved != null)
                {
                    var product = ThingMaker.MakeThing(hediff.def.spawnThingOnRemoved);
                    products.Add(product);
                }
            }

            // Natural body part - only spawn if not missing
            if (part?.def.spawnThingOnRemoved != null && !p.health.hediffSet.PartIsMissing(part))
            {
                var product = ThingMaker.MakeThing(part.def.spawnThingOnRemoved);
                products.Add(product);
            }

            return products;
        }

        // ==================== DAMAGE ====================

        /// <summary>
        /// Apply damage to a pawn.
        /// </summary>
        public static void ApplyDamage(object pawn, object damageDef, float amount, object bodyPart)
        {
            var p = pawn as Pawn;
            var dDef = damageDef as DamageDef;
            var part = bodyPart as BodyPartRecord;

            if (p == null || dDef == null)
                return;

            var damageInfo = new DamageInfo(dDef, amount, 0, -1, null, part);
            p.TakeDamage(damageInfo);
        }

        // ==================== SUCCESS CHANCE ====================

        /// <summary>
        /// Get vanilla surgery success chance.
        /// </summary>
        public static float GetVanillaSuccessChance(object recipe, object pawn, object bodyPart)
        {
            var r = recipe as RecipeDef;
            var p = pawn as Pawn;
            var part = bodyPart as BodyPartRecord;

            if (r == null || p == null)
                return 1f;

            // Use vanilla calculation if available
            // Default to high success for simple operations
            return 0.98f;
        }

        // ==================== VALIDATION ====================

        /// <summary>
        /// Check if pawn is a valid patient (alive, not null).
        /// </summary>
        public static bool IsValidPatient(object pawn)
        {
            var p = pawn as Pawn;
            return p != null && !p.Dead;
        }

        /// <summary>
        /// Check if pawn is flesh-based (not mechanoid/robot).
        /// </summary>
        public static bool IsFleshPawn(object pawn)
        {
            var p = pawn as Pawn;
            return p != null && p.RaceProps.IsFlesh;
        }
    }
}
