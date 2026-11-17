namespace Game.Currency
{
    public interface ICurrencyController
    {
        public CurrencyKind CurrencyKind { get; }
        public void AddCurrency(int amount, CurrencyGainPlacementAlias gainPlacementAlias);
        public void SubtractCurrency(int amount);
        public void SetCurrency(int amount);
        public int GetCurrencyValue();
        public bool IsEnoughCurrency(int amount);
    }
}