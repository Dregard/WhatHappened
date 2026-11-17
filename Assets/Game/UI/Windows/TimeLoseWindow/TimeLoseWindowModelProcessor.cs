using Cysharp.Threading.Tasks;
using Game.Ads;
using Game.Gameplay;
using Game.Levels;
using Game.Purchase;
using Game.UI.Windows.General;

namespace Game.UI.Windows.TimeLoseWindow
{
    public class TimeLoseWindowModelProcessor : WindowModelProcessorBase<TimeLoseWindowModel, EmptyModelParams>
    {
        private readonly WindowSystem _windowSystem;
        private readonly GameplaySystem _gameplaySystem;
        private readonly PurchaseSystem _purchaseSystem;
        private readonly LevelsManager _levelsManager;
        private readonly AdsSystem _adsSystem;

        
        public TimeLoseWindowModelProcessor(WindowSystem windowSystem, GameplaySystem gameplaySystem, 
            PurchaseSystem purchaseSystem, LevelsManager levelsManager, AdsSystem adsSystem)
        {
            _windowSystem = windowSystem;
            _gameplaySystem = gameplaySystem;
            _purchaseSystem = purchaseSystem;
            _levelsManager = levelsManager;
            _adsSystem = adsSystem;
        }

        public override TimeLoseWindowModel GetModel()
        {
            var priceData = (CurrencyPriceData)_levelsManager.LevelsData.RevivePrice;
            return new TimeLoseWindowModel
            {
                RestartAction = Restart,
                PayAndContinueAction = PayAndContinue,
                AdsAction = ShowAdsAndContinue,
                CanPayAndContinue = _purchaseSystem.CanBuy(_levelsManager.LevelsData.RevivePrice),
                CurrencyPriceValue = priceData.Amount,
            };
        }

        private void Restart()
        {
            _gameplaySystem.HandleFail(LoseReason.Time);
            
            _gameplaySystem.Restart();
            
            _windowSystem.Close<TimeLoseWindow>().Forget();
        }

        private void PayAndContinue()
        {
            PayAndContinueAsync().Forget();
        }

        private async UniTask<bool> ShowAdsAndContinue()
        {
            var success = await _adsSystem.TryShowRewarded("TimeOutWindow");
            if (success)
            {
                var extraTime = _levelsManager.LevelsData.ReviveTimer;
                _levelsManager.HandleExtraTimeBought(extraTime);
                _windowSystem.Close<TimeLoseWindow>().Forget();
            }

            return success;
        }

        private async UniTaskVoid PayAndContinueAsync()
        {
            var price = _levelsManager.LevelsData.RevivePrice;
            var extraTime = _levelsManager.LevelsData.ReviveTimer;
            
            await _purchaseSystem.Buy(price, null, "Extra time", PurchasePlacementAlias.TimeOutWindow);
            
            _levelsManager.HandleExtraTimeBought(extraTime);
            _windowSystem.Close<TimeLoseWindow>().Forget();
        }
    }
}
