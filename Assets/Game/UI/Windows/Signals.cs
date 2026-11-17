using Game.Boosters;

namespace Game.UI.Windows
{
    public static class Signals
    {
        public struct BoosterWindowOpenedSignal
        {
            public BoosterKind BoosterKind;
        }

        public struct ExtraTimeBoughtSignal
        {
            public int LevelNumber;
            public int LevelAttempt;
            public float LevelProgress;
        }
        
        public struct ExtraLifeBoughtSignal
        {
            public int LevelNumber;
            public int LevelAttempt;
            public float LevelProgress;
        }
    }
}