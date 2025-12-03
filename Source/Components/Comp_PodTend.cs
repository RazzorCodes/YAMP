using RimWorld;
using System;
using System.Linq;
using UnityEngine;
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
        private OperationalStock OperationalStock => _operationalStock ??= ((Building_MedPod)parent).OperationalStock;

        private PodContainer _podConatiner;
        private PodContainer PodConatiner => _podConatiner ??= ((Building_MedPod)parent).Container;

        private int ticksToComplete = 0;
        private int currentTick = 0;
        private bool isTending = false;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksToComplete, "ticksToComplete", 0);
            Scribe_Values.Look(ref currentTick, "currentTick", 0);
            Scribe_Values.Look(ref isTending, "isTending", false);
        }

        public override void CompTick()
        {
            if (PodConatiner.GetPawn() == null)
            {
                if (isTending)
                {
                    CancelTend();
                }
                return;
            }

            if (isTending)
            {
                currentTick++;
                if (currentTick >= ticksToComplete)
                {
                    CompleteTend();
                }
            }
            else if (parent.IsHashIntervalTick(100))
            {
                TryStartTend();
            }
        }

        private void TryStartTend()
        {
            Pawn patient = PodConatiner.Get().OfType<Pawn>().FirstOrDefault();
            if (patient == null)
            {
                return;
            }

            if (!MedPodCanTend(patient))
            {
                return;
            }

            // Start Tending
            isTending = true;
            currentTick = 0;
            ticksToComplete = 120; // 2 seconds for tend
        }

        private void CancelTend()
        {
            isTending = false;
            currentTick = 0;
            Logger.Log("[Tend]", "Tend cancelled");
        }

        private void CompleteTend()
        {
            isTending = false;
            currentTick = 0;

            Pawn patient = PodConatiner.Get().OfType<Pawn>().FirstOrDefault();
            if (patient == null) return;

            Logger.Log("[Tend]", "Tending patient " + patient.LabelShort);
            if (!MedPodCanTend(patient))
            {
                Logger.Log("[Tend]", "Patient is no longer tendable");
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
                OperationalStock != null && OperationalStock.TotalStock >= Props.tendCost;
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

        public override string CompInspectStringExtra()
        {
            if (isTending)
            {
                return $"Tending: {(float)currentTick / ticksToComplete:P0}";
            }
            return null;
        }

        public override void PostDraw()
        {
            base.PostDraw();

            if (isTending)
            {
                GenDraw.DrawFillableBar(new GenDraw.FillableBarRequest
                {
                    center = parent.DrawPos + Vector3.up * 0.1f + Vector3.forward * 0.25f,
                    size = new Vector2(0.8f, 0.14f),
                    fillPercent = (float)currentTick / ticksToComplete,
                    filledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.2f, 0.8f, 0.2f)), // Green for tend
                    unfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f)),
                    margin = 0.15f,
                    rotation = Rot4.North
                });
            }
        }
    }
}