using System;
using Cysharp.Threading.Tasks;
using Game.Boosters;
using Game.Gameplay.Chapters;
using Game.Levels;
using Game.Profile;
using Game.SFX;
using Game.UI.Hud;
using Game.UI.Windows.General;
using Game.UI.Windows.PreloadWindow;
using Game.UI.Windows.TimeLoseWindow;
using Game.UI.Windows.WinWindow;
using Game.Utils;
using UnityEngine;
using Zenject;

namespace Game.Gameplay
{
    public class GameplaySystem
    {
        private readonly WindowSystem _windowSystem;
        private readonly HudView _hudView;
        private readonly GameplayProgressController _progressController;
        private readonly LevelsManager _levelsManager;
        private readonly LevelProgressController _levelProgressController;
        private readonly ProfileSystem _profileSystem;
        private readonly BoosterSystem _boosterSystem;
        private readonly SignalBus _signalBus;
        private readonly AudioSystem _audioSystem;
        private readonly PauseService _pauseService;
        private readonly ChapterSystem _chapters;
        
        private bool _gameStarted;
        private int _sessionGeneration;
        
        
        /// <summary>Receives scoped gameplay services for preparation, presentation and progression.</summary>
        public GameplaySystem(WindowSystem windowSystem, HudView hudView,
            GameplayProgressController progressController, LevelsManager levelsManager, 
            LevelProgressController levelProgressController, ProfileSystem profileSystem,
            BoosterSystem boosterSystem, SignalBus signalBus, AudioSystem audioSystem,
            PauseService pauseService, ChapterSystem chapters)
        {
            _windowSystem = windowSystem;
            _hudView = hudView;
            _progressController = progressController;
            _levelsManager = levelsManager;
            _levelProgressController = levelProgressController;
            _profileSystem = profileSystem;
            _signalBus = signalBus;
            _boosterSystem = boosterSystem;
            _audioSystem = audioSystem;
            _pauseService = pauseService;
            _chapters = chapters;
        }

        /// <summary>Prepares and pauses the session without opening a window. When startup supplies its preload model, completes its progress and closes the initial window after readiness. Reset invalidates late continuations.</summary>
        public async UniTask PrepareGame(PreloadWindowModel preloadModel = null)
        {
            var generation = ++_sessionGeneration;
            preloadModel?.Report(0.15f);
            _windowSystem.WindowOpenedEvent += HandleWindowOpened;
            _windowSystem.WindowClosedEvent += HandleWindowClosed;
            
            await LoadNextLevel(Progress.Create<float>(value =>
            {
                if (generation == _sessionGeneration)
                    preloadModel?.Report(0.15f + value * 0.8f);
            }));
            ThrowIfSessionInvalid(generation);
            _chapters.Prepare(_levelsManager.LevelsData);
            
            Pause(this);
            if (preloadModel != null)
            {
                preloadModel.Complete();
                await UniTask.NextFrame();
                ThrowIfSessionInvalid(generation);
                await _windowSystem.Close<PreloadWindow>();
            }
        }

        /// <summary>Runs the ready preview and existing gameplay start only while this session remains current.</summary>
        public async UniTask StartGame()
        {
            var generation = _sessionGeneration;
            await _windowSystem.WaitAllClosed();
            ThrowIfSessionInvalid(generation);
            if (_progressController.UsesChapters && (_progressController.IsLevelVictoryPending || _progressController.IsChapterComplete))
            {
                if (_progressController.IsChapterRewardClaimed)
                    await ContinueChapter();
                else
                    await _chapters.Resume(() => ContinueLevel().Forget(), () => ContinueChapter().Forget());
                return;
            }
            
            await _levelsManager.ProcessLevelStart();
            ThrowIfSessionInvalid(generation);
            
            _signalBus.Fire(new Signals.LevelStarted
            {
                LevelNumber = _progressController.CurrentLevel,
                LevelAttempt = _progressController.CurrentAttempt,
            });

            _boosterSystem.CheckUnlocks(_progressController.ActualLevelNumber - 1);
            _boosterSystem.RefillPerLevelBoosters();
            
            await _windowSystem.WaitAllClosed();
            ThrowIfSessionInvalid(generation);
            
            _levelsManager.CheckTutorial();
            
            _levelProgressController.TargetReached += HandleWin;
            _levelsManager.LevelLostEvent += HandleLose;
            
            _gameStarted = true;
            
            
            _pauseService.ForceUnpauseAll();
        }

        /// <summary>Prevents an obsolete session from showing another level or re-enabling gameplay after reset.</summary>
        private void ThrowIfSessionInvalid(int generation)
        {
            if (generation != _sessionGeneration)
                throw new OperationCanceledException("The gameplay session was replaced.");
        }

        private void HandleWindowOpened()
        {
            if (_gameStarted)
                Pause(_windowSystem);
        }
        
