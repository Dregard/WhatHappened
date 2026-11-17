using System.Collections.Generic;
using Game.Levels;
using Game.Purchase;

namespace Game.Gameplay
{
    public static class Signals
    {
        public struct LevelStarted
        {
            public int LevelNumber;
            public int LevelAttempt;
        }

        public struct LevelFinished
        {
            public int LevelNumber;
            public int LevelAttempt;
            public IEnumerable<RewardData> Rewards;
        }

        public struct LevelFailed
        {
            public int LevelNumber;
            public int LevelAttempt;
            public float LevelProgress;
            public LoseReason Reason;
        }
        
        public struct LevelAbandoned
        {
            public int LevelNumber;
            public int LevelAttempt;
            public float LevelProgress;
        }

        public struct LevelFailedSoft
        {
            public int LevelNumber;
            public int LevelAttempt;
            public float LevelProgress;
            public LoseReason Reason;
        }
    }
}