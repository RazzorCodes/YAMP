using RimWorld;
using System;
using System.Linq;
using Verse;

namespace YAMP
{
    public class CompProp_PodTend : CompProperties
    {
        public float successChance = 0.95f;
        public float qualityMin = 0.33f;
        public float qualityMax = 0.90f;
        public float qualityMid = 0.75f;
        public float tendCost = 15f;

        public CompProp_PodTend()
        {
            compClass = typeof(Comp_PodTend);
        }
    }

    public class Comp_PodTend : ThingComp
    {
        public CompProp_PodTend Props => (CompProp_PodTend)props;

        private OperationalStock _operationalStock;
        private OperationalStock OperationalStock => _operationalStock ??= parent.GetComp<OperationalStock>();

        private PodContainer _podConatiner;
        private PodContainer PodConatiner => _podConatiner ??= ((Building_MedPod)parent).Container;

        public override void CompTick()
        {
            if (PodConatiner.GetPawn() == null)
            {
                Logger.Log("[Operate]", "No patient found in pod");
                return;
            }

            if (parent.IsHashIntervalTick(100))
            {
                TryPerformTend();
            }
        }

        private void TryPerformTend()
        {
            Pawn patient = PodConatiner.Get().OfType<Pawn>().FirstOrDefault();
            if (patient == null)
            {
                Logger.Log("[Tend]", "No patient found in pod");
                return;
            }

            Logger.Log("[Tend]", "Tending patient " + patient.LabelShort);
            if (!MedPodCanTend(patient))
            {
                Logger.Log("[Tend]", "Patient is not tendable");
                return;
            }

            if (!PerformTend(patient))
            {
                Logger.Log("Tend", "Failed to perform tend on " + patient.LabelShort);
            }
        }

        private bool MedPodCanTend(Pawn patient)
        {
            return
                patient != null &&
                patient.RaceProps.IsFlesh &&
                patient.health.hediffSet.hediffs.Any(h => h.TendableNow(false)) &&
                OperationalStock != null && OperationalStock.Stock >= Props.tendCost;
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
                if (OperationalStock != null && !OperationalStock.TryConsumeStock(CalculateStockCost()))
                {
                    return false;
                }

                if (Rand.Value > Props.successChance)
                {
                    return false;
                }

                float quality = Rand.Range(0, 100) >= 50
                    ? Rand.Range(Props.qualityMid, Props.qualityMax)
                    : Rand.Range(Props.qualityMin, Props.qualityMid);

                hediff.Tended(quality, 1f, 0);

                return true;
            }
        }

        private float CalculateStockCost()
        {
            return Props.tendCost;
        }
    }
}