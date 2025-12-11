using RimWorld;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using Verse;
using YAMP.Activities;

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
        private readonly int _progressUpdateInterval = 30;
        private readonly int _tendActivityWork = 120;

        public CompProp_PodTend Props => (CompProp_PodTend)props;
        
        private CompPowerTrader _powerComp;
        public bool HasPower => _powerComp?.PowerOn ?? true;
        
        YAMP.Activities.IActivity _currentActivity = null;

        public float Progress => _currentActivity?.ProgressPercentage ?? 0f;

        public override void PostExposeData()
        {
            base.PostExposeData();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            var medPod = (Building_MedPod)parent;
            _powerComp = medPod.PowerComp;
            Logger.Debug("Comp_PodTend spawned!");
        }

        private bool CheckPowerRequirements(string operation)
        {
            if (!HasPower)
            {
                Messages.Message($"Cannot {operation}: No power", parent, MessageTypeDefOf.RejectInput);
                return false;
            }
            return true;
        }

        public override void CompTick()
        {
            base.CompTick();

            // Check power state every 60 ticks when active
            if (_currentActivity != null && parent.IsHashIntervalTick(60) && !HasPower)
            {
                Logger.Debug("Power lost during tend, stopping current activity");
                _currentActivity.Stop();
                _currentActivity = null;
                Messages.Message("Tend halted: Power lost", parent, MessageTypeDefOf.NegativeEvent);
                return;
            }

            // Handle activity completion
            if (_currentActivity != null && _currentActivity.IsFinished)
            {
                Logger.Log("[Tend]", "Tend activity finished");
                _currentActivity = null;
                CheckTend();
                return;
            }

            // Update activity progress on interval
            if (_currentActivity != null && parent.IsHashIntervalTick(_progressUpdateInterval))
            {
                _currentActivity.Update(Verse.GenTicks.TicksGame);
                return;
            }

            CheckTend();
        }

        public void CheckTend()
        {
            Pawn patient = ((Building_MedPod)parent).GetCurOccupant(0);
            if (patient == null)
            {
                // No patient; if there was an activity, stop it
                if (_currentActivity != null && !_currentActivity.IsFinished)
                {
                    _currentActivity.Stop();
                    _currentActivity = null;
                }
                return;
            }

            if (_currentActivity == null && parent is Building_MedPod medPod && TendHandler.CanTend(medPod))
            {
                // Check power before starting tend
                if (!CheckPowerRequirements("start tend"))
                {
                    return;
                }
                
                // Attempt to start a new tend activity if possible
                var args = new object[] { medPod };

                _currentActivity = new Activity(
                    name: "Tend",
                    onComplete: TendHandler.OnCompleteHandler,
                    onStop: TendHandler.OnStopHandler,
                    args: args,
                    speedMultiplier: 1f
                );

                _currentActivity.Start(_tendActivityWork); // 2 seconds (120 ticks)
                Logger.Debug($"Started tend activity");
            }
        }

        public override string CompInspectStringExtra()
        {
            if (_currentActivity != null)
            {
                if (_currentActivity.InProgress)
                {
                    return $"Tending: {_currentActivity.ProgressPercentage:P0}";
                }
                else
                {
                    return $"Pending Tend";
                }
            }

            return "No pending tend";
        }
    }
}
