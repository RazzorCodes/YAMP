using System;
using System.Collections.Generic;
using Verse;

namespace YAMP.OperationSystem
{
    /// <summary>
    /// Result of an operation execution
    /// </summary>
    public class OperationResult
    {
        public bool Success { get; set; }
        public List<Thing> Products { get; set; } = new List<Thing>();
        public List<Hediff> AppliedHediffs { get; set; } = new List<Hediff>();
        public string FailureReason { get; set; }
        public Exception Error { get; set; }
    }
}
