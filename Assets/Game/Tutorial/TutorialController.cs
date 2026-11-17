using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Gameplay;
using Game.Levels;
using UnityEngine;
using Zenject;

namespace Game.Tutorial
{
    public class TutorialController : MonoBehaviour
    {
        public event System.Action Completed;
        [SerializeReference] private List<TutorialStep> _steps;
        
        private SignalBus _signalBus;
        private DiContainer _container;
        
        private GameplayProgressController _gameplayProgressController;
        private LevelProgressController _levelProgressController;
        
        private CancellationTokenSource _cts;
        private int _currentStepIndex = -1;

        
        [Inject]
        public void Construct(SignalBus signalBus, DiContainer container, 
            GameplayProgressController progressController, LevelProgressController levelProgressController)
        {
            _signalBus = signalBus;
            _container = container;
            
            _gameplayProgressController = progressController;
            _levelProgressController = levelProgressController;
        }

        private void Start()
        {
            _cts = new CancellationTokenSource();
            foreach (var step in _steps)
            {
                _container.Inject(step);
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            if (_currentStepIndex >= 0 && _currentStepIndex < _steps.Count)
            {
                _steps[_currentStepIndex].Cleanup();
            }
        }

        /// <summary>Advances the existing tutorial sequence and notifies its owner after the final successful step.</summary>
        public void StartNextStep()
        {
            _currentStepIndex++;
            if (_currentStepIndex >= _steps.Count)
            {
                Debug.Log("[Tutorial] Tutorial finished");
                _signalBus.Fire<Signals.TutorialCompletedSignal>();
                Completed?.Invoke();
                return;
            }

            var step = _steps[_currentStepIndex];
            step.Execute(CompleteStep, _cts.Token).Forget();
        }

        private void CompleteStep()
        {
            var step = _steps[_currentStepIndex];
            _signalBus.Fire(new Signals.TutorialStepCompletedSignal
            {
                StepId = step.name,
                LevelNumber = _gameplayProgressController.CurrentLevel,
                LevelAttempt = _gameplayProgressController.CurrentAttempt,
                LevelProgress = _levelProgressController.GetLevelProgress(),
            });

            StartNextStep();
        }
    }
}
