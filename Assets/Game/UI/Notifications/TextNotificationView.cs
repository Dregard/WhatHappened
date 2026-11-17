using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.UI.Notifications
{
    public sealed class TextNotificationView : MonoBehaviour
    {
        [Tooltip("HUD text used exclusively for queued notifications; it does not receive pointer input.")]
        [SerializeField] private TMP_Text _text;
        [Tooltip("Seconds to fade from transparent to opaque and settle the entrance scale. Must be positive.")]
        [Min(0.01f)] [SerializeField] private float _appearDuration = 0.3f;
        [Tooltip("Entrance scale relative to the authored text scale; below one grows into place.")]
        [Range(0.01f, 1f)] [SerializeField] private float _startScale = 0.85f;
        [Tooltip("OutBack overshoot strength. Zero removes overshoot; larger values increase the scale bounce.")]
        [Min(0f)] [SerializeField] private float _bounceStrength = 1f;

        [Header("Flight to star counter")]
        [Tooltip("Currency counter icon whose rectangle center receives the notification.")]
        [SerializeField] private Image _flightTarget;
        [FormerlySerializedAs("_holdDuration")]
        [Tooltip("Unscaled seconds to keep the text visible after entrance and before flight. Zero starts immediately.")]
        [Min(0f)] [SerializeField] private float _flightDelay = 1.5f;
        [FormerlySerializedAs("_hideDuration")]
        [Tooltip("Flight duration in unscaled seconds, after the delay. Must be positive.")]
        [Min(0.01f)] [SerializeField] private float _flightDuration = 0.7f;
        [Tooltip("Flight progress over normalized time: 0 is the starting position, 1 is the counter. Use endpoints (0,0) and (1,1).")]
        [SerializeField] private AnimationCurve _flightCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Vertical offset over flight progress, multiplied by arc height. Use zero at both ends; positive values bend upward.")]
        [SerializeField] private AnimationCurve _arcCurve = new(new Keyframe(0f, 0f, 4f, 4f), new Keyframe(1f, 0f, -4f, -4f));
        [Tooltip("Arc offset in the text parent's local UI units. Positive bends upward; negative bends downward; zero gives a straight path.")]
        [SerializeField] private float _arcHeight = 150f;
        [Tooltip("Scale multiplier over normalized flight time: start at 1 and finish at 0. Nonnegative values control how the text shrinks.")]
        [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        private Sequence _sequence;
        private Vector3 _restScale;
        private Vector3 _restPosition;
        private Vector3 _flightStartPosition;

        /// <summary>Caches the authored scale and keeps the notification transparent until supplied with text.</summary>
        private void Awake()
        {
            _restScale = _text.rectTransform.localScale;
            _restPosition = _text.rectTransform.localPosition;
            _text.raycastTarget = false;
            _text.alpha = 0f;
            _text.text = string.Empty;
        }

        /// <summary>Animates entrance, delay and shrinking arc flight using unscaled time; completes after arrival.</summary>
        public void Show(string text, Action onAppeared, Action onCompleted)
        {
            ValidateSettings();
            Stop();
            _text.text = text;
            _text.rectTransform.localScale = _restScale * _startScale;
            _sequence = DOTween.Sequence().SetUpdate(true);
            _sequence.Append(_text.DOFade(1f, _appearDuration).SetEase(Ease.InOutQuad));
            _sequence.Join(_text.rectTransform.DOScale(_restScale, _appearDuration).SetEase(Ease.OutBack, _bounceStrength));
            _sequence.AppendCallback(() => onAppeared?.Invoke());
            _sequence.AppendInterval(_flightDelay);
            _sequence.AppendCallback(() => _flightStartPosition = _text.rectTransform.localPosition);
            _sequence.Append(DOVirtual.Float(0f, 1f, _flightDuration, UpdateFlight).SetEase(Ease.Linear));
            _sequence.OnComplete(() =>
            {
                _sequence = null;
                _text.alpha = 0f;
                _text.text = string.Empty;
                _text.rectTransform.localScale = _restScale;
                _text.rectTransform.localPosition = _restPosition;
                onCompleted?.Invoke();
            });
            if (!isActiveAndEnabled)
                _sequence.Pause();
        }

        /// <summary>Moves the text toward the current icon center in parent space and shrinks it to zero on arrival.</summary>
        private void UpdateFlight(float time)
        {
            var rect = _text.rectTransform;
            var targetRect = _flightTarget.rectTransform;
            var targetPosition = rect.parent.InverseTransformPoint(targetRect.TransformPoint(targetRect.rect.center));
            var progress = _flightCurve.Evaluate(time);
            var position = Vector3.LerpUnclamped(_flightStartPosition, targetPosition, progress);
            position.y += _arcCurve.Evaluate(progress) * _arcHeight;
            rect.localPosition = time >= 1f ? targetPosition : position;
            rect.localScale = time >= 1f ? Vector3.zero : _restScale * Mathf.Max(0f, _scaleCurve.Evaluate(time));
        }

        /// <summary>Rejects invalid authored settings rather than silently replacing them at runtime.</summary>
        private void ValidateSettings()
        {
            if (!float.IsFinite(_appearDuration) || _appearDuration <= 0f ||
                _flightTarget == null ||
                !float.IsFinite(_flightDelay) || _flightDelay < 0f ||
                !float.IsFinite(_flightDuration) || _flightDuration <= 0f ||
                !float.IsFinite(_arcHeight) ||
                _flightCurve == null || _flightCurve.length < 2 ||
                _arcCurve == null || _arcCurve.length < 2 ||
                _scaleCurve == null || _scaleCurve.length < 2 ||
                !float.IsFinite(_startScale) || _startScale <= 0f || _startScale > 1f ||
                !float.IsFinite(_bounceStrength) || _bounceStrength < 0f)
                throw new InvalidOperationException("Invalid text notification animation settings.");
        }

        /// <summary>Cancels callbacks and clears presentation without completing a queued entry.</summary>
        public void Stop()
        {
            _sequence?.Kill();
            _sequence = null;
            _text.alpha = 0f;
            _text.text = string.Empty;
            _text.rectTransform.localScale = _restScale;
            _text.rectTransform.localPosition = _restPosition;
        }

        /// <summary>Resumes an interrupted presentation without replaying its appearance action.</summary>
        private void OnEnable()
        {
            _sequence?.Play();
        }

        /// <summary>Suspends progress while the HUD cannot display the notification.</summary>
        private void OnDisable()
        {
            _sequence?.Pause();
        }

        /// <summary>Releases the tween and its callbacks with the HUD lifetime.</summary>
        private void OnDestroy()
        {
            _sequence?.Kill();
            _sequence = null;
        }
    }
}
