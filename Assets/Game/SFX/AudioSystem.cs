using UnityEngine;
using Zenject;

namespace Game.SFX
{
    public class AudioSystem : IInitializable
    {
        private const string SfxVolumeKey = "SfxVolume";
        private const string MusicVolumeKey = "MusicVolume";

        private const float ActiveMusicValue = .5f;
        private const float ActiveSoundValue = .7f;

        public float SfxVolume { get; private set; } = 1f;
        public float MusicVolume { get; private set; } = 1f;
        
        private SfxManager _sfxManager;
        private MusicManager _musicManager;


        public AudioSystem(SfxManager sfxManager, MusicManager musicManager)
        {
            _sfxManager = sfxManager;
            _musicManager = musicManager;
            
            var sfxVolume = PlayerPrefs.HasKey(SfxVolumeKey) 
                ? PlayerPrefs.GetFloat(SfxVolumeKey) 
                : ActiveSoundValue;
            SetSfxVolume(sfxVolume);

            var musicVolume = PlayerPrefs.HasKey(MusicVolumeKey)
                ? PlayerPrefs.GetFloat(MusicVolumeKey)
                : ActiveMusicValue;
            SetMusicVolume(musicVolume);
        }

        public void Initialize()
        {
            _musicManager.StartMusic(MusicVolume);
        }

        public void PlaySfx(string sfxName)
        {
            _sfxManager.Play(sfxName, SfxVolume);
        }

        public void StopSfx(string sfxName)
        {
            _sfxManager.Stop(sfxName);
        }

        public void ActivateSfx(bool isActive)
        {
            SetSfxVolume(isActive ? ActiveSoundValue : .0f);
        }

        public void ActivateMusic(bool isActive)
        {
            SetMusicVolume(isActive ? ActiveMusicValue : .0f);
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = value;

            PlayerPrefs.SetFloat(SfxVolumeKey, value);
            
            _sfxManager.SetVolume(value);
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = value;

            PlayerPrefs.SetFloat(MusicVolumeKey, value);
            
            _musicManager.SetVolume(value);
        }
    }
}