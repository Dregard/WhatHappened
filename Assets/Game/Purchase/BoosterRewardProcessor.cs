using Cysharp.Threading.Tasks;
using Game.Boosters;

namespace Game.Purchase
{
    public class BoosterRewardProcessor : RewardProcessor<BoosterRewardData>
    {
        private readonly BoosterSystem _boosterSystem;

        public BoosterRewardProcessor(BoosterSystem boosterSystem)
        {
            _boosterSystem = boosterSystem;
        }

        protected override UniTask ProcessReward(BoosterRewardData rewardData)
        {
            _boosterSystem.Add(rewardData.BoosterKind, rewardData.Amount);
            return UniTask.CompletedTask;
        }
    }
}