using System.Collections.Generic;
using RimWorld;
using Verse;
using YAMP;

namespace YAMP.OperationSystem.RimWorld
{
    /// <summary>
    /// Static utility class wrapping facility/stock operations.
    /// Provides RimWorld-agnostic interface for facility management.
    /// </summary>
    public static class FacilityHelper
    {
        // ==================== STOCK MANAGEMENT ====================

        /// <summary>
        /// Check if facility has required operational stock for recipe.
        /// </summary>
        public static bool HasRequiredStock(object facility, object recipe)
        {
            var f = facility as ThingWithComps;
            var r = recipe as RecipeDef;

            if (f == null || r == null)
                return true; // No facility/recipe means no requirements

            // Try to get OperationalStock component
            var stockComp = f.TryGetComp<Comp_PodOperate>();
            if (stockComp?.OperationalStock == null)
                return true; // No stock system means no requirements

            float required = OperationalStock.CalculateStockCost(r);
            return stockComp.OperationalStock.TotalStock >= required;
        }

        /// <summary>
        /// Consume operational stock for recipe.
        /// </summary>
        public static void ConsumeStock(object facility, object recipe)
        {
            var f = facility as ThingWithComps;
            var r = recipe as RecipeDef;

            if (f == null || r == null)
                return;

            var stockComp = f.TryGetComp<Comp_PodOperate>();
            if (stockComp?.OperationalStock == null)
                return;

            float required = OperationalStock.CalculateStockCost(r);
            stockComp.OperationalStock.TryConsumeStock(required);
        }

        // ==================== PART MANAGEMENT ====================
        // Note: Part reservation/management might be facility-specific
        // These are placeholder implementations

        /// <summary>
        /// Check if facility has required parts.
        /// </summary>
        public static bool HasRequiredParts(object facility, object[] thingDefs)
        {
            // Placeholder - part management may need custom implementation
            return true;
        }

        /// <summary>
        /// Reserve parts for operation.
        /// </summary>
        public static void ReserveParts(object facility, string reservationKey)
        {
            // Placeholder - part reservation may need custom implementation
        }

        /// <summary>
        /// Unreserve parts (rollback).
        /// </summary>
        public static void UnreserveParts(object facility, string reservationKey)
        {
            // Placeholder - part unreservation may need custom implementation
        }

        /// <summary>
        /// Consume reserved parts.
        /// </summary>
        public static void ConsumeParts(object facility, string reservationKey)
        {
            // Placeholder - part consumption may need custom implementation
        }

        /// <summary>
        /// Return reserved parts (on failure).
        /// </summary>
        public static void ReturnParts(object facility, string reservationKey)
        {
            // Placeholder - part return may need custom implementation
        }

        // ==================== PRODUCT HANDLING ====================

        /// <summary>
        /// Check if products can be stored in facility container.
        /// </summary>
        public static bool CanStoreInContainer(object facility, object[] products)
        {
            var medPod = facility as Building_MedPod;
            return medPod?.Container != null;
        }

        /// <summary>
        /// Store products in facility container.
        /// </summary>
        public static void StoreInContainer(object facility, object[] products)
        {
            var medPod = facility as Building_MedPod;
            if (medPod?.Container == null)
                return;

            foreach (var product in products)
            {
                var thing = product as Thing;
                if (thing != null)
                {
                    medPod.Container.GetDirectlyHeldThings().TryAddOrTransfer(thing);
                }
            }
        }

        /// <summary>
        /// Dump products near facility.
        /// </summary>
        public static void DumpNearFacility(object facility, object[] products)
        {
            var f = facility as ThingWithComps;
            if (f == null)
                return;

            foreach (var product in products)
            {
                var thing = product as Thing;
                if (thing != null)
                {
                    GenPlace.TryPlaceThing(thing, f.Position, f.Map, ThingPlaceMode.Near);
                }
            }
        }
    }
}
