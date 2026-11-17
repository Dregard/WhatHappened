namespace Game.Purchase
{
    public static class Singals
    {
        public struct PurchaseSucceed
        {
            public string PurchaseId;
            public PurchasePlacementAlias PlacementAlias;
            public PriceData PriceData;
            public RewardData RewardData;
            public int LevelNumber;
            public int LevelAttempt;
            public float LevelProgress;
        }
    }
}