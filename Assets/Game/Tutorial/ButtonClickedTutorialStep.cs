using Game.Gameplay;
using UnityEngine;
using Zenject;

namespace Game.Tutorial
{
    public class ButtonClickedTutorialStep : TutorialStep
    {
        [SerializeField] private GameObject _tutorialObject;
        [SerializeField] private string _targetId;
        [SerializeField] private bool _needHighlight;

        private GameplaySystem _gameplaySystem;
        

        [Inject]
        public void Construct(GameplaySystem gameplaySystem)
        {
            _gameplaySystem = gameplaySystem;
        }
        
        protected override void OnStepStarted()
        {
            _gameplaySystem.Pause(this);
            
            SignalBus.Subscribe<Signals.ButtonClicked>(HandleButtonClicked);
            
            if (_needHighlight)
            {
                SignalBus.Fire(new Signals.ShowButtonRequested
                {
                    Id = _targetId,
                });
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            SignalBus?.TryUnsubscribe<Signals.ButtonClicked>(HandleButtonClicked);
            _gameplaySystem.Unpause(this);
        }

        private void HandleButtonClicked(Signals.ButtonClicked signal)
        {
            var buttonId = signal.Id;
            if (string.IsNullOrEmpty(_targetId) || _targetId == buttonId)
                Complete();
        }

        protected override void ExecuteInternal()
        {
            if (_tutorialObject != null)
                _tutorialObject.SetActive(true);
        }

        protected override void CompleteInternal()
        {
            if (_tutorialObject != null)
                _tutorialObject.SetActive(false);
            
            _gameplaySystem.Unpause(this);
        }
    }
}
