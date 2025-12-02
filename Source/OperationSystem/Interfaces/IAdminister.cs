using Verse;

namespace YAMP.OperationSystem
{
    /// <summary>
    /// Drug/item administration operations
    /// </summary>
    public interface IAdminister : IOperation
    {
        /// <summary>
        /// The drug or item definition to administer
        /// </summary>
        ThingDef ItemDef { get; }

        /// <summary>
        /// Number of items required
        /// </summary>
        int RequiredCount { get; }
    }
}
