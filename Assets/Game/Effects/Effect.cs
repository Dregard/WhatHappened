using UnityEngine;
using System;

namespace Game.Effects
{
    public abstract class Effect : MonoBehaviour
    {
        public event Action<Effect> Finished;

        public abstract float GetDuration();

        public virtual void Play()
        {
            float duration = GetDuration();
            if (duration > 0)
            {
                Invoke(nameof(Finish), duration);
            }
        }

        public virtual void Stop()
        {
            Finish();
        }

        protected virtual void Finish()
        {
            Finished?.Invoke(this);
        }
        
        public virtual void ResetEffect()
        {
            CancelInvoke(nameof(Finish));
        }
    }
}
