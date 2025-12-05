using System;
using RimWorld;
using Verse;

namespace YAMP.ConditionalOperations
{
    /// <summary>
    /// Types of conditions that can trigger automatic surgeries
    /// </summary>
    public enum ConditionType
    {
        BloodLoss
        // Future: Pain, Consciousness, Infection, etc.
    }

    /// <summary>
    /// Comparison operators for condition checking
    /// </summary>
    public enum OperatorType
    {
        GreaterThan,
        LessThan,
        Equal,
        GreaterThanOrEqual,
        LessThanOrEqual
    }

    /// <summary>
    /// Represents a conditional operation that auto-enqueues a surgery when conditions are met
    /// </summary>
    public class ConditionalOperation : IExposable
    {
        public ConditionType conditionType;
        public OperatorType operatorType;
        public float threshold;
        public RecipeDef recipe;

        public ConditionalOperation()
        {
            // Parameterless constructor for serialization
        }

        public ConditionalOperation(ConditionType conditionType, OperatorType operatorType, float threshold, RecipeDef recipe)
        {
            this.conditionType = conditionType;
            this.operatorType = operatorType;
            this.threshold = threshold;
            this.recipe = recipe;
        }

        /// <summary>
        /// Checks if the condition is met for the given pawn
        /// </summary>
        public bool CheckCondition(Pawn pawn)
        {
            if (pawn == null || pawn.Dead) return false;

            float currentValue = GetConditionValue(pawn);
            if (float.IsNaN(currentValue)) return false;

            return CompareValues(currentValue, threshold, operatorType);
        }

        private float GetConditionValue(Pawn pawn)
        {
            switch (conditionType)
            {
                case ConditionType.BloodLoss:
                    var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
                    return hediff?.Severity ?? 0f;
                default:
                    return float.NaN;
            }
        }

        private bool CompareValues(float current, float threshold, OperatorType op)
        {
            switch (op)
            {
                case OperatorType.GreaterThan:
                    return current > threshold;
                case OperatorType.LessThan:
                    return current < threshold;
                case OperatorType.Equal:
                    return Math.Abs(current - threshold) < 0.01f;
                case OperatorType.GreaterThanOrEqual:
                    return current >= threshold;
                case OperatorType.LessThanOrEqual:
                    return current <= threshold;
                default:
                    return false;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref conditionType, "conditionType", ConditionType.BloodLoss);
            Scribe_Values.Look(ref operatorType, "operatorType", OperatorType.GreaterThan);
            Scribe_Values.Look(ref threshold, "threshold", 0f);
            Scribe_Defs.Look(ref recipe, "recipe");
        }

        public string GetLabel()
        {
            string conditionName = conditionType.ToString();
            string operatorSymbol = GetOperatorSymbol();
            string thresholdLabel = GetThresholdLabel();
            string recipeLabel = recipe?.label ?? "None";

            return $"{conditionName} {operatorSymbol} {thresholdLabel} → {recipeLabel}";
        }

        private string GetOperatorSymbol()
        {
            switch (operatorType)
            {
                case OperatorType.GreaterThan: return ">";
                case OperatorType.LessThan: return "<";
                case OperatorType.Equal: return "=";
                case OperatorType.GreaterThanOrEqual: return ">=";
                case OperatorType.LessThanOrEqual: return "<=";
                default: return "?";
            }
        }

        private string GetThresholdLabel()
        {
            // For blood loss, show named thresholds
            if (conditionType == ConditionType.BloodLoss)
            {
                if (threshold <= 0.01f) return "None (0%)";
                if (threshold <= 0.25f) return "Mild (25%)";
                if (threshold <= 0.50f) return "Severe (50%)";
                if (threshold <= 0.75f) return "Extreme (75%)";
                return $"{threshold:P0}";
            }
            return $"{threshold:F2}";
        }
    }
}
