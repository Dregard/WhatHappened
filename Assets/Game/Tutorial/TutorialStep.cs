using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Game.Tutorial
{
    public abstract class TutorialStep : MonoBehaviour
    {
        [SerializeField] private float _delay;
        [SerializeField] private float _duration;

        protected SignalBus SignalBus;
        private CancellationTokenSource _stepCts;
        private Action _onCompleted;

        public float Delay => _delay;
        public float Duration => _duration;

        [Inject]
        public void Construct(SignalBus signalBus)
        {
            SignalBus = signalBus;
        }

        public virtual async UniTask Execute(Action onCompleted, CancellationToken ct)
        {
            _onCompleted = onCompleted;
            _stepCts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            if (_delay > 0)
            {
                await UniTask.Delay((int)(_delay * 1000), cancellationToken: _stepCts.Token);
            }
            
            ExecuteInternal();

            OnStepStarted();

            if (_duration > 0)
            {
                StartTimer(_duration, _stepCts.Token).Forget();
            }
        }

        protected abstract void OnStepStarted();

        protected virtual void Complete()
        {
            CompleteInternal();

            Cleanup();
            
            _onCompleted?.Invoke();
        }

        public virtual void Cleanup()
        {
            _stepCts?.Cancel();
            _stepCts?.Dispose();
            _stepCts = null;
        }

        private async UniTaskVoid StartTimer(float duration, CancellationToken ct)
        {
            try
            {
                await UniTask.Delay((int)(duration * 1000), cancellationToken: ct);
                Complete();
            }
            catch (OperationCanceledException)
            {
            }
        }
        
        
        protected abstract void ExecuteInternal();
        protected abstract void CompleteInternal();
    }
}
