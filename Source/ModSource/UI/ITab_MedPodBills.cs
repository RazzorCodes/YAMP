using System;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace YAMP
{
    /// <summary>
    /// Helper class to access protected members of ITab_Pawn_Health
    /// </summary>
    public class ITab_Pawn_Health_Helper : ITab_Pawn_Health
    {
        private static PropertyInfo selThingProperty;
        private static FieldInfo selThingBackingField;

        public Thing GetSelThingPublic()
        {
            return SelThing;
        }

        public void SetSelThingPublic(Thing thing)
        {
            // Try to set via property first (reflection can sometimes bypass read-only)
            if (selThingProperty == null)
            {
                selThingProperty = typeof(ITab).GetProperty("SelThing", BindingFlags.Public | BindingFlags.Instance);
            }

            if (selThingProperty != null)
            {
                try
                {
                    selThingProperty.SetValue(this, thing);
                    return; // Success!
                }
                catch (Exception ex)
                {
                    // Property is read-only, try backing field approach
                    Log.Warning($"[YAMP] Property SetValue failed: {ex.Message}");
                }
            }

            // If property setting failed, try to find and set the backing field
            // Search through all types in the inheritance hierarchy
            if (selThingBackingField == null)
            {
                Type currentType = typeof(ITab);
                Thing currentSelThing = SelThing; // Get current value to match against

                // Search through inheritance hierarchy
                while (currentType != null && selThingBackingField == null)
                {
                    // Get all fields (including inherited ones)
                    FieldInfo[] fields = currentType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    foreach (FieldInfo field in fields)
                    {
                        // Check if field type matches Thing or is assignable
                        if (typeof(Thing).IsAssignableFrom(field.FieldType) || field.FieldType == typeof(object))
                        {
                            try
                            {
                                object fieldValue = field.GetValue(this);
                                // If this field's value matches SelThing, it's likely the backing field
                                if (fieldValue == currentSelThing)
                                {
                                    selThingBackingField = field;
                                    Log.Message($"[YAMP] Found SelThing backing field: {field.Name} in {currentType.Name}");
                                    break;
                                }
                            }
                            catch
                            {
                                // Continue searching
                            }
                        }
                    }

                    if (selThingBackingField == null)
                    {
                        currentType = currentType.BaseType;
                    }
                }
            }

            if (selThingBackingField != null)
            {
                try
                {
                    selThingBackingField.SetValue(this, thing);
                }
                catch (Exception ex)
                {
                    Log.Error($"[YAMP] Failed to set SelThing backing field: {ex.Message}");
                }
            }
            else
            {
                // SelThing is managed by the inspector window system, but we're using global selection as a workaround
                // This is expected and not an error - the global selection change handles it
                // Log.Warning("[YAMP] Could not find SelThing property or backing field. Using global selection workaround.");
            }
        }

        public void FillTabPublic()
        {
            FillTab();
        }

        public Vector2 GetSizePublic()
        {
            return size;
        }
    }

    public class ITab_MedPodBills : ITab
    {
        private static ITab_Pawn_Health_Helper pawnHealthTab;

        public ITab_MedPodBills()
        {
            // Get size from RimWorld's ITab_Pawn_Health to match its dimensions
            // This ensures the UI displays correctly with proper width and height
            if (pawnHealthTab == null)
            {
                pawnHealthTab = new ITab_Pawn_Health_Helper();
            }
            Vector2 baseSize = pawnHealthTab.GetSizePublic(); // Use RimWorld's native size

            // Add extra width to accommodate mod buttons (like Dubs Mint Menus) and ensure close button is visible
            // RimWorld's default is typically around 630x510, we add extra width for mod compatibility
            size = new Vector2(baseSize.x, baseSize.y);
            labelKey = "TabBills";
        }

        protected Building_MedPod SelMedPod => (Building_MedPod)SelThing;
        protected override void FillTab()
        {
            // Resize window when tab is opened to ensure proper dimensions
            if (pawnHealthTab == null)
            {
                pawnHealthTab = new ITab_Pawn_Health_Helper();
            }
            Vector2 baseSize = pawnHealthTab.GetSizePublic();
            size = new Vector2(baseSize.x + 250f, baseSize.y);

            Pawn patient = SelMedPod.Container.GetPawn();

            if (patient == null)
            {
                // No patient case - show simple message
                Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "No patient in pod");
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            // Delegate to RimWorld's ITab_Pawn_Health to render bills
            // This ensures mod compatibility and uses RimWorld's native UI
            try
            {
                // Ensure helper instance exists (already created above)

                // Store original selection
                Thing originalSelectedThing = Find.Selector.SingleSelectedThing;

                try
                {
                    // Temporarily change global selection to the pawn
                    // This allows ITab_Pawn_Health to access the pawn via SelThing
                    if (originalSelectedThing != patient)
                    {
                        Find.Selector.ClearSelection();
                        Find.Selector.Select(patient);
                    }

                    // Try to set SelThing on the helper instance
                    pawnHealthTab.SetSelThingPublic(patient);

                    // Delegate to RimWorld's FillTab - this will render the bills UI
                    pawnHealthTab.FillTabPublic();
                }
                finally
                {
                    // Restore original selection
                    if (originalSelectedThing != patient && originalSelectedThing != null)
                    {
                        Find.Selector.ClearSelection();
                        Find.Selector.Select(originalSelectedThing);
                    }
                    else if (originalSelectedThing == null && Find.Selector.SingleSelectedThing == patient)
                    {
                        // Restore to no selection if it was null originally
                        Find.Selector.ClearSelection();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[YAMP] Error delegating to RimWorld's ITab_Pawn_Health: {ex}");
                // Fallback: show error message
                Rect rect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, $"Error loading bills: {ex.Message}");
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }

    }
}