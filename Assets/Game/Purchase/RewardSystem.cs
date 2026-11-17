using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace Game.Purchase
{
    public class RewardSystem
    {
        private readonly Dictionary<System.Type, IRewardProcessor> _processors;

        public RewardSystem(List<IRewardProcessor> processors)
        {
            _processors = processors.ToDictionary(p => p.RewardType);
        }

        public async UniTask GiveRewards(List<RewardData> rewards)
        {
            foreach (var reward in rewards)
            {
                await GiveReward(reward);
            }
        }
        
        public async UniTask GiveReward(RewardData rewardData)
        {
            if (rewardData == null) return;

            if (_processors.TryGetValue(rewardData.GetType(), out var processor))
            {
                await processor.ProcessReward(rewardData);
            }
            else
            {
                UnityEngine.Debug.LogError($"No reward processor found for type {rewardData.GetType()}");
            }
        }
    }
}
