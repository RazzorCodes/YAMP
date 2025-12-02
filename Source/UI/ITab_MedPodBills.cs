using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace YAMP
{
    public class ITab_MedPodBills : ITab
    {
        private float viewHeight = 1000f;
        private Vector2 scrollPosition = Vector2.zero;
        private Bill mouseoverBill;

        private static readonly Vector2 WinSize = new Vector2(420f, 480f);

        public ITab_MedPodBills()
        {
            size = WinSize;
            labelKey = "TabBills";
        }

        protected Building_MedPod SelMedPod => (Building_MedPod)SelThing;

        protected override void FillTab()
        {
            Rect rect = new Rect(0f, 0f, WinSize.x, WinSize.y).ContractedBy(10f);

            // Draw header
            Text.Font = GameFont.Small;
            Rect headerRect = new Rect(rect.x, rect.y, rect.width, 30f);

            Pawn patient = SelMedPod.Container.GetPawn();
            if (patient != null)
            {
                Widgets.Label(headerRect, $"Patient: {patient.LabelShort}");
            }
            else
            {
                Widgets.Label(headerRect, "No patient in pod");
            }

            // Draw bill list
            Rect billListRect = new Rect(rect.x, headerRect.yMax + 5f, rect.width, rect.height - headerRect.height - 40f);
            DrawBillList(billListRect);

            // Draw add bill button
            Rect addButtonRect = new Rect(rect.x, billListRect.yMax + 5f, rect.width, 30f);
            if (patient != null && Widgets.ButtonText(addButtonRect, "Add Bill"))
            {
                OpenAddBillMenu();
            }
        }

        private void DrawBillList(Rect rect)
        {
            BillStack billStack = SelMedPod.BillStack;
            if (billStack == null || billStack.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "No bills");
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, viewHeight);

            // Wrap scroll view in try-finally to ensure EndScrollView is always called
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
            try
            {
                float curY = 0f;
                for (int i = 0; i < billStack.Count; i++)
                {
                    Bill bill = billStack[i];
                    Rect billRect = new Rect(0f, curY, viewRect.width, 50f);

                    DrawBill(bill, billRect, i);
                    curY += 55f;
                }

                viewHeight = curY;
            }
            finally
            {
                Widgets.EndScrollView();
            }
        }

        private void DrawBill(Bill bill, Rect rect, int index)
        {
            // Background
            if (index % 2 == 1)
            {
                Widgets.DrawLightHighlight(rect);
            }

            if (Mouse.IsOver(rect))
            {
                mouseoverBill = bill;
                Widgets.DrawHighlight(rect);
            }

            // Bill label
            Rect labelRect = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 100f, 20f);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, bill.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;

            // Status
            if (bill is Bill_Medical billMedical)
            {
                Rect statusRect = new Rect(rect.x + 5f, labelRect.yMax, rect.width - 100f, 20f);
                Text.Font = GameFont.Tiny;

                if (billMedical.Part != null)
                {
                    Widgets.Label(statusRect, $"Part: {billMedical.Part.Label}");
                }

                Text.Font = GameFont.Small;
            }

            // Delete button
            Rect deleteRect = new Rect(rect.xMax - 90f, rect.y + 10f, 80f, 30f);
            if (Widgets.ButtonText(deleteRect, "Delete"))
            {
                SelMedPod.BillStack.Delete(bill);
            }
        }

        private void OpenAddBillMenu()
        {
            Pawn patient = SelMedPod.Container.GetPawn();
            if (patient == null) return;

            List<FloatMenuOption> options = new List<FloatMenuOption>();

            // Get all medical recipes
            IEnumerable<RecipeDef> recipes = DefDatabase<RecipeDef>.AllDefs
                .Where(r => r.AllRecipeUsers != null &&
                           r.AllRecipeUsers.Any(t => t.defName == "Human" || t.race?.Humanlike == true));

            foreach (RecipeDef recipe in recipes)
            {
                // Check if recipe is applicable
                if (!recipe.AvailableNow || !recipe.AvailableOnNow(patient))
                {
                    continue;
                }

                // Create menu option
                string label = recipe.LabelCap;
                Action action = () =>
                {
                    if (!recipe.targetsBodyPart)
                    {
                        // No body part selection needed
                        Bill_Medical bill = new Bill_Medical(recipe, null);
                        SelMedPod.BillStack.AddBill(bill);
                    }
                    else
                    {
                        // Need to select body part
                        OpenBodyPartMenu(recipe, patient);
                    }
                };

                options.Add(new FloatMenuOption(label, action));
            }

            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("No available surgeries", null));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void OpenBodyPartMenu(RecipeDef recipe, Pawn patient)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            // Get applicable body parts
            IEnumerable<BodyPartRecord> parts = recipe.Worker.GetPartsToApplyOn(patient, recipe);

            foreach (BodyPartRecord part in parts)
            {
                string label = part.Label;
                Action action = () =>
                {
                    Bill_Medical bill = new Bill_Medical(recipe, null);
                    bill.Part = part;
                    SelMedPod.BillStack.AddBill(bill);
                };

                options.Add(new FloatMenuOption(label, action));
            }

            if (options.Count == 0)
            {
                options.Add(new FloatMenuOption("No applicable body parts", null));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }
    }
}