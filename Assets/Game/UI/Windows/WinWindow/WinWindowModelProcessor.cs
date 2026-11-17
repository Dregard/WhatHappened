using Game.Gameplay;
using Game.UI.Windows.General;

namespace Game.UI.Windows.WinWindow
{
    public class WinWindowModelProcessor : WindowModelProcessorBase<WinWindowModel, EmptyModelParams>
    {
        private readonly WindowSystem _windowSystem;
        private readonly GameplaySystem _gameplaySystem;
        private readonly LevelsManager _levelsManager;
        private readonly GameplayProgressController _progressController;

        /// <summary>Connects level victory to progression; individual levels no longer own rewards.</summary>
        public WinWindowModelProcessor(WindowSystem windowSystem, GameplaySystem gameplaySystem,
            LevelsManager levelsManager, GameplayProgressController progressController)
        {
            _windowSystem = windowSystem;
            _gameplaySystem = gameplaySystem;
            _levelsManager = levelsManager;
            _progressController = progressController;
        }

        /// <summary>Supplies search results, tolerating restored victory without an attempt snapshot.</summary>
        public override WinWindowModel GetModel()
        {
            return new WinWindowModel
            {
                ContinueAction = Continue,
                CurrentLevel = _progressController.CurrentLevel - (_progressController.IsLevelVictoryPending ? 1 : 0),
                Statistics = _levelsManager.CompletedAttempt,
            };
        }

        /// <summary>Continues chapter progression or the flat anomaly catalog after level victory.</summary>
        private async void Continue()
        {
            await _windowSystem.Close<WinWindow>();
            await _gameplaySystem.Restart();
        }
    }
}
