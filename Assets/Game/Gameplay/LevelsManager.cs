using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Game.AssetManagement;
using Game.Camera;
using Game.Levels;
using Game.Profile;
using Game.Tutorial;
using Game.Utils;
using Zenject;
using Object = UnityEngine.Object;

namespace Game.Gameplay
{
    public class LevelsManager : IDisposable, IApplicationPauseListener, IPausable
    {
        public event Action<LoseReason> LevelLostEvent;
        public event Action LevelStartedEvent;

        private readonly AddressablesAssetSystem _assetSystem;
        private readonly LevelProgressController _levelProgressController;
        private readonly MainCameraController _cameraController;
        private readonly SignalBus _signalBus;
        private readonly GameplayProgressController _gameplayProgressController;
        private readonly ProfileSystem _profileSystem;
        private readonly LevelController _levelController;
        private readonly LevelContentSystem _contentSystem;
        private readonly UnityLifecycleSystem _lifecycleSystem;
        private Task _unloadTask = Task.CompletedTask;
        private Task _loadTask = Task.CompletedTask;
        private bool _isDisposed;
        private int _requestedLevelIndex;
        private int _transitionGeneration;

        private LevelsData _levelsData;
        private LevelData _currentLevelData;
        private DiContainer _container;

        private TutorialController _tutorialController;
        private int _currentLevelIndex;

        public int CurrentLevelIndex => _currentLevelIndex;
        public string CurrentLevelId { get; private set; }
        public LevelsData LevelsData => _levelsData;
        public MainCameraController MainCameraController => _cameraController;
        /// <summary>Connects gameplay consumers and scoped content ownership, subscribing to loss and application-pause events until disposal.</summary>
        public LevelsManager(AddressablesAssetSystem assetSystem, LevelProgressController progressController, MainCameraController cameraController,
            SignalBus signalBus, DiContainer container,
            GameplayProgressController gameplayProgressController, ProfileSystem profileSystem, UnityLifecycleSystem lifecycleSystem,
            LevelController levelController, LevelContentSystem contentSystem)
        {
            _assetSystem = assetSystem;
            _levelProgressController = progressController;
            _cameraController = cameraController;
            _signalBus = signalBus;
            _container = container;
            _gameplayProgressController = gameplayProgressController;
            _profileSystem = profileSystem;
            _levelController = levelController;
            _contentSystem = contentSystem;
            _lifecycleSystem = lifecycleSystem;
            
            lifecycleSystem.AddListener(this);
            
            _levelController.LevelLostEvent += HandleLevelLost;
        }

        public LevelAttemptStatisticsModel CompletedAttempt { get; private set; }

        /// <summary>Snapshots the completed search clock for the level victory view.</summary>
        public void CaptureCompletedAttempt()
        {
            CompletedAttempt = new LevelAttemptStatisticsModel(_levelController.AttemptTime.ElapsedSeconds,
                _levelController.AttemptTime.QualifiesForLike);
        }

        public float CurrentTimer => _levelController == null ? 0 : _levelController.TimerValue;
        public LevelController CurrentLevelController => _levelController;

