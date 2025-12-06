namespace YAMP.OperationSystem.Core
{
    /// <summary>
    /// Interface for operation pipeline executor.
    /// </summary>
    public interface IOperationExecutor
    {
        /// <summary>
        /// Execute an operation through the hook pipeline.
        /// </summary>
        OperationResult Execute(IOperation operation, OperationContext context);
    }
}
