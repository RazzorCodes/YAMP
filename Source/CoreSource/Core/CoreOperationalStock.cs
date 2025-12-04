using System;
using System.Collections.Generic;

namespace YAMP.OperationSystem.Core
{
    /// <summary>
    /// Pure .NET Core logic for operational stock management.
    /// No RimWorld dependencies, no medicine type knowledge.
    /// </summary>
    public static class CoreOperationalStock
    {
        // ==================== CONFIGURATION ====================

        public const float MAX_BUFFER = 500f;
        public const int BUFFER_TICK_INTERVAL = 300;

        // ==================== BUFFER MANAGEMENT ====================

        /// <summary>
        /// Check if an item value can be buffered without overflow.
        /// </summary>
        public static bool CanBuffer(float itemValue, float currentBuffer, float maxBuffer)
        {
            return (currentBuffer + itemValue) <= maxBuffer;
        }

        /// <summary>
        /// Determine how much from buffer can be allocated.
        /// </summary>
        public static float CalculateBufferAllocation(float requested, float available)
        {
            return Math.Min(requested, available);
        }

        // ==================== STOCK COMPUTATIONS ====================

        /// <summary>
        /// Compute total stock from buffer and unused stock.
        /// </summary>
        public static float ComputeTotalStock(float buffer, float unusedStock)
        {
            return buffer + unusedStock;
        }

        /// <summary>
        /// Calculate required stock cost from ingredient counts.
        /// Ignores fixed ingredients.
        /// </summary>
        public static float CalculateRequiredStock(
            Dictionary<string, (int count, bool isFixed, bool isMedicine)> ingredients)
        {
            float total = 0f;

            foreach (var kvp in ingredients)
            {
                var (count, isFixed, isMedicine) = kvp.Value;

                // Skip fixed ingredients
                if (isFixed)
                    continue;

                // Only count if it's medicine
                if (isMedicine)
                    total += count;
            }

            return total;
        }

        // ==================== ALLOCATION LOGIC ====================

        /// <summary>
        /// Try to allocate amount from buffer. Returns allocated amount and remaining needed.
        /// </summary>
        public static (float allocated, float remaining) TryAllocateFromBuffer(
            float requested,
            float bufferAvailable)
        {
            float allocated = CalculateBufferAllocation(requested, bufferAvailable);
            float remaining = requested - allocated;

            return (allocated, remaining);
        }

        /// <summary>
        /// Calculate how many items to consume from a stack.
        /// </summary>
        public static int CalculateItemsToConsume(
            float stillNeeded,
            float perItemValue,
            int stackSize)
        {
            int needed = (int)Math.Ceiling(stillNeeded / perItemValue);
            return Math.Min(needed, stackSize);
        }

        // ==================== WAIT CONDITIONS ====================

        /// <summary>
        /// Check if operation should wait for more stock.
        /// </summary>
        public static bool ShouldWaitForStock(float required, float totalAvailable)
        {
            return totalAvailable < required;
        }
    }
}
