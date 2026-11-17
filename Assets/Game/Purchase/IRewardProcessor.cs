using System;
using Cysharp.Threading.Tasks;

namespace Game.Purchase
{
    public interface IRewardProcessor
    {
        Type RewardType { get; }
        UniTask ProcessReward(RewardData rewardData);
    }

    public abstract class RewardProcessor<T> : IRewardProcessor where T : RewardData
    {
        public Type RewardType => typeof(T);

        public UniTask ProcessReward(RewardData rewardData)
        {
            return ProcessReward((T)rewardData);
        }

        protected abstract UniTask ProcessReward(T rewardData);
    }
}
