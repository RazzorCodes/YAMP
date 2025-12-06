using Xunit;
using YAMP.OperationSystem.Core;
using System.Collections.Generic;

namespace YAMP.Tests
{
    public class CoreOperationalStockTests
    {
        // ==================== BUFFER MANAGEMENT TESTS ====================

        [Fact]
        public void CanBuffer_WithinLimit_ReturnsTrue()
        {
            // Arrange
            float itemValue = 10f;
            float currentBuffer = 400f;
            float maxBuffer = 500f;

            // Act
            bool result = CoreOperationalStock.CanBuffer(itemValue, currentBuffer, maxBuffer);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanBuffer_ExactlyAtLimit_ReturnsTrue()
        {
            // Arrange
            float itemValue = 100f;
            float currentBuffer = 400f;
            float maxBuffer = 500f;

            // Act
            bool result = CoreOperationalStock.CanBuffer(itemValue, currentBuffer, maxBuffer);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CanBuffer_ExceedsLimit_ReturnsFalse()
        {
            // Arrange
            float itemValue = 101f;
            float currentBuffer = 400f;
            float maxBuffer = 500f;

            // Act
            bool result = CoreOperationalStock.CanBuffer(itemValue, currentBuffer, maxBuffer);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CalculateBufferAllocation_SufficientBuffer_ReturnsRequested()
        {
            // Arrange
            float requested = 50f;
            float available = 100f;

            // Act
            float result = CoreOperationalStock.CalculateBufferAllocation(requested, available);

            // Assert
            Assert.Equal(50f, result);
        }

        [Fact]
        public void CalculateBufferAllocation_InsufficientBuffer_ReturnsAvailable()
        {
            // Arrange
            float requested = 150f;
            float available = 100f;

            // Act
            float result = CoreOperationalStock.CalculateBufferAllocation(requested, available);

            // Assert
            Assert.Equal(100f, result);
        }

        // ==================== STOCK COMPUTATION TESTS ====================

        [Fact]
        public void ComputeTotalStock_AddsTwoValues()
        {
            // Act
            float result = CoreOperationalStock.ComputeTotalStock(100f, 250f);

            // Assert
            Assert.Equal(350f, result);
        }

        [Fact]
        public void CalculateRequiredStock_IgnoresFixedIngredients()
        {
            // Arrange
            var ingredients = new Dictionary<string, (int count, bool isFixed, bool isMedicine)>
            {
                { "Ingredient1", (5, true, true) },  // Fixed, should be ignored
                { "Ingredient2", (10, false, true) }, // Not fixed, should count
                { "Ingredient3", (3, false, true) }  // Not fixed, should count
            };

            // Act
            float result = CoreOperationalStock.CalculateRequiredStock(ingredients);

            // Assert
            Assert.Equal(13f, result); // 10 + 3 = 13
        }

        [Fact]
        public void CalculateRequiredStock_IgnoresNonMedicine()
        {
            // Arrange
            var ingredients = new Dictionary<string, (int count, bool isFixed, bool isMedicine)>
            {
                { "Ingredient1", (5, false, false) }, // Not medicine, should be ignored
                { "Ingredient2", (10, false, true) }  // Medicine, should count
            };

            // Act
            float result = CoreOperationalStock.CalculateRequiredStock(ingredients);

            // Assert
            Assert.Equal(10f, result);
        }

        // ==================== ALLOCATION TESTS ====================

        [Fact]
        public void TryAllocateFromBuffer_SufficientBuffer_AllocatesFullAmount()
        {
            // Arrange
            float requested = 50f;
            float available = 100f;

            // Act
            var (allocated, remaining) = CoreOperationalStock.TryAllocateFromBuffer(requested, available);

            // Assert
            Assert.Equal(50f, allocated);
            Assert.Equal(0f, remaining);
        }

        [Fact]
        public void TryAllocateFromBuffer_InsufficientBuffer_AllocatesPartial()
        {
            // Arrange
            float requested = 150f;
            float available = 100f;

            // Act
            var (allocated, remaining) = CoreOperationalStock.TryAllocateFromBuffer(requested, available);

            // Assert
            Assert.Equal(100f, allocated);
            Assert.Equal(50f, remaining);
        }

        [Fact]
        public void CalculateItemsToConsume_ExactMatch_ReturnsExactCount()
        {
            // Arrange
            float stillNeeded = 30f;
            float perItemValue = 10f;
            int stackSize = 10;

            // Act
            int result = CoreOperationalStock.CalculateItemsToConsume(stillNeeded, perItemValue, stackSize);

            // Assert
            Assert.Equal(3, result);
        }

        [Fact]
        public void CalculateItemsToConsume_RoundsUp()
        {
            // Arrange
            float stillNeeded = 25f;
            float perItemValue = 10f;
            int stackSize = 10;

            // Act
            int result = CoreOperationalStock.CalculateItemsToConsume(stillNeeded, perItemValue, stackSize);

            // Assert
            Assert.Equal(3, result); // Ceil(25/10) = 3
        }

        [Fact]
        public void CalculateItemsToConsume_LimitedByStackSize_ReturnsStackSize()
        {
            // Arrange
            float stillNeeded = 100f;
            float perItemValue = 10f;
            int stackSize = 5;

            // Act
            int result = CoreOperationalStock.CalculateItemsToConsume(stillNeeded, perItemValue, stackSize);

            // Assert
            Assert.Equal(5, result); // Limited by stack
        }

        // ==================== WAIT CONDITION TESTS ====================

        [Fact]
        public void ShouldWaitForStock_Insufficient_ReturnsTrue()
        {
            // Act
            bool result = CoreOperationalStock.ShouldWaitForStock(100f, 50f);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ShouldWaitForStock_Sufficient_ReturnsFalse()
        {
            // Act
            bool result = CoreOperationalStock.ShouldWaitForStock(50f, 100f);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldWaitForStock_ExactAmount_ReturnsFalse()
        {
            // Act
            bool result = CoreOperationalStock.ShouldWaitForStock(100f, 100f);

            // Assert
            Assert.False(result);
        }
    }
}
