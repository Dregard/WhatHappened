using System;
using Cysharp.Threading.Tasks;
using Game.UI.Windows.General;

namespace Game.UI.Windows.PauseWindow
{
    public class PauseWindowModel : IWindowModel
    {
        public Func<UniTask> ContinueAction;
        public Func<UniTask> RestartAction;
        public Action OpenCheatsAction;
        public float CurrentSoundVolume;
        public float CurrentMusicVolume;
        public Action<float> SetSoundVolumeAction;
        public Action<float> SetMusicVolumeAction;
    }
}
