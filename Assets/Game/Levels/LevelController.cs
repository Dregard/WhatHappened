using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Camera;
using Game.SFX;
using Game.UI.Hud;
using UnityEngine;
using Zenject;
namespace Game.Levels
{
    public class LevelController : MonoBehaviour, IAdditionalCameraProvider
    {
        public event System.Action<LoseReason> LevelLostEvent;
        public event System.Action WinSequenceFinished;
        public AnomalyLevelController AnomalyLevel { get; private set; }
        [SerializeField] private UnityEngine.Camera _levelCamera;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private VignetteController _vignetteController;
        [SerializeField] private List<float> _timeoutWarningTimes = new() { 30f, 20f, 10f };
        public LevelAttemptTimeModel AttemptTime { get; } = new();
        public bool IsTimerPaused
        {
            get { return _isGameplayPaused || _isTimerFrozen || AnomalyLevel?.Boosters.IsPhotoOpen == true; }
            set
            {
                _isGameplayPaused = value;
                if (AnomalyLevel != null)
                    AnomalyLevel.Search.IsPaused = value || (AnomalyLevel.Search.IsStarted && !_isStarted);
            }
        }
        public bool HasTimer { get; private set; }
        private LevelProgressController _progressController;
        public LevelProgressController ProgressController => _progressController;
        public UnityEngine.Camera LevelCamera => _levelCamera;
        [Inject] private AudioSystem _audioSystem;
        [InjectOptional] private HudView _hudView;
        private float _levelTimer;
        private int _currentWarningIndex = -1;
        private bool _isGameplayPaused;
        private bool _isTimerFrozen;
        private bool _isStarted;
        private int _levelGeneration;
        public int LevelGeneration { get { return _levelGeneration; } }
        public float TimerValue => _levelTimer;
        public AdditionalCameraData[] CameraDatas => new AdditionalCameraData[]
        {
            new AdditionalCameraData
            {
                Camera = _levelCamera,
                Priority = 0,
            }
        };

        /// <summary>Removes the active level subscription when the persistent controller is destroyed.</summary>
        private void OnDestroy()
        {
            AnomalyLevel?.Dispose();
            if (_progressController != null)
                _progressController.TargetReached -= HandleTargetReached;
        }

        /// <summary>Stops gameplay and removes all runtime consumers before their Addressables assets are released on the next frame.</summary>
        public void ClearLevel()
        {
            AnomalyLevel?.Dispose();
            AnomalyLevel = null;
            _levelGeneration++;
            _isStarted = false;
            _levelTimer = 0;
            AttemptTime.Reset(false);
            HasTimer = false;
            _vignetteController.Stop();
            if (_progressController != null)
                _progressController.TargetReached -= HandleTargetReached;
            _progressController = null;
        }

        /// <summary>Accumulates attempt time on the same active ticks as the countdown, including untimed levels.</summary>
        private void Update()
        {
            if (AnomalyLevel != null && (!AnomalyLevel.Search.IsStarted || AnomalyLevel.Search.IsStageFound))
                return;
            HasTimer = _levelTimer > 0;

            AttemptTime.Tick(Time.deltaTime, _isStarted && !_isGameplayPaused && AnomalyLevel?.Boosters.IsPhotoOpen != true,
                _isTimerFrozen ? float.MaxValue : _levelTimer);
            if (_isStarted == false || IsTimerPaused || HasTimer == false || AttemptTime.IsCompleted)
                return;

            _levelTimer -= Time.deltaTime;

            UpdateWarningState();

            if (_levelTimer <= 0)
            {
                _isStarted = false;
                if (AnomalyLevel != null)
                    AnomalyLevel.Search.IsPaused = true;
                _vignetteController.Stop();
                LevelLostEvent?.Invoke(LoseReason.Time);
            }
        }

