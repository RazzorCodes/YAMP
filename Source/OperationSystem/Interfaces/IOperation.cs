using System.Collections.Generic;
using Verse;

namespace YAMP.OperationSystem
{
    /// <summary>
    /// Base interface for all medical operations (surgeries, drug administration, etc.)
    /// </summary>
    public interface IOperation
    {
        /// <summary>
        /// Name of the operation for logging/display
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Executes the operation and returns products (removed organs, etc.)
        /// </summary>
        OperationResult Perform(OperationContext context);
    }
}