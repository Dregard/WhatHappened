using UnityEngine;

namespace Game.SFX
{
    public class MusicManager : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;

        
        public void StartMusic(float volume)
        {
            SetVolume(volume);
            
            _audioSource.loop = true;
            _audioSource.Play();
        }

        public void SetVolume(float value)
        {
            _audioSource.volume = value;
        }
    }
}