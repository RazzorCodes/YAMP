using RimWorld;
using System;
using System.Linq;
using Verse;

namespace YAMP
{
    public class CompProperties_MedPodTend : CompProperties
    {
        public float tendSuccessChance = 0.95f;
        public float tendQualityMin = 0.33f;
        public float tendQualityMax = 0.90f;
        public float tendQualityMid = 0.75f;
        public float stockConsumptionFactor = 1.0f;

        public CompProperties_MedPodTend()
        {
            compClass = typeof(CompMedPodTend);
        }
    }

    public class CompMedPodTend : ThingComp
    {
        public CompProperties_MedPodTend Props => (CompProperties_MedPodTend)props;
        // public ThingOwner innerContainer; // Removed: Uses CompMedPodOperations container

        private CompMedPodFuel fuelComp;

        public CompMedPodFuel FuelComp
        {
            get
            {
                if (fuelComp == null) fuelComp = parent.GetComp<CompMedPodFuel>();
                return fuelComp;
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(250))
            {
                TryPerformTend();
            }
        }

        private void TryPerformTend()
        {
            // Get patient from Operations component
            CompMedPodSurgery ops = parent.GetComp<CompMedPodSurgery>();
            if (ops == null || ops.innerContainer == null) return;

            Pawn patient = ops.innerContainer.OfType<Pawn>().FirstOrDefault();
            if (patient == null) return;

            // Log.Message("YAMP: Tending patient " + patient.LabelShort); // Reduced spam
            if (!MedPodCanTend(patient))
            {
                return;
            }

            if (!PerformTend(patient))
            {
                // Log.Warning("YAMP: Failed to perform tend on " + patient.LabelShort);
            }
        }

        private bool MedPodCanTend(Pawn patient)
        {
            return
                patient != null &&
                patient.RaceProps.IsFlesh &&
                // HealthAIUtility.ShouldSeekMedicalRest(patient) && // Removed: Pod should tend even if not strictly "resting" logic
                patient.health.hediffSet.hediffs.Any(h => h.TendableNow(false));
        }

        private bool PerformTend(Pawn patient)
        {
            Hediff hediff = patient.health.hediffSet.hediffs
                .Where<Hediff>((Func<Hediff, bool>)(h => h.TendableNow(false)))
                .ToList<Hediff>()
                .OrderByDescending(h => (h as Hediff_Injury)?.BleedRate ?? 0f) // Prioritize greatest bleeding
                .ThenByDescending(h => h.Severity) // Then by greatest severity
                .FirstOrDefault();

            if (hediff == null)
            {
                return false;
            }
            else
            {
                CompMedPodFuel fuel = parent.GetComp<CompMedPodFuel>();
                if (fuel != null)
                {
                    fuel.stock -= CalculateStockCost();
                }

                if (Rand.Value > Props.tendSuccessChance)
                {
                    return false;
                }

                float tendQuality = Rand.Range(0, 100) >= 50
                    ? Rand.Range(Props.tendQualityMid, Props.tendQualityMax)
                    : Rand.Range(Props.tendQualityMin, Props.tendQualityMid);

                hediff.Tended(tendQuality, 1f, 0);

                return true;
            }
        }

        private float CalculateStockCost()
        {
            return Props.stockConsumptionFactor;
        }
    }
}