using Cysharp.Threading.Tasks;
using Game.Gameplay;
using Game.SFX;
using Game.UI.Windows.General;

namespace Game.UI.Windows.PauseWindow
{
    public class PauseWindowModelProcessor : WindowModelProcessorBase<PauseWindowModel, EmptyModelParams>
    {
        private readonly WindowSystem _windowSystem;
        private readonly AudioSystem _audioSystem;
        private readonly GameplaySystem _gameplaySystem;

        
        public PauseWindowModelProcessor(WindowSystem windowSystem, AudioSystem audioSystem, GameplaySystem gameplaySystem)
        {
            _windowSystem = windowSystem;
            _audioSystem = audioSystem;
            _gameplaySystem = gameplaySystem;
        }

        public override PauseWindowModel GetModel()
        {
            return new PauseWindowModel
            {
                ContinueAction = Continue,
                RestartAction = Restart,
                OpenCheatsAction = OpenCheats,
                CurrentSoundVolume = _audioSystem.SfxVolume,
                CurrentMusicVolume = _audioSystem.MusicVolume,
                SetSoundVolumeAction = _audioSystem.SetSfxVolume,
                SetMusicVolumeAction = _audioSystem.SetMusicVolume,
            };
        }

        private async UniTask Continue()
        {
            await _windowSystem.Close<PauseWindow>();
        }

        private async UniTask Restart()
        {
            await _gameplaySystem.Restart();

            _windowSystem.Close<PauseWindow>().Forget();
        }

        private void OpenCheats()
        {
            _windowSystem.Open<CheatsWindow.CheatsWindow>().Forget();
            _windowSystem.Close<PauseWindow>().Forget();
        }
    }
}
