using System;
using UnityEngine;

namespace Game.Anomalies
{
    public sealed class AnomalyBoosterController : IDisposable
    {
        // Product constraint: every photo view lasts five active seconds.
        public const float PhotoDuration = 5f;
        private readonly AnomalySearchSystem _search;
        private float _photoRemaining;
        private float _hintRemaining;
        private int _hintStage;
        private bool _isDisposed;
        public event Action Changed;
        public int PhotoUses { get; private set; }
        public bool IsPhotoOpen { get; private set; }
        public float PhotoRemaining { get { return Mathf.Max(0, _photoRemaining); } }
        public AnomalyData HintTarget { get; private set; }
        public AnomalyHintData HintData { get; private set; }
        public bool CanPhoto { get { return IsAvailable() && !IsPhotoOpen; } }
        public bool CanHint { get { return IsAvailable() && !IsPhotoOpen && HintTarget == null && FindTarget() != null; } }

        /// <summary>Owns usage and effect lifetime for one attempt only; no stage transition resets the photo allowance.</summary>
        public AnomalyBoosterController(AnomalySearchSystem search)
        {
            _search = search;
            _search.Changed += Refresh;
        }

        /// <summary>Accepts a valid pointer only when an unfound current-stage target exists; the economy validates availability before spending.</summary>
        public bool TryHint(AnomalyHintData data, float duration)
        {
            if (!CanHint || !IsValidHint(data) || duration <= 0 || !float.IsFinite(duration))
                return false;
            HintTarget = FindTarget();
            HintData = data;
            _hintStage = _search.StageIndex;
            _hintRemaining = duration;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Validates authored visual/timing values before the economy is allowed to spend a hint.</summary>
        public static bool IsValidHint(AnomalyHintData data)
        {
            return data != null && data.FingerSprite != null &&
                data.FingerSize.x > 0 && data.FingerSize.y > 0 && float.IsFinite(data.FingerSize.x) &&
                float.IsFinite(data.FingerSize.y) && data.PulseDuration > 0 && float.IsFinite(data.PulseDuration) &&
                data.Fingertip.x >= 0 && data.Fingertip.x <= 1 && data.Fingertip.y >= 0 && data.Fingertip.y <= 1 &&
                data.PulseScale >= 0 && data.PulseScale <= 0.5f;
        }

        /// <summary>Opens the original once and immediately reserves one of the three attempt-local uses, rejecting duplicate presses.</summary>
        public bool TryPhoto()
        {
            if (!CanPhoto)
                return false;
            PhotoUses++;
            IsPhotoOpen = true;
            _photoRemaining = PhotoDuration;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Advances effect clocks in active game time; external gameplay pauses preserve the remaining photo/hint time.</summary>
        public void Tick(float deltaTime)
        {
            if (_isDisposed || _search.IsPaused || deltaTime <= 0)
                return;
            if (IsPhotoOpen)
            {
                _photoRemaining -= deltaTime;
                if (_photoRemaining <= 0)
                {
                    IsPhotoOpen = false;
                    Changed?.Invoke();
                }
                return;
            }
            if (HintTarget == null)
                return;
            _hintRemaining -= deltaTime;
            if (_hintRemaining <= 0)
                ClearHint();
        }

        /// <summary>Restricts effects to active search; completed stages cannot consume resources while their findings finish.</summary>
        private bool IsAvailable()
        {
            return !_isDisposed && _search.IsStarted && !_search.IsPaused && !_search.IsCompleted &&
                !_search.IsTransitionPending && !_search.IsStageFound;
        }

        /// <summary>Selects only a current-stage anomaly that has not already been accepted as found.</summary>
        private AnomalyData FindTarget()
        {
            foreach (var anomaly in _search.CurrentAnomalies)
                if (!_search.IsFound(anomaly.Id))
                    return anomaly;
            return null;
        }

        /// <summary>Removes obsolete guidance synchronously on findings, stage changes and attempt completion.</summary>
        private void Refresh()
        {
            if (HintTarget != null && (_search.IsFound(HintTarget.Id) || _hintStage != _search.StageIndex || _search.IsCompleted))
                ClearHint();
            if (_search.IsCompleted && IsPhotoOpen)
            {
                IsPhotoOpen = false;
                Changed?.Invoke();
            }
        }

        /// <summary>Clears the target before notifying presentation; no stale pointer remains after an accepted finding.</summary>
        private void ClearHint()
        {
            HintTarget = null;
            Changed?.Invoke();
        }

        /// <summary>Ends effects and detaches state notifications before the attempt's view or images are released.</summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            _search.Changed -= Refresh;
            HintTarget = null;
            IsPhotoOpen = false;
            Changed?.Invoke();
        }
    }
}
