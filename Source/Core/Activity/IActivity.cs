using System;

namespace YAMP.Activities
{
    /// <summary>
    /// Core activity interface with no external dependencies
    /// </summary>
    public interface IActivity
    {
        string Name { get; }
        bool InProgress { get; }
        float Progress { get; }
        float WorkAmount { get; }
        float ProgressPercentage { get; }
        bool IsFinished { get; }

        void Start(float workAmount);
        void Update(int currentTicks);
        void Stop();
    }
}