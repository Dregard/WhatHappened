using Cysharp.Threading.Tasks;
using Game.Ads;
using Game.Boosters;
using Game.Gameplay;
using Game.Purchase;
using Game.UI.Windows.General;
using UnityEngine;

namespace Game.UI.Windows.BoosterWindow
{
    public class BoosterWindowModelProcessor : WindowModelProcessorBase<BoosterWindowModel, BoosterWindowModelParams>
    {
        private WindowSystem _windowSystem;
        private GameplaySystem _gameplaySystem;
        private PurchaseSystem _purchaseSystem;
        private BoosterSystem _boosterSystem;
        private AdsSystem _adsSystem;
        
		private BoosterKind _boosterKind;

        
        public BoosterWindowModelProcessor(WindowSystem windowSystem, GameplaySystem gameplaySystem, PurchaseSystem purchaseSystem,
            BoosterSystem boosterSystem, AdsSystem adsSystem)
        {
            _windowSystem = windowSystem;
            _gameplaySystem = gameplaySystem;
            _purchaseSystem = purchaseSystem;
            _boosterSystem = boosterSystem;
            _adsSystem = adsSystem;
        }

        public override void SetParams(BoosterWindowModelParams modelParams)
        {
            _boosterKind = modelParams.BoosterKind;
        }

        public override BoosterWindowModel GetModel()
        {
            var boosterModel = _boosterSystem.GetViewModel(_boosterKind);
            return new BoosterWindowModel
            {
                BoosterViewModel = boosterModel,
                BuyBoosterAction = BuyBooster,
                CanBuyBooster = _purchaseSystem.CanBuy(boosterModel.Price),
                CloseAction = Close,
                AdsAction = ShowAdsAndGetReward,
            };
        }
        
        private async void BuyBooster()
        {
            var boosterModel = _boosterSystem.GetViewModel(_boosterKind);
            if (_purchaseSystem.CanBuy(boosterModel.Price) == false)
            {
                Debug.Log($"Cannot buy booster {_boosterKind}");
                return;
            }
                
            await _purchaseSystem.Buy(boosterModel.Price, boosterModel.Reward, "Booster", PurchasePlacementAlias.BoosterWindow);
            
            await _windowSystem.Close<BoosterWindow>();
        }

        private async UniTask<bool> ShowAdsAndGetReward()
        {
            var success = await _adsSystem.TryShowRewarded("BoosterWindow");
            if (success)
            {
                _boosterSystem.Add(_boosterKind, 1);
                _windowSystem.Close<BoosterWindow>().Forget();
            }

            return success;
        }

        private async void Close()
        {
            await _windowSystem.Close<BoosterWindow>();
        }
    }
}