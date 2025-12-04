using System;
using UnityEngine;
using Verse;
using YAMP.Settings;

namespace YAMP
{
    /// <summary>
    /// Main mod class for YAMP. Handles mod initialization and settings UI.
    /// </summary>
    public class YAMPMod : Mod
    {
        private YAMPSettings settings;

        public YAMPMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<YAMPSettings>();

            // Initialize Logger with the loaded settings
            Logger.Level = settings.logLevel;
        }

        /// <summary>
        /// The name that appears in the mod settings list.
        /// </summary>
        public override string SettingsCategory()
        {
            return "YAMP";
        }

        /// <summary>
        /// Renders the settings window contents.
        /// </summary>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            // Header
            listingStandard.Label("YAMP Logging Settings");
            listingStandard.GapLine();
            listingStandard.Gap();

            // Description
            Text.Font = GameFont.Small;
            listingStandard.Label("Select the minimum log level to display in the console:");
            listingStandard.Gap();

            // Store the previous selection to detect changes
            Logger.LogLevel previousLevel = settings.logLevel;

            // Radio buttons for each log level
            foreach (Logger.LogLevel level in Enum.GetValues(typeof(Logger.LogLevel)))
            {
                string label = GetLogLevelLabel(level);
                string description = GetLogLevelDescription(level);

                if (listingStandard.RadioButton($"{label} - {description}", settings.logLevel == level))
                {
                    settings.logLevel = level;
                }
            }

            // If the setting changed, update the Logger immediately
            if (settings.logLevel != previousLevel)
            {
                Logger.Level = settings.logLevel;
                Logger.Info($"Log level changed to: {GetLogLevelLabel(settings.logLevel)}");
            }

            listingStandard.Gap();
            listingStandard.GapLine();

            // Info message
            Text.Font = GameFont.Tiny;
            listingStandard.Label("Changes take effect immediately and are saved automatically.");

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// Gets a user-friendly label for a log level.
        /// </summary>
        private string GetLogLevelLabel(Logger.LogLevel level)
        {
            switch (level)
            {
                case Logger.LogLevel.Debug:
                    return "Debug";
                case Logger.LogLevel.Trace:
                    return "Trace";
                case Logger.LogLevel.Info:
                    return "Info";
                case Logger.LogLevel.Warning:
                    return "Warning";
                case Logger.LogLevel.Error:
                    return "Error";
                case Logger.LogLevel.Critical:
                    return "Critical";
                case Logger.LogLevel.Panic:
                    return "Panic";
                default:
                    return level.ToString();
            }
        }

        /// <summary>
        /// Gets a description for each log level.
        /// </summary>
        private string GetLogLevelDescription(Logger.LogLevel level)
        {
            switch (level)
            {
                case Logger.LogLevel.Debug:
                    return "Most verbose, shows all messages";
                case Logger.LogLevel.Trace:
                    return "Detailed execution tracing";
                case Logger.LogLevel.Info:
                    return "General informational messages";
                case Logger.LogLevel.Warning:
                    return "Warning messages and above";
                case Logger.LogLevel.Error:
                    return "Error messages and above";
                case Logger.LogLevel.Critical:
                    return "Critical errors only";
                case Logger.LogLevel.Panic:
                    return "Panic-level errors only (least verbose)";
                default:
                    return "";
            }
        }
    }
}
