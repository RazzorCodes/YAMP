using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using YAMP.ConditionalOperations;

namespace YAMP
{
    /// <summary>
    /// Tab for managing conditional surgery operations on the MedPod
    /// </summary>
    public class ITab_MedPodBills : ITab
    {
        private Vector2 scrollPosition;
        private const float LineHeight = 30f;
        private const float ButtonWidth = 100f;

        public ITab_MedPodBills()
        {
            size = new Vector2(500f, 480f);
            labelKey = "TabBills";
        }

        private Building_MedPod SelMedPod => (Building_MedPod)SelThing;

        protected override void FillTab()
        {
            var medPod = SelMedPod;
            if (medPod == null) return;

            var condComp = medPod.GetComp<Comp_PodConditionals>();
            if (condComp == null) return;

            var manager = condComp.Manager;
            Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);

            // Title
            Widgets.Label(rect.TopPartPixels(30f), "Conditional Operations");

            Rect contentRect = rect;
            contentRect.yMin += 35f;

            // Add button
            Rect addButtonRect = new Rect(contentRect.x, contentRect.y, ButtonWidth, 24f);
            if (Widgets.ButtonText(addButtonRect, "Add"))
            {
                Find.WindowStack.Add(new Dialog_AddConditionalOperation(manager));
            }

            contentRect.yMin += 30f;

            // List of operations
            Rect listRect = contentRect;
            if (manager.Operations.Count == 0)
            {
                Widgets.Label(listRect, "No conditional operations configured.");
            }
            else
            {
                float viewHeight = manager.Operations.Count * LineHeight;
                Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, viewHeight);

                Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);

                float y = 0f;
                foreach (var operation in manager.Operations.ToList())
                {
                    Rect lineRect = new Rect(0f, y, viewRect.width, LineHeight);
                    DrawOperationLine(lineRect, operation, manager);
                    y += LineHeight;
                }

                Widgets.EndScrollView();
            }
        }

        private void DrawOperationLine(Rect rect, ConditionalOperation operation, ConditionalOperationManager manager)
        {
            // Label
            Rect labelRect = new Rect(rect.x, rect.y + 5f, rect.width - 110f, LineHeight - 10f);
            Widgets.Label(labelRect, operation.GetLabel());

            // Delete button
            Rect deleteRect = new Rect(rect.xMax - 100f, rect.y + 3f, 90f, LineHeight - 6f);
            if (Widgets.ButtonText(deleteRect, "Delete"))
            {
                manager.RemoveOperation(operation);
            }

            Widgets.DrawLineHorizontal(rect.x, rect.yMax, rect.width);
        }
    }

    /// <summary>
    /// Dialog for adding a new conditional operation
    /// </summary>
    public class Dialog_AddConditionalOperation : Window
    {
        private ConditionalOperationManager manager;
        private ConditionType selectedCondition = ConditionType.BloodLoss;
        private OperatorType selectedOperator = OperatorType.GreaterThan;
        private float selectedThreshold = 0.5f; // Default to Severe (50%)
        private RecipeDef selectedRecipe = null;

        public override Vector2 InitialSize => new Vector2(400f, 300f);

        public Dialog_AddConditionalOperation(ConditionalOperationManager manager)
        {
            this.manager = manager;
            doCloseButton = true;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            // Title
            Text.Font = GameFont.Medium;
            listing.Label("Add Conditional Operation");
            Text.Font = GameFont.Small;
            listing.Gap();

            // Condition dropdown
            if (listing.ButtonText($"Condition: {selectedCondition}"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (ConditionType type in System.Enum.GetValues(typeof(ConditionType)))
                {
                    options.Add(new FloatMenuOption(type.ToString(), () => selectedCondition = type));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // Operator dropdown
            if (listing.ButtonText($"Operator: {GetOperatorSymbol(selectedOperator)}"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (OperatorType type in System.Enum.GetValues(typeof(OperatorType)))
                {
                    options.Add(new FloatMenuOption(GetOperatorSymbol(type), () => selectedOperator = type));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // Threshold dropdown
            if (listing.ButtonText($"Threshold: {GetThresholdLabel(selectedThreshold)}"))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>
                {
                    new FloatMenuOption("None (0%)", () => selectedThreshold = 0f),
                    new FloatMenuOption("Mild (25%)", () => selectedThreshold = 0.25f),
                    new FloatMenuOption("Severe (50%)", () => selectedThreshold = 0.5f),
                    new FloatMenuOption("Extreme (75%)", () => selectedThreshold = 0.75f)
                };
                Find.WindowStack.Add(new FloatMenu(options));
            }

            // Recipe selector
            if (listing.ButtonText(selectedRecipe == null ? "Select Surgery..." : selectedRecipe.label))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();

                // Find Recipe_BloodTransfusion

                // and other surgery recipes
                var recipes = DefDatabase<RecipeDef>.AllDefsListForReading
                    .Where(r => r.defName.Contains("BloodTransfusion") || r.IsSurgery)
                    .OrderBy(r => r.label);

                foreach (var recipe in recipes)
                {
                    options.Add(new FloatMenuOption(recipe.label, () => selectedRecipe = recipe));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }

            listing.Gap();

            // Add button
            if (listing.ButtonText("Add Operation"))
            {
                if (selectedRecipe != null)
                {
                    var operation = new ConditionalOperation(
                        selectedCondition,
                        selectedOperator,
                        selectedThreshold,
                        selectedRecipe
                    );
                    manager.AddOperation(operation);
                    Close();
                }
                else
                {
                    Messages.Message("Please select a surgery recipe", MessageTypeDefOf.RejectInput, false);
                }
            }

            listing.End();
        }

        private string GetOperatorSymbol(OperatorType op)
        {
            switch (op)
            {
                case OperatorType.GreaterThan: return ">";
                case OperatorType.LessThan: return "<";
                case OperatorType.Equal: return "=";
                case OperatorType.GreaterThanOrEqual: return ">=";
                case OperatorType.LessThanOrEqual: return "<=";
                default: return "?";
            }
        }

        private string GetThresholdLabel(float threshold)
        {
            if (threshold <= 0.01f) return "None (0%)";
            if (threshold <= 0.25f) return "Mild (25%)";
            if (threshold <= 0.50f) return "Severe (50%)";
            if (threshold <= 0.75f) return "Extreme (75%)";
            return $"{threshold:P0}";
        }
    }
}