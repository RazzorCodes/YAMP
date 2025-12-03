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

        private int ticksToComplete = 0;
        private int currentTick = 0;
        private bool isTending = false;

        ActivityTend _currentActivity = null;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksToComplete, "ticksToComplete", 0);
            Scribe_Values.Look(ref currentTick, "currentTick", 0);
            Scribe_Values.Look(ref isTending, "isTending", false);
        }

        public override void CompTick()
        {
            base.CompTick();
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

            if (_currentActivity == null && parent is Building_MedPod medPod && ActivityTend.CanTend(medPod))
            {
                // Attempt to start a new tend activity if possible
                _currentActivity = new ActivityTend(medPod);
                Logger.Debug($"Started tend activity: {_currentActivity.Name}");
                _currentActivity.Start();
            }

            if (_currentActivity != null && _currentActivity.IsFinished)
            {
                Logger.Log("[Tend]", "Tend activity finished, executing tend");

                var activityToExecute = _currentActivity;
                activityToExecute.Execute();

                _currentActivity = null;
                return;
            }

            if (_currentActivity != null && parent.IsHashIntervalTick(100))
            {
                _currentActivity.Update();
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