using Cysharp.Threading.Tasks;
using Game.Currency;

namespace Game.Purchase
{
    public class CurrencyRewardProcessor : RewardProcessor<CurrencyRewardData>
    {
        private readonly CurrencySystem _currencySystem;

        public CurrencyRewardProcessor(CurrencySystem currencySystem)
        {
            _currencySystem = currencySystem;
        }

        protected override UniTask ProcessReward(CurrencyRewardData rewardData)
        {
            _currencySystem.AddCurrency(rewardData.CurrencyKind, rewardData.Amount, CurrencyGainPlacementAlias.Purchase);
            return UniTask.CompletedTask;
        }
    }
}
