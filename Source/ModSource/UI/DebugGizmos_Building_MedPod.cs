using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YAMP
{
    public partial class Building_MedPod
    {
        private IEnumerable<Gizmo> GetDebugGizmos()
        {
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: Fill Stock",
                    defaultDesc = "Fill operational stock buffer to maximum (1000).",
                    icon = TexCommand.GatherSpotActive,
                    action = () =>
                    {
                        _operationalStock.GetType()
                            .GetField("_buffer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.SetValue(_operationalStock, 1000f);
                        Logger.Debug("YAMP: Stock buffer filled to 1000");
                    }
                };

                yield return new Command_Action
                {
                    defaultLabel = "DEV: Empty Stock",
                    defaultDesc = "Empty operational stock buffer to zero.",
                    icon = TexCommand.ClearPrioritizedWork,
                    action = () =>
                    {
                        _operationalStock.GetType()
                            .GetField("_buffer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.SetValue(_operationalStock, 0f);
                        Logger.Debug("YAMP: Stock buffer emptied to 0");
                    }
                };
            }

            if (DebugSettings.godMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "GOD: Instant Fill",
                    defaultDesc = "Instantly fill stock buffer to maximum (god mode only).",
                    icon = TexCommand.DesirePower,
                    action = () =>
                    {
                        _operationalStock.GetType()
                            .GetField("_buffer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?.SetValue(_operationalStock, 1000f);
                        Messages.Message("Stock instantly filled to 1000", MessageTypeDefOf.TaskCompletion, false);
                    }
                };
            }
        }
    }
}

