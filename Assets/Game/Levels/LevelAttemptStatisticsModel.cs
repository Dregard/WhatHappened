using System;

namespace Game.Levels
{
    public sealed class LevelAttemptStatisticsModel
    {
        public float ElapsedSeconds { get; }
        public bool TimeLike { get; }
        public string FormattedTime { get; }

        /// <summary>Snapshots the completed search time without puzzle counts or combo eligibility.</summary>
        public LevelAttemptStatisticsModel(float elapsedSeconds, bool timeLike)
        {
            ElapsedSeconds = elapsedSeconds;
            TimeLike = timeLike;
            var seconds = Math.Max(0, (int)elapsedSeconds);
            FormattedTime = $"{seconds / 60}:{seconds % 60:00}";
        }
    }
}
