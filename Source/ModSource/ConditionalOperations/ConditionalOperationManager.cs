using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace YAMP.ConditionalOperations
{
    /// <summary>
    /// Manages conditional operations for a MedPod, checking conditions and auto-enqueuing surgeries
    /// </summary>
    public class ConditionalOperationManager : IExposable
    {
        private List<ConditionalOperation> _operations = new List<ConditionalOperation>();
        public List<ConditionalOperation> Operations => _operations;

        public ConditionalOperationManager()
        {
        }

        /// <summary>
        /// Adds a conditional operation to the manager
        /// </summary>
        public void AddOperation(ConditionalOperation operation)
        {
            if (operation != null && operation.recipe != null)
            {
                _operations.Add(operation);
            }
        }

        /// <summary>
        /// Removes a conditional operation from the manager
        /// </summary>
        public void RemoveOperation(ConditionalOperation operation)
        {
            _operations.Remove(operation);
        }

        public bool ShouldEnqueueOperation(Pawn pawn)
        {
            return _operations.Any(operation => operation.CheckCondition(pawn));
        }

        /// <summary>
        /// Checks all conditional operations and enqueues matching surgeries for the pawn
        /// </summary>
        public void CheckAndEnqueueOperations(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.health?.surgeryBills == null)
            {
                return;
            }

            var billStack = pawn.health.surgeryBills;

            foreach (var operation in _operations)
            {
                if (operation.CheckCondition(pawn))
                {
                    // Check if surgery is already in the bill stack to avoid excessive duplicates
                    bool alreadyExists = billStack.Bills.Any(b =>
                        b is Bill_Medical bm && bm.recipe == operation.recipe);

                    if (!alreadyExists)
                    {
                        // Create new medical bill
                        var bill = new Bill_Medical(operation.recipe, null);

                        // Use AddBill which properly initializes the bill with the pawn reference
                        billStack.AddBill(bill);

                        // Move to top for highest priority
                        billStack.Bills.Remove(bill);
                        billStack.Bills.Insert(0, bill);

                        Logger.Debug($"Auto-enqueued surgery: {operation.recipe.label} for {pawn.Name.ToStringShort} due to condition: {operation.conditionType}");
                    }
                }
            }
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref _operations, "operations", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.LoadingVars && _operations == null)
            {
                _operations = new List<ConditionalOperation>();
            }
        }
    }
}