        /// <summary>Waits for requested teardown before loading anew; otherwise shares the active selection's load until unloading is requested.</summary>
        public UniTask LoadLevel(int levelIndex, IProgress<float> progress = null)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(LevelsManager));
            if (!_unloadTask.IsCompleted)
                return LoadAfterUnloadAsync(levelIndex, progress);
            if (!_loadTask.IsCompleted)
            {
                if (_requestedLevelIndex != levelIndex)
                    throw new InvalidOperationException("Finish the current transition before selecting another level.");
                return _loadTask.AsUniTask();
            }
            if (CurrentLevelId != null)
            {
                if (_currentLevelIndex != levelIndex)
                    throw new InvalidOperationException("Unload the current level before selecting another level.");
                progress?.Report(1f);
                return UniTask.CompletedTask;
            }
            _requestedLevelIndex = levelIndex;
            _loadTask = PrepareLevelAsync(levelIndex, _transitionGeneration, progress).AsTask();
            return _loadTask.AsUniTask();
        }

        /// <summary>Queues a fresh selection after the old consumers and asset ownership have been released.</summary>
        private async UniTask LoadAfterUnloadAsync(int levelIndex, IProgress<float> progress)
        {
            await _unloadTask;
            await LoadLevel(levelIndex, progress);
        }

        /// <summary>Rolls back failed initialization before allowing a retry; the asset system also releases failed load handles.</summary>
        private async UniTask PrepareLevelAsync(int levelIndex, int generation, IProgress<float> progress)
        {
            try
            {
                await LoadLevelAsync(levelIndex, generation, progress);
            }
            catch
            {
                await UnloadLevelAsync();
                throw;
            }
        }

        /// <summary>Waits for the previous release, prepares the selected level with anomaly content and the shared timer, then starts background disk downloads after initialization.</summary>
        private async UniTask LoadLevelAsync(int levelIndex, int generation, IProgress<float> progress)
        {
            await _unloadTask;
            CheckData();
            await _contentSystem.EnsureCurrentLevelAsync(levelIndex, Progress.Create<float>(value => progress?.Report(value * 0.5f)));
            ThrowIfTransitionInvalid(generation);
            progress?.Report(0.5f);
            _currentLevelIndex = levelIndex;
            var levelReference = _levelsData.Levels[_currentLevelIndex];
            CurrentLevelId = levelReference.LevelId;
            await _assetSystem.LoadAsync(CurrentLevelId, Progress.Create<float>(value => progress?.Report(0.5f + value * 0.35f)));
            ThrowIfTransitionInvalid(generation);
            progress?.Report(0.85f);
            _currentLevelData = _assetSystem.GetAsset<LevelData>(CurrentLevelId);
            if (_gameplayProgressController.UsesChapters && (_currentLevelData.SearchData?.ClueImage == null || _currentLevelData.SearchData.MainImage == null))
                throw new InvalidOperationException("Every chapter level requires SearchData.MainImage and SearchData.ClueImage.");
            CompletedAttempt = null;
            if (_currentLevelData.SearchData?.MainImage == null)
                throw new InvalidOperationException("Assign SearchData.MainImage before loading this anomaly level.");
            var tutorialData = _levelsData.Tutorials?.Find(t => t.LevelNumber == _currentLevelIndex + 1);
            if (tutorialData.HasValue && tutorialData.Value.TutorialPrefab != null)
                _tutorialController = _container.InstantiatePrefabForComponent<TutorialController>(tutorialData.Value.TutorialPrefab, _levelController.transform);
            await _levelController.InitializeSearch(_currentLevelData, _levelProgressController, _gameplayProgressController.CurrentLevel);
            ThrowIfTransitionInvalid(generation);
            LevelStartedEvent?.Invoke();
            _contentSystem.MaintainReserve();
            progress?.Report(1f);
        }

        /// <summary>Starts presentation only after the selected level is initialized; unload invalidates pending starts.</summary>
        public async UniTask ProcessLevelStart()
        {
            var generation = _transitionGeneration;
            await _loadTask;
            ThrowIfTransitionInvalid(generation);
            await _levelController.ProcessLevelStart();
            ThrowIfTransitionInvalid(generation);
        }

        /// <summary>Rejects late load or preview continuations without canceling their underlying Addressables ownership.</summary>
        private void ThrowIfTransitionInvalid(int generation)
        {
            if (_isDisposed || generation != _transitionGeneration)
                throw new OperationCanceledException("The level transition is no longer current.");
        }

        /// <summary>Starts a retained general tutorial authored for the current level.</summary>
        public void CheckTutorial()
        {
            StartAuthoredTutorial();
        }

        /// <summary>Starts the level-specific sequence, when present.</summary>
        private void StartAuthoredTutorial()
        {
            _tutorialController?.StartNextStep();
        }

        /// <summary>Starts removal of runtime consumers. The next load waits for deferred destruction and Addressables release.</summary>
        public void UnloadLevel()
        {
            if (!_unloadTask.IsCompleted)
                return;
            _transitionGeneration++;
            _levelController.ClearLevel();
            _unloadTask = ReleaseAfterLoadAsync().AsTask();
        }

        /// <summary>Lets an in-flight load finish or roll back before destroying its consumers; repeated unload requests share this completion.</summary>
        private async UniTask ReleaseAfterLoadAsync()
        {
            try
            {
                await _loadTask;
            }
            catch (Exception)
            {
                // PrepareLevelAsync already rolled back a failed transition.
            }
            await UnloadLevelAsync();
        }

        /// <summary>Destroys all level consumers before releasing their asset ownership; retained loop levels remain on disk only.</summary>
        private async UniTask UnloadLevelAsync()
        {
            var levelId = CurrentLevelId;
            if (levelId == null)
            {
                return;
            }
            CurrentLevelId = null;
            if (_tutorialController != null)
            {
                Object.Destroy(_tutorialController.gameObject);
                _tutorialController = null;
            }
            
            var hasRuntimeConsumers = _levelController != null;
            if (hasRuntimeConsumers)
                _levelController.ClearLevel();
            _currentLevelData = null;
            if (hasRuntimeConsumers && UnityEngine.Application.isPlaying)
                await UniTask.NextFrame();
            _assetSystem.Release(levelId);
            if (!_isDisposed)
            {
                _contentSystem.HandleLevelReleased(_currentLevelIndex);
            }
        }

        /// <summary>Resolves the local catalog and configures saved progression before selecting any gameplay level.</summary>
        public void CheckData()
        {
            if (_levelsData == null)
                _levelsData = _assetSystem.GetAsset<LevelsData>("LevelsData");
            _gameplayProgressController.Configure(_levelsData);
            _contentSystem.Configure(_levelsData);
        }

        private void HandleLevelLost(LoseReason reason)
        {
            LevelLostEvent?.Invoke(reason);
        }
        
        /// <summary>Continues this attempt with purchased or rewarded time and removes its timed-level like eligibility.</summary>
        public void HandleExtraTimeBought(float time)
        {
            _levelController.AttemptTime.MarkExtraTimeReceived();
            _levelController.AddTime(time);
            
            _signalBus.Fire(new UI.Windows.Signals.ExtraTimeBoughtSignal
            {
                LevelNumber = _gameplayProgressController.ActualLevelNumber,
                LevelAttempt = _gameplayProgressController.CurrentAttempt,
                LevelProgress = _levelProgressController.GetLevelProgress(),
            });
        }

        public void HandleRevive()
        {
            _levelController.Revive();
            
            _signalBus.Fire(new UI.Windows.Signals.ExtraLifeBoughtSignal
            {
                LevelNumber = _gameplayProgressController.ActualLevelNumber,
                LevelAttempt = _gameplayProgressController.CurrentAttempt,
                LevelProgress = _levelProgressController.GetLevelProgress(),
            });
        }

        public void Pause()
        {
            if (_levelController == null)
                return;

            _levelController.IsTimerPaused = true;
        }

        public void Unpause()
        {
            if (_levelController == null)
                return;

            _levelController.IsTimerPaused = false;
        }

        public void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                if (_levelController != null)
                {
                    _gameplayProgressController.SetAbandonedMissionData(_gameplayProgressController.ActualLevelNumber, _gameplayProgressController.CurrentAttempt, _levelProgressController.GetLevelProgress());
                    _profileSystem.Save();
                }
            }
            else
            {
                _gameplayProgressController.SetAbandonedMissionData(null);
            }
        }

        /// <summary>Removes subscriptions and delays asset release until any in-flight transition has completed.</summary>
        public void Dispose()
        {
            _isDisposed = true;
            _lifecycleSystem.RemoveListener(this);
            _levelController.LevelLostEvent -= HandleLevelLost;
            
            if (_levelController != null)
            {
                _gameplayProgressController.SetAbandonedMissionData(_gameplayProgressController.ActualLevelNumber, _gameplayProgressController.CurrentAttempt, _levelProgressController.GetLevelProgress());

                _gameplayProgressController.HandleLevelFailed();
                _profileSystem.Save();
            }
            DisposeResourcesAsync().Forget();
        }

        /// <summary>Awaits late load results without starting another level, then balances remaining asset ownership.</summary>
        private async UniTask DisposeResourcesAsync()
        {
            try
            {
                await _loadTask;
            }
            catch (Exception)
            {
                // Failed loads are rolled back by the asset system; required downloads may be cancelled during teardown.
            }
            await _unloadTask;
            await UnloadLevelAsync();
        }
    }
}
