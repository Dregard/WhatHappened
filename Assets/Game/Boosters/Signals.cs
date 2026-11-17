namespace Game.Boosters
{
    public static class Signals
    {
        public struct BoosterUsed
        {
            public BoosterKind BoosterKind;
            public int LevelNumber;
            public int LevelAttempt;
            public float LevelProgress;
        }
    }
}