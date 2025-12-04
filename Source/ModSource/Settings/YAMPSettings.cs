using Verse;

namespace YAMP.Settings
{
    /// <summary>
    /// Stores persistent settings for the YAMP mod.
    /// </summary>
    public class YAMPSettings : ModSettings
    {
        /// <summary>
        /// The current log level setting. Stored as an int for serialization.
        /// </summary>
        public Logger.LogLevel logLevel = Logger.LogLevel.Info;

        /// <summary>
        /// Save and load settings data.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref logLevel, "logLevel", Logger.LogLevel.Info);

            // Apply the loaded setting to the Logger
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                Logger.Level = logLevel;
            }
        }
    }
}
