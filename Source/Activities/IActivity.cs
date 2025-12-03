namespace YAMP.Activities
{
    interface IActivity
    {
        string Name { get; }
        bool InProgress { get; }
        float Progress { get; }
        float TotalTicks { get; }
        float ProgressPercentage { get; }

        bool IsFinished { get; }

        void Start();
        void End();
        void Stop();
    }
}