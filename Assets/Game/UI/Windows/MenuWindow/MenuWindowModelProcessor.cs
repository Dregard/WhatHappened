using Cysharp.Threading.Tasks;
using Game.Gameplay;
using Game.UI.Windows.General;

namespace Game.UI.Windows.MenuWindow
{
    public class MenuWindowModelProcessor : WindowModelProcessorBase<MenuWindowModel, EmptyModelParams>
    {
        private readonly WindowSystem _windowSystem;
        private readonly GameplaySystem _gameplaySystem;
        
        
        public MenuWindowModelProcessor(WindowSystem windowSystem, GameplaySystem gameplaySystem)
        {
            _windowSystem = windowSystem;
            _gameplaySystem = gameplaySystem;
        }
        
        public override MenuWindowModel GetModel()
        {
            return new MenuWindowModel
            {
                StartGameAction = StartGame,
            };
        }

        private async UniTask StartGame()
        {
            await _windowSystem.Close<MenuWindow>();
            
            _gameplaySystem.PrepareGame();
        }
    }
}