using System;

namespace YAMP.Activities
{

    /// <summary>
    /// Base activity implementation with time-based progress tracking
    /// </summary>
    public class Activity : IActivity
    {
        private int _startTick;
        private int _lastUpdateTick;
        private bool _isStarted;
        private bool _isStopped;
        private readonly float _speedMultiplier;

        protected readonly Action<object[]> _onComplete;
        protected readonly Action<object[]> _onStop;
        protected readonly object[] _args;

        public string Name { get; }
        public float WorkAmount { get; private set; }
        public float Progress { get; private set; }
        public float ProgressPercentage => WorkAmount > 0 ? Progress / WorkAmount : 0f;
        public bool InProgress => _isStarted && !_isStopped && Progress < WorkAmount;
        public bool IsFinished => _isStopped || ProgressPercentage >= 1f;

        public Activity(
            string name,
            Action<object[]> onComplete,
            Action<object[]> onStop = null,
            object[] args = null,
            float speedMultiplier = 1f)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _onComplete = onComplete ?? throw new ArgumentNullException(nameof(onComplete));
            _onStop = onStop;
            _args = args ?? Array.Empty<object>();
            _speedMultiplier = speedMultiplier > 0 ? speedMultiplier : 1f;
        }

        public virtual void Start(float workAmount)
        {
            if (_isStarted)
                throw new InvalidOperationException($"Activity '{Name}' is already started");

            if (workAmount <= 0)
                throw new ArgumentException("Work amount must be greater than zero", nameof(workAmount));

            WorkAmount = workAmount;
            Progress = 0f;
            _isStarted = true;
            _isStopped = false;
            _startTick = 0; // Will be set on first Update
            _lastUpdateTick = 0;
        }

        public virtual void Update(int currentTicks)
        {
            if (!InProgress)
                return;

            // Initialize tick tracking on first update
            if (_startTick == 0)
            {
                _startTick = currentTicks;
                _lastUpdateTick = currentTicks;
                return;
            }

            // Calculate progress delta
            int tickDelta = currentTicks - _lastUpdateTick;
            if (tickDelta < 0)
                throw new ArgumentException("Current ticks cannot be less than previous ticks", nameof(currentTicks));

            _lastUpdateTick = currentTicks;

            // Apply progress with speed multiplier
            Progress += tickDelta * _speedMultiplier;

            // Check for completion
            if (Progress >= WorkAmount)
            {
                Progress = WorkAmount; // Clamp to exact work amount
                Complete();
            }
        }

        public virtual void Stop()
        {
            if (!_isStarted || _isStopped)
                return;

            _isStopped = true;
            _onStop?.Invoke(_args);
        }

        protected virtual void Complete()
        {
            if (_isStopped)
                return;

            _isStopped = true;
            _onComplete?.Invoke(_args);
        }
    }

    /// <summary>
    /// Activity manager to handle multiple concurrent activities
    /// </summary>
    public class ActivityManager
    {
        private readonly System.Collections.Generic.List<IActivity> _activities = new();

        public void RegisterActivity(IActivity activity)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));

            _activities.Add(activity);
        }

        public void UnregisterActivity(IActivity activity)
        {
            _activities.Remove(activity);
        }

        public void UpdateAll(int currentTicks)
        {
            // Update in reverse to allow safe removal during iteration
            for (int i = _activities.Count - 1; i >= 0; i--)
            {
                var activity = _activities[i];
                activity.Update(currentTicks);

                // Auto-remove finished activities
                if (activity.IsFinished)
                {
                    _activities.RemoveAt(i);
                }
            }
        }

        public void StopAll()
        {
            foreach (var activity in _activities)
            {
                activity.Stop();
            }
            _activities.Clear();
        }

        public System.Collections.Generic.IReadOnlyList<IActivity> GetActiveActivities()
        {
            return _activities.AsReadOnly();
        }
    }
}