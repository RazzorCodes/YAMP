using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace YAMP
{
    public class ITab_MedPodMedicine : ITab
    {
        private float _scrollViewHeight;
        private Vector2 _scrollPosition;
        private static readonly Vector2 WinSize = new Vector2(400f, 400f);

        private Building_MedPod SelMedPod => (Building_MedPod)SelThing;

        public ITab_MedPodMedicine()
        {
            size = WinSize;
            labelKey = "TabStorage";
        }

        protected override void FillTab()
        {
            Building_MedPod medPod = SelMedPod;
            if (medPod == null) return;

            // Ensure dictionary is initialized
            if (medPod.medicineRanges == null)
            {
                medPod.medicineRanges = new Dictionary<string, IntRange>();
            }

            Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);

            // Header
            Rect headerRect = rect;
            headerRect.height = 30f;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(headerRect, "Medicine Storage Settings");
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            // Columns Header
            Rect colHeaderRect = new Rect(rect.x, headerRect.yMax + 5f, rect.width, 24f);
            float nameWidth = colHeaderRect.width - 24f - 60f - 60f - 10f; // Checkbox(24) + Min(60) + Max(60)

            Rect checkHeader = new Rect(colHeaderRect.x, colHeaderRect.y, 24f, 24f);
            Rect nameHeader = new Rect(checkHeader.xMax + 5f, colHeaderRect.y, nameWidth, 24f);
            Rect minHeader = new Rect(nameHeader.xMax, colHeaderRect.y, 60f, 24f);
            Rect maxHeader = new Rect(minHeader.xMax, colHeaderRect.y, 60f, 24f);

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(minHeader, "Min");
            Widgets.Label(maxHeader, "Max");
            Text.Anchor = TextAnchor.UpperLeft;

            // List
            Rect listRect = new Rect(rect.x, colHeaderRect.yMax + 5f, rect.width, rect.height - colHeaderRect.yMax - 5f);
            Rect viewRect = new Rect(0f, 0f, listRect.width - 16f, _scrollViewHeight);

            Widgets.BeginScrollView(listRect, ref _scrollPosition, viewRect);

            float curY = 0f;
            List<ThingDef> medicines = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.IsMedicine)
                .OrderByDescending(d => d.BaseMarketValue)
                .ToList();

            foreach (ThingDef med in medicines)
            {
                if (!medPod.medicineRanges.ContainsKey(med.defName))
                {
                    // Default range: 0-0 implies not allowed/no stock logic yet, or we can use -1 to signify 'not allowed'
                    // The user wanted a checkbox.
                    // Let's interpret: If Key exists, it's allowed. If not, it's disallowed?
                    // Or we store Min/Max. If not in dict, we assume not allowed?
                    // Better: Always have entry if allowed.
                    // Let's mimic what I wrote: "Key exists = tracked settings".
                    // But if unchecked, we should probably remove from dict or have a flag.
                    // The user said: "tickmark for accepting ... AND 2x textbox ... min and max"

                }

                bool isAllowed = medPod.medicineRanges.ContainsKey(med.defName);
                bool wasAllowed = isAllowed;

                Rect rowRect = new Rect(0f, curY, viewRect.width, 24f);
                if (curY + 24f > _scrollPosition.y && curY < _scrollPosition.y + listRect.height)
                {
                    if (isAllowed)
                    {
                        Widgets.DrawHighlightIfMouseover(rowRect);
                    }

                    // Checkbox
                    Rect checkRect = new Rect(0f, curY, 24f, 24f);
                    Widgets.Checkbox(checkRect.position, ref isAllowed);

                    // Name
                    Rect nameRect = new Rect(checkRect.xMax + 5f, curY, nameWidth, 24f);
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(nameRect, med.LabelCap);
                    Text.Anchor = TextAnchor.UpperLeft;

                    if (isAllowed)
                    {
                        // Logic for adding/removing
                        if (!wasAllowed)
                        {
                            medPod.medicineRanges[med.defName] = new IntRange(0, 20); // Default 0-20
                        }

                        // Textboxes
                        IntRange range = medPod.medicineRanges[med.defName];
                        int min = range.min;
                        int max = range.max;

                        Rect minRect = new Rect(nameRect.xMax, curY, 60f, 24f);
                        Rect maxRect = new Rect(minRect.xMax, curY, 60f, 24f);

                        string minBuffer = min.ToString();
                        string maxBuffer = max.ToString();

                        Widgets.TextFieldNumeric(minRect, ref min, ref minBuffer, 0, 9999);
                        Widgets.TextFieldNumeric(maxRect, ref max, ref maxBuffer, 0, 9999);

                        if (min != range.min || max != range.max)
                        {
                            medPod.medicineRanges[med.defName] = new IntRange(min, max);
                        }
                    }
                    else if (wasAllowed)
                    {
                        medPod.medicineRanges.Remove(med.defName);
                    }
                }

                curY += 24f;
            }

            _scrollViewHeight = curY;
            Widgets.EndScrollView();
        }
    }
}
