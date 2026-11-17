using System;

namespace Game.Levels
{
    public sealed class LevelAttemptTimeModel
    {
        public float ElapsedSeconds { get; private set; }
        public bool IsTimed { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool ReceivedExtraTime { get; private set; }
        public bool QualifiesForLike
        {
            get { return !IsTimed || !ReceivedExtraTime; }
        }

        /// <summary>Starts a new attempt, restoring time-like eligibility independently of previous revives.</summary>
        public void Reset(bool isTimed)
        {
            ElapsedSeconds = 0;
            IsTimed = isTimed;
            IsCompleted = false;
            ReceivedExtraTime = false;
        }

        /// <summary>Consumes only active timer ticks; caps the timeout frame at the remaining countdown.</summary>
        public void Tick(float deltaSeconds, bool isActive, float remainingSeconds)
        {
            if (!isActive || IsCompleted)
                return;
            ElapsedSeconds += IsTimed ? Math.Min(deltaSeconds, Math.Max(0f, remainingSeconds)) : deltaSeconds;
        }

        /// <summary>Marks extra time from the time-loss continuation, preserving accumulated active time.</summary>
        public void MarkExtraTimeReceived()
        {
            if (!IsCompleted)
                ReceivedExtraTime = true;
        }

        /// <summary>Prevents victory presentation, snap bonuses or later waits from advancing this attempt.</summary>
        public void Complete()
        {
            IsCompleted = true;
        }
    }
}