        private void HandleWindowClosed()
        {
            if (_windowSystem.OpenWindowsCount == 0)
                Unpause(_windowSystem);
        }

        public void Pause(object requester)
        {
            _pauseService.Pause(requester);
        }

        public void Unpause(object requester)
        {
            _pauseService.Unpause(requester);
        }

        private void Finish()
        {
            Pause(this);
            
            _levelProgressController.TargetReached -= HandleWin;
            _levelsManager.LevelLostEvent -= HandleLose;
            _windowSystem.WindowOpenedEvent -= HandleWindowOpened;
            _windowSystem.WindowClosedEvent -= HandleWindowClosed;
        }

        public async UniTask Restart()
        {
            Debug.Log("Restart");
            
            Reset();

            await PrepareGame();
            
            StartGame().Forget();
        }

        public void ForceWin()
        {
            _levelProgressController.ForceWin();
        }

        /// <summary>Invalidates pending starts before unloading consumers and resetting the HUD.</summary>
        public void Reset()
        {
            _sessionGeneration++;
            _chapters.Cancel();
            Finish();
            
            _levelsManager.UnloadLevel();
            
            _hudView.ResetHud();
            _gameStarted = false;
        }

        /// <summary>Waits for search presentation and clue arrival, persists completion, then opens mandatory chapter-level victory.</summary>
        private async void HandleWin()
        {
            var generation = _sessionGeneration;
            if (_progressController.UsesChapters)
                _chapters.BeginCompletion();
            Debug.Log("Win");
            
            _signalBus.Fire(new Signals.LevelFinished
            {
                LevelNumber = _progressController.CurrentLevel,
                LevelAttempt = _progressController.CurrentAttempt,
            });
            
            Finish();
            
            var completionSource = new UniTaskCompletionSource();
            Action onSequenceFinished = null;
            onSequenceFinished = () =>
            {
                _levelsManager.CurrentLevelController.WinSequenceFinished -= onSequenceFinished;
                completionSource.TrySetResult();
            };
            _levelsManager.CurrentLevelController.WinSequenceFinished += onSequenceFinished;

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            if (generation != _sessionGeneration)
                return;
            _levelsManager.CaptureCompletedAttempt();
            await completionSource.Task;
            if (generation != _sessionGeneration)
                return;

            if (_progressController.UsesChapters)
            {
                try
                {
                    var level = _levelsManager.CurrentLevelController.AnomalyLevel;
                    await _chapters.ShowResult(level.ClueImage, level.ClueName, () => ContinueLevel().Forget(), () => ContinueChapter().Forget());
                }
                catch (OperationCanceledException) when (generation != _sessionGeneration)
                {
                }
                return;
            }
            _progressController.HandleLevelCompleted();
            _profileSystem.Save();
            await _windowSystem.Open<WinWindow>();
        }

        private void HandleLose(LoseReason reason)
        {
            Debug.Log($"Lose: {reason}");

            _audioSystem.PlaySfx("GameOver");
            
            switch (reason)
            {
                case LoseReason.Time:
                    _windowSystem.Open<TimeLoseWindow>().Forget();
                    break;
            }
            
            _signalBus.Fire(new Signals.LevelFailedSoft
            {
                LevelNumber = _progressController.CurrentLevel,
                LevelAttempt = _progressController.CurrentAttempt,
                LevelProgress = _levelProgressController.GetLevelProgress(),
                Reason = reason,
            });
        }

        /// <summary>Closes the chapter result screens after the level's reward claim and loads the next level of the chapter.</summary>
        private async UniTask ContinueLevel()
        {
            var generation = _sessionGeneration;
            await _windowSystem.Close<ChapterWindow>();
            ThrowIfSessionInvalid(generation);
            await Restart();
        }

        /// <summary>Closes the rewarded chapter before resetting slots and loading the next chapter, wrapping after the final configured chapter.</summary>
        private async UniTask ContinueChapter()
        {
            if (!_progressController.StartNextChapter())
                return;
            _profileSystem.Save();
            await _windowSystem.Close<ChapterWindow>();
            await Restart();
        }

        /// <summary>Configures progression before choosing a level, including resumed games already in the final loop.</summary>
        private async UniTask LoadNextLevel(IProgress<float> progress)
        {
            _levelsManager.CheckData();
            var levelIndex = _progressController.ActualLevelIndex;
            await _levelsManager.LoadLevel(levelIndex, progress);
        }

        public void HandleFail(LoseReason reason)
        {
            _signalBus.Fire(new Signals.LevelFailed
            {
                LevelNumber = _progressController.CurrentLevel,
                LevelAttempt = _progressController.CurrentAttempt,
                LevelProgress = _levelProgressController.GetLevelProgress(),
                Reason = reason,
            });
            _progressController.HandleLevelFailed();
        }
    }
}
