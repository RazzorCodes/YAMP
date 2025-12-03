using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace YAMP.Activities
{
    class ActivityTend : BaseActivity
    {
        private const string ActivityType = "Tend";
        private string _currentTendTarget = "";
        public override string Name => ActivityType + (_currentTendTarget != "" ? ("/" + _currentTendTarget) : string.Empty);

        private readonly Building_MedPod _facility;
        private readonly Pawn _patient;

        // Mirrored stock handling with Comp_PodTend defaults
        private static readonly float _stockCost = 15f;

        // Tend quality parameters (mirrors Comp_PodTend / old implementation)
        private readonly float _successChance = 0.95f;
        private readonly float _qualityMin = 0.33f;
        private readonly float _qualityMax = 1f;
        private readonly float _qualityMid = 0.75f;

        public ActivityTend(Building_MedPod facility)
        {
            _facility = facility;
            _patient = facility?.Container?.GetPawn();

            if (_facility == null)
            {
                Logger.Debug($"Activity {Name} missing facility");
                End();
                return;
            }

            if (_patient == null)
            {
                Logger.Debug($"Activity {Name} missing patient");
                End();
                return;
            }
        }

        /// <summary>
        /// Execute the actual tend once the activity has finished.
        /// Mirrors the stock + success + quality logic from the previous Comp_PodTend implementation.
        /// </summary>
        public void Execute()
        {
            if (_patient == null || _patient.Dead)
            {
                Logger.Debug($"Activity {Name} patient is null or dead");
                End();
                return;
            }

            if (!CanTend(_facility))
            {
                Logger.Debug($"Activity {Name} patient cannot be tended at execute time");
                End();
                return;
            }

            if (!PerformTend())
            {
                Logger.Debug($"Activity {Name} failed to perform tend");
            }

            End();
        }

        public override void Start()
        {
            // Tend takes 2 seconds (120 ticks) – mirror old Comp_PodTend behaviour
            base.Start(120);

            if (_patient == null)
            {
                Logger.Debug($"Activity {Name} missing patient");
                End();
                return;
            }

            if (!CanTend(_facility))
            {
                Logger.Debug($"Activity {Name} patient cannot be tended at start");
                End();
                return;
            }
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
                facility.Stock.TotalStock >= _stockCost;
        }

        private bool PerformTend()
        {
            Hediff hediff = _patient.health.hediffSet.hediffs
                .Where(h => h.TendableNow(false))
                .OrderByDescending(h => (h as Hediff_Injury)?.BleedRate ?? 0f) // Prioritize greatest bleeding
                .ThenByDescending(h => h.Severity) // Then by greatest severity
                .FirstOrDefault();

            if (hediff == null)
            {
                Logger.Debug($"Activity {Name} no tendable hediff found");
                return false;
            }

            _currentTendTarget = hediff.LabelCap;

            // Consume stock
            if (!_facility.Stock.TryConsumeStock(_stockCost))
            {
                Logger.Debug($"Activity {Name} failed to consume stock during tend");
                return false;
            }

            // Check success chance
            if (Rand.Value > _successChance)
            {
                Logger.Debug($"Activity {Name} failed success check");
                return false;
            }

            // Calculate quality
            float quality = Rand.Range(0, 100) >= 50
                ? Rand.Range(_qualityMid, _qualityMax)
                : Rand.Range(_qualityMin, _qualityMid);

            // Perform the tend
            hediff.Tended(quality, 1f, 0);

            Logger.Debug($"Activity {Name} successfully tended {hediff.Label} with quality {quality:P0}");
            return true;
        }

        public override void End()
        {
            base.End();
        }
    }
}