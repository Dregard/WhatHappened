using Cysharp.Threading.Tasks;
using Game.Currency;

namespace Game.Purchase
{
    public class CurrencyPriceProcessor : PurchaseProcessor<CurrencyPriceData>
    {
        private readonly CurrencySystem _currencySystem;

        public CurrencyPriceProcessor(CurrencySystem currencySystem)
        {
            _currencySystem = currencySystem;
        }

        protected override UniTask<bool> ProcessPurchase(CurrencyPriceData priceData)
        {
            if (CanBuy(priceData))
            {
                _currencySystem.SubtractCurrency(priceData.CurrencyKind, priceData.Amount);
                return UniTask.FromResult(true);
            }

            return UniTask.FromResult(false);
        }

        protected override bool CanBuy(CurrencyPriceData priceData)
        {
            return _currencySystem.IsEnoughCurrency(priceData.CurrencyKind, priceData.Amount);
        }
    }
}
