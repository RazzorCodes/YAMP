using Verse;

namespace YAMP.OperationSystem
{
    /// <summary>
    /// Surgery-specific operations (remove/install body parts, implants)
    /// </summary>
    public interface ISurgery : IOperation
    {
        /// <summary>
        /// Check if this surgery can be performed on the target
        /// </summary>
        bool CanPerform(Pawn patient, BodyPartRecord part);

        /// <summary>
        /// Get the base success chance before modifiers
        /// </summary>
        float GetBaseSuccessChance(Pawn patient, BodyPartRecord part);
    }
}
