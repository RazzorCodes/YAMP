using Verse;

namespace YAMP.OperationSystem
{
    /// <summary>
    /// Extract operations (hemogen, ovum)
    /// </summary>
    public interface IExtract : IOperation
    {
        /// <summary>
        /// Check if extraction can be performed
        /// </summary>
        bool CanExtract(Pawn patient);

        /// <summary>
        /// Get the base success chance for extraction
        /// </summary>
        float GetBaseSuccessChance(Pawn patient);
    }
}
