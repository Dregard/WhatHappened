using System;
using Cysharp.Threading.Tasks;

namespace Game.Purchase
{
    public interface IPurchaseProcessor
    {
        Type PriceType { get; }
        UniTask<bool> ProcessPurchase(PriceData priceData);
        bool CanBuy(PriceData priceData);
    }

    public abstract class PurchaseProcessor<T> : IPurchaseProcessor where T : PriceData
    {
        public Type PriceType => typeof(T);

        public UniTask<bool> ProcessPurchase(PriceData priceData)
        {
            return ProcessPurchase((T)priceData);
        }

        public bool CanBuy(PriceData priceData)
        {
            return CanBuy((T)priceData);
        }

        protected abstract UniTask<bool> ProcessPurchase(T priceData);
        protected abstract bool CanBuy(T priceData);
    }
}
