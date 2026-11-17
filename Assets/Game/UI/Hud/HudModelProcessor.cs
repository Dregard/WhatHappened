using Cysharp.Threading.Tasks;
using Game.Boosters;
using Game.Gameplay;
using Game.UI.Windows.General;
using Game.UI.Windows.PauseWindow;
using Zenject;
using Game.Levels;
using Game.UI.Windows.BoosterWindow;
using Signals = Game.Tutorial.Signals;

namespace Game.UI.Hud
{
    public class HudModelProcessor : IInitializable
    {
        private readonly DiContainer _container;
        private readonly WindowSystem _windowSystem;
        private readonly GameplayProgressController _gameplayProgressController;
        private readonly HudView _view;
        private readonly LevelsManager _levelsManager;
        private readonly BoosterSystem _boosterSystem;
        private readonly LevelProgressController _levelProgressController;
        
        private HudModel _model;
        
        
        public HudModelProcessor(DiContainer container, WindowSystem windowSystem,
            HudView view, LevelsManager levelsManager, BoosterSystem boosterSystem, 
            LevelProgressController levelProgressController, SignalBus signalBus, GameplayProgressController gameplayProgressController)
        {
            _container = container;
            _windowSystem = windowSystem;
            _view = view;
            _levelsManager = levelsManager;
            _boosterSystem = boosterSystem;
            _levelProgressController = levelProgressController;
            _gameplayProgressController = gameplayProgressController;
            
            signalBus.Subscribe<Signals.TutorialStepCompletedSignal>(HandleStepCompleted);
            signalBus.Subscribe<Signals.ShowButtonRequested>(HandleButtonRequested);
        }

        public void Initialize()
        {
            _model = new HudModel
            {
                Container = _container,
                
                PauseAction = OnPausePressed,
                GetTimeFunc = GetTime,
                Boosters = _boosterSystem.Boosters,
                GetBoosterCountFunc = id => _boosterSystem.GetCount(id),
                CanUseBoosterFunc = id => _boosterSystem.CanUse(id),
                IsSearchStartedFunc = () => _levelsManager.CurrentLevelController?.AnomalyLevel?.Search?.IsStarted == true,
                IsBoosterUnlockedFunc = id => _boosterSystem.IsOpened(id),
                CurrentLevel = _levelsManager.CurrentLevelIndex + 1,
                UseBoosterAction = UseBooster,
                SubscribeOnBoosterCountChanged = action => _boosterSystem.OnBoosterCountChanged += action,
            };

            _levelsManager.LevelStartedEvent += () =>
            {
                _model.CurrentLevel = _gameplayProgressController.CurrentLevel;
                _model.InvokeLevelStarted();
            };
            
            _view.Setup(_model);
        }

        private float GetTime()
        {
            return _levelsManager.CurrentTimer;
        }

        private void OnPausePressed()
        {
            _windowSystem.Open<PauseWindow>().Forget();
        }

        /// <summary>Uses the same availability gate for purchases and spending, including pause, open photo and attempt limits.</summary>
        private void UseBooster(BoosterKind boosterKind)
        {
            if (!_boosterSystem.IsOpened(boosterKind) || !_boosterSystem.CanUse(boosterKind))
                return;
            var boosterCount = _boosterSystem.GetCount(boosterKind);
            if (boosterCount <= 0)
            {
                _windowSystem.Open<BoosterWindow>(new BoosterWindowModelParams
                {
                    BoosterKind = boosterKind
                }).Forget();
                return;
            }
            
            _boosterSystem.TryUse(boosterKind);
        }

        private void HandleStepCompleted(Signals.TutorialStepCompletedSignal signal)
        {
            _model.InvokeTutorialHideRequest();
        }

        private void HandleButtonRequested(Signals.ShowButtonRequested signal)
        {
            _model.InvokeTutorialButtonRequest(signal.Id);
        }
    }
}
