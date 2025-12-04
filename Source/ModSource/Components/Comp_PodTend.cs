using RimWorld;
using System;
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
        public CompProp_PodTend Props => (CompProp_PodTend)props;
        YAMP.Activities.IActivity _currentActivity = null;

        public override void PostExposeData()
        {
            base.PostExposeData();
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Logger.Debug("Comp_PodTend spawned!");
        }

        public override void CompTick()
        {
            base.CompTick();

            // Handle activity completion
            if (_currentActivity != null && _currentActivity.IsFinished)
            {
                Logger.Log("[Tend]", "Tend activity finished");
                _currentActivity = null;
                CheckTend();
                return;
            }

            // Update activity progress on interval
            if (_currentActivity != null && parent.IsHashIntervalTick(100))
            {
                _currentActivity.Update(Verse.GenTicks.TicksGame);
                return;
            }

            CheckTend();
        }

        public void CheckTend()
        {
            Pawn patient = ((Building_MedPod)parent).Container.GetPawn();
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
                // Attempt to start a new tend activity if possible
                var args = new object[] { medPod };

                _currentActivity = new Activity(
                    name: "Tend",
                    onComplete: TendHandler.OnCompleteHandler,
                    onStop: TendHandler.OnStopHandler,
                    args: args,
                    speedMultiplier: 1f
                );

                _currentActivity.Start(120); // 2 seconds (120 ticks)
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

        public override void PostDraw()
        {
            base.PostDraw();
            Logger.Debug($"_currentActivity: {_currentActivity?.InProgress} {_currentActivity?.ProgressPercentage}");

            if (_currentActivity?.InProgress == true)
            {
                GenDraw.DrawFillableBar(new GenDraw.FillableBarRequest
                {
                    center = parent.DrawPos + Vector3.up * 0.1f + Vector3.forward * 0.25f,
                    size = new Vector2(0.8f, 0.14f),
                    fillPercent = _currentActivity.ProgressPercentage,
                    filledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.2f, 0.8f, 0.2f)), // Green for tend
                    unfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.3f, 0.3f, 0.3f)),
                    margin = 0.15f,
                    rotation = this.parent.Rotation
                });
            }
        }
    }
}
