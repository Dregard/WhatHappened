using UnityEngine;

namespace Game.Effects
{
    public class GameplayEffect : Effect
    {
        [SerializeField] private ParticleSystem _effect;


        public override float GetDuration()
        {
            if (_effect.main.loop)
            {
                return -1f;
            }
            
            return _effect.main.duration + 0.3f;
        }
        
        public override void Play()
        {
            _effect.gameObject.SetActive(true);
            
            base.Play();
        }

        public override void ResetEffect()
        {
            base.ResetEffect();
            
            _effect.gameObject.SetActive(false);
        }
    }
}