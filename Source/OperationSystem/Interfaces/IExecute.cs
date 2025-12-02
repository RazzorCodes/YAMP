using Verse;

namespace YAMP.OperationSystem
{
    /// <summary>
    /// Execute operations (euthanasia, etc.)
    /// </summary>
    public interface IExecute : IOperation
    {
        /// <summary>
        /// Check if execution can be performed
        /// </summary>
        bool CanExecute(Pawn patient);
    }
}
