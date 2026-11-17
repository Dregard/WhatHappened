using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Game.Gameplay;
using Game.Levels;
using Zenject;

namespace Game.Purchase
{
    public class PurchaseSystem
    {
        private readonly Dictionary<System.Type, IPurchaseProcessor> _processors;
        private readonly RewardSystem _rewardSystem;
        private readonly SignalBus _signalBus;
        private readonly GameplayProgressController _gameplayProgressController;
        private readonly LevelProgressController _levelProgressController;
        

        public PurchaseSystem(SignalBus signalBus, List<IPurchaseProcessor> processors, RewardSystem rewardSystem, LevelProgressController levelProgressController, GameplayProgressController gameplayProgressController)
        {
            _signalBus = signalBus;
            _processors = processors.ToDictionary(p => p.PriceType);
            _rewardSystem = rewardSystem;
            _levelProgressController = levelProgressController;
            _gameplayProgressController = gameplayProgressController;
        }

        public async UniTask<bool> Buy(PriceData priceData, RewardData rewardData, string transactionName, PurchasePlacementAlias placementAlias)
        {
            if (priceData == null) 
                return false;

            if (_processors.TryGetValue(priceData.GetType(), out var processor))
            {
                var success = await processor.ProcessPurchase(priceData);
                if (success && rewardData != null)
                {
                    await _rewardSystem.GiveReward(rewardData);
                    _signalBus.Fire(new Singals.PurchaseSucceed
                    {
                        PlacementAlias = placementAlias,
                        PriceData = priceData,
                        RewardData = rewardData,
                        PurchaseId = transactionName,
                        LevelNumber = _gameplayProgressController.CurrentLevel,
                        LevelAttempt = _gameplayProgressController.CurrentAttempt,
                        LevelProgress = _levelProgressController.GetLevelProgress(),
                    });
                    return true;
                }
            }
            else
            {
                UnityEngine.Debug.LogError($"No purchase processor found for type {priceData.GetType()}");
            }

            return false;
        }

        public bool CanBuy(PriceData priceData)
        {
            if (priceData == null) 
                return false;

            if (_processors.TryGetValue(priceData.GetType(), out var processor))
            {
                return processor.CanBuy(priceData);
            }

            return false;
        }
    }
}
