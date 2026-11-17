using Cysharp.Threading.Tasks;
using Game.Analytics;
using Game.AssetManagement;
using Game.Gameplay;
using Game.Profile;
using Game.UI.Windows.General;
using Game.UI.Windows.MenuWindow;
using Game.UI.Windows.PreloadWindow;
using UnityEngine;
using Zenject;

namespace Game.Initialization
{
    public class GameStartupProcessor : IInitializable
    {
        private readonly WindowSystem _windowSystem;
        private readonly GameplaySystem _gameplaySystem;
        private readonly ProfileSystem _profileSystem;
        private readonly AddressablesAssetSystem _assetSystem;
        private readonly GameplayProgressController _gameplayProgressController;
        private readonly AnalyticsInitializer _analyticsInitializer;


        public GameStartupProcessor(WindowSystem windowSystem, GameplaySystem gameplaySystem,
            ProfileSystem profileSystem, AddressablesAssetSystem assetSystem,
            GameplayProgressController gameplayProgressController, AnalyticsInitializer analyticsInitializer)
        {
            _windowSystem = windowSystem;
            _gameplaySystem = gameplaySystem;
            _profileSystem = profileSystem;
            _assetSystem = assetSystem;
            _gameplayProgressController = gameplayProgressController;
            _analyticsInitializer = analyticsInitializer;
        }

        /// <summary>Prepares only the playable level, closes the splash as soon as it is ready and leaves reserve downloads running in the background.</summary>
        public async void Initialize()
        {
            var preloadModel = new PreloadWindowModel();
            await _windowSystem.Open<PreloadWindow>(preloadModel, refreshModel: true);

            await _assetSystem.LoadAsync("LevelsData", Progress.Create<float>(value => preloadModel.Report(value * 0.1f)));
            
            _analyticsInitializer.Initialize();
            
            _gameplayProgressController.CheckAbandonedMissionData();

            await _gameplaySystem.PrepareGame(preloadModel);
            _gameplaySystem.StartGame().Forget();

            return;


            if (_profileSystem.TryGetProfileHolder<GameplayProgressController>(out var profileHolder) == false)
            {
                Debug.LogError("Gameplay progress controller not found");
                _windowSystem.Open<MenuWindow>().Forget();
                return;
            }

            if (profileHolder.CurrentLevel - 1 < 2)
                _gameplaySystem.StartGame();
            else
                _windowSystem.Open<MenuWindow>().Forget();
        }
    }
}
