using Game.Boosters;
using Game.Gameplay;
using Game.UI.Windows.General;

namespace Game.UI.Windows.BoosterUnlockWindow
{
    public class BoosterUnlockWindowModelProcessor : WindowModelProcessorBase<BoosterUnlockWindowModel, BoosterUnlockWindowModelParams>
    {
        private readonly WindowSystem _windowSystem;
        private readonly GameplaySystem _gameplaySystem;
        private readonly BoosterSystem _boosterSystem;
        
        private BoosterKind _boosterKind;

        public BoosterUnlockWindowModelProcessor(WindowSystem windowSystem, GameplaySystem gameplaySystem, BoosterSystem boosterSystem)
        {
            _windowSystem = windowSystem;
            _gameplaySystem = gameplaySystem;
            _boosterSystem = boosterSystem;
        }

        public override void SetParams(BoosterUnlockWindowModelParams modelParams)
        {
            _boosterKind = modelParams.BoosterKind;
        }

        public override BoosterUnlockWindowModel GetModel()
        {
            var boosterModel = _boosterSystem.GetViewModel(_boosterKind);
            return new BoosterUnlockWindowModel
            {
                BoosterViewModel = boosterModel,
                CloseAction = Close,
            };
        }

        private async void Close()
        {
            await _windowSystem.Close<BoosterUnlockWindow>();
        }
    }
}
