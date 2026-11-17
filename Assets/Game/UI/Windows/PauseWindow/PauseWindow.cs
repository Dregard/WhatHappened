using System;
using Cysharp.Threading.Tasks;
using Game.UI.Buttons;
using Game.UI.Windows.General;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.Windows.PauseWindow
{
    public class PauseWindow : Window<PauseWindowModel>
    {
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private GameButton _cheatsButton;
        [SerializeField] private VolumeSlider _soundVolumeSlider;
        [SerializeField] private VolumeSlider _musicVolumeSlider;

        private Func<UniTask> _continueAction;
        private Func<UniTask> _restartAction;

        
        protected override void Setup(PauseWindowModel model)
        {
            _continueAction = model.ContinueAction;
            _restartAction = model.RestartAction;
            _soundVolumeSlider.SetVolume(model.CurrentSoundVolume);
            _soundVolumeSlider.OnVolumeChanged += model.SetSoundVolumeAction;
            _musicVolumeSlider.SetVolume(model.CurrentMusicVolume);
            _musicVolumeSlider.OnVolumeChanged += model.SetMusicVolumeAction;

            _continueButton.onClick.AddListener(Continue);
            _restartButton.onClick.AddListener(Restart);
            
            _cheatsButton.gameObject.SetActive(Debug.isDebugBuild || Application.isEditor);
            _cheatsButton.ClickEvent += () => model.OpenCheatsAction?.Invoke();
        }

        private void Continue()
        {
            _continueAction?.Invoke().Forget();
        }

        private void Restart()
        {
            _restartAction?.Invoke().Forget();
        }
    }
}
