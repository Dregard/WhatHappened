using Cysharp.Threading.Tasks;
using Game.Boosters;
using Game.Currency;
using Game.Gameplay;
using Game.UI.Windows.General;

namespace Game.UI.Windows.CheatsWindow
{
    public class CheatsWindowModelProcessor : WindowModelProcessorBase<CheatsWindowModel, EmptyModelParams>
    {
        private readonly WindowSystem _windowSystem;
        private readonly GameplaySystem _gameplaySystem;
        private readonly GameplayProgressController _progressController;
        private readonly CurrencySystem _currencySystem;
        private readonly BoosterSystem _boosterSystem;

        public CheatsWindowModelProcessor(
            WindowSystem windowSystem, 
            GameplaySystem gameplaySystem, 
            GameplayProgressController progressController,
            CurrencySystem currencySystem,
            BoosterSystem boosterSystem)
        {
            _windowSystem = windowSystem;
            _gameplaySystem = gameplaySystem;
            _progressController = progressController;
            _currencySystem = currencySystem;
            _boosterSystem = boosterSystem;
        }

        public override CheatsWindowModel GetModel()
        {
            return new CheatsWindowModel
            {
                GoToLevelAction = GoToLevel,
                AddMoneyAction = AddMoney,
                AddBoostersAction = AddBoosters,
                CloseAction = Close
            };
        }

        private void Close()
        {
            _windowSystem.Close<CheatsWindow>().Forget();
        }

        private async void GoToLevel(int level)
        {
            _windowSystem.CloseAll().Forget();
            
            _progressController.SetLevelIndex(level - 2);
            
            _gameplaySystem.ForceWin();
        }

        private void AddMoney(int amount)
        {
            _currencySystem.AddCurrency(CurrencyKind.Gold, amount, CurrencyGainPlacementAlias.Cheats);
        }

        private void AddBoosters(BoosterKind kind, int count)
        {
            _boosterSystem.Add(kind, count);
        }
    }
}
