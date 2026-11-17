using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.SFX
{
    public class SfxEmitter : MonoBehaviour
    {
        [SerializeField] protected string _id;
        [SerializeField] protected AudioSource _audioSource;
        [SerializeField] protected AudioClip[] _clips;

        
        public string Id => _id;
        public bool IsPlaying => _audioSource.isPlaying;
        
        public void Play(float delay, float volume)
        {
            _audioSource.volume = volume;
            _audioSource.clip = GetClip();
            
            StartCoroutine(Routine(delay));
        }

        public void Stop()
        {
            StopAllCoroutines();
            _audioSource.Stop();
        }

        private IEnumerator Routine(float delay)
        {
            yield return new WaitForSeconds(delay);
            _audioSource.Play();
        }

        private AudioClip GetRandomClip()
        {
            if (_clips.Length == 0)
            {
                Debug.LogWarning($"No clips for {_id}");
                return null;
            }

            if (_clips.Length == 1)
                return _clips[0];
            
            var i = Random.Range(0, _clips.Length);
            return _clips[i];
        }

        protected virtual AudioClip GetClip()
        {
            return GetRandomClip();
        }

        public void SetVolume(float value)
        {
            _audioSource.volume = value;
        }
    }
}