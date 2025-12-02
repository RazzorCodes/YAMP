using System;
using System.Collections.Generic;
using Verse;

namespace YAMP.OperationSystem
{
    /// <summary>
    /// Context passed to operations containing all necessary data and hooks
    /// </summary>
    public class OperationContext
    {
        public Pawn Patient { get; set; }
        public BodyPartRecord BodyPart { get; set; }
        public List<Thing> Ingredients { get; set; }
        public ThingWithComps Facility { get; set; }
        public Pawn Surgeon { get; set; }

        // Customizable properties
        public float SuccessChance { get; set; } = 0.98f;

        // Hooks
        public Action<OperationContext> PreOperationHook { get; set; }
        public Action<OperationContext, OperationResult> PostOperationHook { get; set; }
        public Func<OperationContext, float> SuccessChanceCalculator { get; set; }
    }
}
