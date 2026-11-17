namespace Game.Currency
{
    public static class Signals
    {
        public struct CurrencyAdded
        {
            public CurrencyKind CurrencyKind { get; set; }
            public int CurrentAmount { get; set; }
            public int Delta { get; set; }
            public CurrencyGainPlacementAlias GainPlacementAlias { get; set; }
        }

        public struct NotEnoughCurrency
        {
            public CurrencyKind CurrencyKind { get; set; }
        }

        public struct CurrencySubtracted
        {
            public CurrencyKind CurrencyKind { get; set; }
            public int CurrentAmount { get; set; }
            public int Delta { get; set; }
        }
    }
}