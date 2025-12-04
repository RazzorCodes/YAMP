using System.Linq;
using RimWorld;
using Verse;

namespace YAMP.Activities
{
    /// <summary>
    /// Static handler for tend completion callbacks
    /// </summary>
    public static class TendHandler
    {
        private const float StockCost = 15f;
        private const float SuccessChance = 0.95f;
        private const float QualityMin = 0.33f;
        private const float QualityMax = 1f;
        private const float QualityMid = 0.75f;

        /// <summary>
        /// Executed when a tend activity completes.
        /// Args: [Building_MedPod facility]
        /// </summary>
        public static void OnCompleteHandler(object[] args)
        {
            if (args == null || args.Length < 1)
            {
                Logger.Error("TendHandler: Invalid arguments for OnCompleteHandler");
                return;
            }

            var facility = args[0] as Building_MedPod;
            if (facility == null)
            {
                Logger.Error("TendHandler: Missing facility");
                return;
            }

            var patient = facility.Container.GetPawn();
            if (patient == null || patient.Dead)
            {
                Logger.Debug($"TendHandler: Patient is null or dead");
                return;
            }

            if (!CanTend(facility))
            {
                Logger.Debug($"TendHandler: Patient cannot be tended at execute time");
                return;
            }

            if (!PerformTend(facility, patient))
            {
                Logger.Debug($"TendHandler: Failed to perform tend");
            }
        }

        /// <summary>
        /// Executed when a tend activity is stopped early.
        /// Args: [Building_MedPod facility]
        /// </summary>
        public static void OnStopHandler(object[] args)
        {
            Logger.Debug("TendHandler: Activity stopped");
        }

        public static bool CanTend(Building_MedPod facility)
        {
            if (facility == null)
            {
                return false;
            }

            var patient = facility.Container?.GetPawn();

            return
                patient != null &&
                !patient.Dead &&
                patient.RaceProps.IsFlesh &&
                patient.health?.hediffSet?.hediffs != null &&
                patient.health.hediffSet.hediffs.Any(h => h.TendableNow(false)) &&
                facility.Stock != null &&
                facility.Stock.TotalStock >= StockCost;
        }

        private static bool PerformTend(Building_MedPod facility, Pawn patient)
        {
            Hediff hediff = patient.health.hediffSet.hediffs
                .Where(h => h.TendableNow(false))
                .OrderByDescending(h => (h as Hediff_Injury)?.BleedRate ?? 0f) // Prioritize greatest bleeding
                .ThenByDescending(h => h.Severity) // Then by greatest severity
                .FirstOrDefault();

            if (hediff == null)
            {
                Logger.Debug($"TendHandler: No tendable hediff found");
                return false;
            }

            // Consume stock
            if (!facility.Stock.TryConsumeStock(StockCost))
            {
                Logger.Debug($"TendHandler: Failed to consume stock during tend");
                return false;
            }

            // Check success chance
            if (Rand.Value > SuccessChance)
            {
                Logger.Debug($"TendHandler: Failed success check");
                return false;
            }

            // Calculate quality
            float quality = Rand.Range(0, 100) >= 50
                ? Rand.Range(QualityMid, QualityMax)
                : Rand.Range(QualityMin, QualityMid);

            // Perform the tend
            hediff.Tended(quality, 1f, 0);

            Logger.Debug($"TendHandler: Successfully tended {hediff.Label} with quality {quality:P0}");
            return true;
        }
    }
}