        /// <summary>Prepares search with the shared attempt clock; Ready alone enables the attempt.</summary>
        public UniTask InitializeSearch(LevelData data, LevelProgressController progress, int levelValue)
        {
            ClearLevel();
            _progressController = progress;
            _progressController.TargetReached += HandleTargetReached;
            _levelTimer = Mathf.Max(0, data.LevelTime);
            AttemptTime.Reset(_levelTimer > 0);
            HasTimer = _levelTimer > 0;
            _isTimerFrozen = false;
            ResetWarningState();
            AnomalyLevel = new AnomalyLevelController(_hudView != null ? _hudView.HudCanvas : _canvas, data.SearchData, progress, () => _isStarted = true,
                id => _audioSystem?.PlaySfx(id), id => _audioSystem?.StopSfx(id));
            return UniTask.CompletedTask;
        }

        /// <summary>Leaves search startup to the view's Ready and photograph gate.</summary>
        public UniTask ProcessLevelStart()
        {
            return UniTask.CompletedTask;
        }
        /// <summary>Resumes the shared clock after revival.</summary>
        public void Revive()
        {
            _isStarted = true;
        }

        /// <summary>Continues the timed attempt without clearing external gameplay pause or photo allowance.</summary>
        public void AddTime(float time)
        {
            _levelTimer += time;
            _isStarted = true;
            if (AnomalyLevel != null)
                AnomalyLevel.Search.IsPaused = _isGameplayPaused;

            ResetWarningState();
            UpdateWarningState();
        }

        /// <summary>Clears countdown warning presentation when starting or extending an attempt.</summary>
        private void ResetWarningState()
        {
            _currentWarningIndex = -1;
            _vignetteController.Stop();
        }
        /// <summary>Updates authored countdown warning thresholds and their sound and vignette feedback.</summary>
        private void UpdateWarningState()
        {
            if (_timeoutWarningTimes == null || _timeoutWarningTimes.Count == 0)
                return;
            var nextIndex = -1;
            for (var i = 0; i < _timeoutWarningTimes.Count; i++)
            {
                if (_levelTimer <= _timeoutWarningTimes[i])
                {
                    nextIndex = i;
                }
            }
            if (nextIndex > _currentWarningIndex)
            {
                _currentWarningIndex = nextIndex;
                if (_currentWarningIndex == _timeoutWarningTimes.Count - 1)
                {
                    _audioSystem.PlaySfx("TimeUp");
                    _vignetteController.ShowStatic(VignetteKind.TimeOut);
                }
                else
                {
                    _vignetteController.Play(VignetteKind.TimeOut);
                }
            }
            else if (nextIndex < _currentWarningIndex)
            {
                if (nextIndex == -1)
                {
                    ResetWarningState();
                }
                else
                {
                    _currentWarningIndex = nextIndex;
                    _vignetteController.Play(VignetteKind.TimeOut);
                }
            }
        }
        /// <summary>Restores the final countdown warning after an independent timer freeze.</summary>
        private void CheckWarningState()
        {
            if (_currentWarningIndex == _timeoutWarningTimes.Count - 1)
            {
                _vignetteController.ShowStatic(VignetteKind.TimeOut);
            }
        }
        /// <summary>Completes the shared clock after all search presentation gates; ignores stale sequence completion.</summary>
        private async void HandleTargetReached()
        {
            var generation = _levelGeneration;
            _isStarted = false;
            AttemptTime.Complete();
            _vignetteController.Stop();
            await PlayWinSequence();
            if (generation == _levelGeneration && this != null)
                WinSequenceFinished?.Invoke();
        }
        /// <summary>Plays victory audio on the next frame, leaving clue and chapter visuals to their owners.</summary>
        private async UniTask PlayWinSequence()
        {
            await UniTask.NextFrame();
            _audioSystem?.PlaySfx("Win");
        }
        /// <summary>Freezes countdown consumption without pausing active play or overriding window pauses.</summary>
        public void FreezeTimer()
        {
            _isTimerFrozen = true;
            _vignetteController.ShowStatic(VignetteKind.TimeFreeze);
        }
        /// <summary>Releases only the booster freeze; gameplay pauses remain authoritative.</summary>
        public void UnfreezeTimer()
        {
            _isTimerFrozen = false;
            _vignetteController.Stop();
            CheckWarningState();
        }
    }
}
