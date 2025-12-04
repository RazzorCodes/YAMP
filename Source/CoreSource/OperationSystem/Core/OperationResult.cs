using System;
using System.Collections.Generic;

namespace YAMP.OperationSystem.Core
{
    /// <summary>
    /// Mutable operation result that can be modified by hooks.
    /// Generic design with object lists makes it RimWorld-agnostic.
    /// </summary>
    public class OperationResult
    {
        public bool Success { get; set; }
        public List<object> Products { get; set; } = new List<object>();
        public List<object> AppliedEffects { get; set; } = new List<object>();
        public string FailureReason { get; set; }
        public Exception Error { get; set; }
    }
}
