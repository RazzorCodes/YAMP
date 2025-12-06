using System;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace YAMP
{
    /// <summary>
    /// Generic slider gizmo for setting target levels of any resource.
    /// Mimics vanilla Gizmo_SetFuelLevel but with configurable parameters instead of CompRefuelable dependency.
    /// </summary>
    public class Gizmo_SetLevel : Gizmo_Slider
    {
        // Core value accessors
        public Func<float> GetCurrentValue;
        public Func<float> GetMaxCapacity;
        public Func<float> GetTargetValue;
        public Action<float> SetTargetValue;

        // Display properties
        public string GizmoTitle;
        public Func<string> GetBarLabel;
        public Func<string> GetTooltipText;

        // Behavior flags
        public bool IsTargetConfigurable = true;

        // Optional auto-refill toggle
        public bool ShowAutoRefillToggle = false;
        public Func<bool> GetAutoRefillEnabled;
        public Action<bool> SetAutoRefillEnabled;
        public Texture2D AutoRefillIcon;
        public Func<string> GetAutoRefillTooltip;

        // Static dragging state
        private static bool draggingBar;

        protected override float Target
        {
            get => GetTargetValue() / GetMaxCapacity();
            set => SetTargetValue(value * GetMaxCapacity());
        }

        protected override float ValuePercent => GetCurrentValue() / GetMaxCapacity();

        protected override string Title => GizmoTitle;

        protected override bool IsDraggable => IsTargetConfigurable;

        protected override string BarLabel
        {
            get
            {
                if (GetBarLabel != null)
                    return GetBarLabel();
                return $"{GetCurrentValue().ToStringDecimalIfSmall()} / {GetMaxCapacity().ToStringDecimalIfSmall()}";
            }
        }

        protected override bool DraggingBar
        {
            get => Gizmo_SetLevel.draggingBar;
            set => Gizmo_SetLevel.draggingBar = value;
        }

        protected override string GetTooltip()
        {
            if (GetTooltipText != null)
                return GetTooltipText();
            return GizmoTitle ?? "Set target level";
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            if (!ShowAutoRefillToggle)
                return base.GizmoOnGUI(topLeft, maxWidth, parms);

            KeyCode keyCode = KeyBindingDefOf.Command_ItemForbid == null ? KeyCode.None : KeyBindingDefOf.Command_ItemForbid.MainKey;
            if (keyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(keyCode) && KeyBindingDefOf.Command_ItemForbid.KeyDownEvent)
            {
                ToggleAutoRefill();
                Event.current.Use();
            }
            return base.GizmoOnGUI(topLeft, maxWidth, parms);
        }

        protected override void DrawHeader(Rect headerRect, ref bool mouseOverElement)
        {
            if (ShowAutoRefillToggle && GetAutoRefillEnabled != null && SetAutoRefillEnabled != null)
            {
                headerRect.xMax -= 24f;
                Rect rect = new Rect(headerRect.xMax, headerRect.y, 24f, 24f);

                // Draw icon (use provided icon or default)
                Texture iconTexture = AutoRefillIcon ?? BaseContent.BadTex;
                GUI.DrawTexture(rect, iconTexture);

                // Draw checkbox overlay
                bool autoRefillEnabled = GetAutoRefillEnabled();
                GUI.DrawTexture(
                    new Rect(rect.center.x, rect.y, rect.width / 2f, rect.height / 2f),
                    autoRefillEnabled ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex
                );

                if (Widgets.ButtonInvisible(rect))
                    ToggleAutoRefill();

                if (Mouse.IsOver(rect))
                {
                    Widgets.DrawHighlight(rect);
                    if (GetAutoRefillTooltip != null)
                        TooltipHandler.TipRegion(rect, new Func<string>(GetAutoRefillTooltip), 828267373);
                    mouseOverElement = true;
                }
            }
            base.DrawHeader(headerRect, ref mouseOverElement);
        }

        private void ToggleAutoRefill()
        {
            if (GetAutoRefillEnabled != null && SetAutoRefillEnabled != null)
            {
                bool currentValue = GetAutoRefillEnabled();
                SetAutoRefillEnabled(!currentValue);

                if (!currentValue)
                    SoundDefOf.Tick_High.PlayOneShotOnCamera();
                else
                    SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            }
        }
    }
}
