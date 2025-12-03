

using System.Collections.Concurrent;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace YAMP.Activities
{
    public abstract class BaseActivity : IActivity
    {
        private int _lastTick = 0;
        protected float _activityMultiplier = 1f;

        protected bool force_stopped = false;

        public float Progress { get; protected set; }
        public float TotalTicks { get; protected set; }
        public float ProgressPercentage => Progress / TotalTicks;
        public bool InProgress => Progress < TotalTicks;
        public virtual string Name { get; protected set; }

        public bool IsFinished => force_stopped || ProgressPercentage >= 1f;

        public virtual void Update()
        {
            int currentTick = Verse.GenTicks.TicksGame;
            float tickDelta = currentTick - _lastTick;
            _lastTick = currentTick;

            Progress += tickDelta / TotalTicks;

            Logger.Debug($"Activity \"{Name}\" progress: {ProgressPercentage * 100}% of {TotalTicks} ticks");

            if (ProgressPercentage >= 1f)
            {
                End();
            }
        }

        public virtual void Execute(List<Thing> parts, Bill_Medical bill)
        {
        }

        public virtual void Start()
        {
            Logger.Error("Activity start called without total work");
        }
        public virtual void Start(float totalWork)
        {
            _lastTick = Verse.GenTicks.TicksGame;
            TotalTicks = totalWork;
            Progress = 0;
            Logger.Debug($"Activity started: {totalWork} ticks");
        }

        public virtual void End()
        {
            Logger.Debug($"Activity ending");
            if (TotalTicks > Progress)
            {
                Logger.Debug($"Activity ended unexpectedly before completion: {ProgressPercentage * 100}% of {TotalTicks} ticks");
            }
            else if (ProgressPercentage == 1f)
            {
                Logger.Debug($"Activity ended as expected");
            }
        }

        public virtual void Stop()
        {
            force_stopped = true;
            if (ProgressPercentage > 0.95f)
            {
                Logger.Debug($"Activity stopped near completion; rubberbanding to finish");
                Progress = TotalTicks;
            }
            End();
        }
    }
}