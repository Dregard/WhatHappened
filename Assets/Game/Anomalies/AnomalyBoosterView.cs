using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Anomalies
{
    public sealed class AnomalyBoosterView : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>Alpha floor for the blocking backdrop: a fully transparent mesh would be culled and stop catching taps.</summary>
        private const float BackdropMinAlpha = 0.01f;

        [SerializeField] private Image _ring;
        [SerializeField] private Image _finger;
        [SerializeField] private Image _photo;
        [SerializeField] private Image _photoOriginal;
        [SerializeField] private AspectRatioFitter _photoOriginalFitter;
        [SerializeField] private RectTransform[] _photoLabels;
        [SerializeField] private RectTransform[] _photoDecor;
        [SerializeField] private TMP_Text _countdown;
        [SerializeField] private TMP_Text _backText;

        private readonly List<Graphic> _labelGraphics = new();
        private AnomalyBoosterController _controller;
        private AnomalySearchSystem _search;
        private AnomalyPresentationData _presentation;
        private GameObject _polaroid;
        private RectTransform _flight;
        private Coroutine _photoRoutine;
        private AnomalyPhotoPhase _shownPhase;
        private Vector2 _photoOffset;
        private Vector2 _startCenter;
        private Vector2 _endCenter;
        private float _endScale;
        private float _startAngle;
        private float _labelsAlpha;
        private bool _isPolaroidHidden;
        private float _pulseTime;

        /// <summary>Connects the prefab's board-relative finger and fullscreen raycast-blocking polaroid of the original to the booster state; the photo flies together with its frame to and from the thumbnail polaroid.</summary>
        public void Initialize(AnomalyBoosterController controller, AnomalySearchSystem search, RectTransform picture, Sprite original,
            GameObject polaroid, AnomalyPresentationData presentation)
        {
            _controller = controller;
            _search = search;
            _polaroid = polaroid;
            _presentation = presentation;
            _photoOriginal.sprite = original;
            AnomalyUi.SetAspect(_photoOriginalFitter, original);
            // The flying HUD frame replaces the old standalone card: its art is hidden, and only the original's fitted rectangle defines the viewing size.
            _photoOriginal.enabled = false;
            foreach (var part in _photoDecor)
                part.gameObject.SetActive(false);
            CollectGraphics(_photoLabels, _labelGraphics);
            RaiseAboveBoard(_ring.gameObject, picture);
            RaiseAboveBoard(_finger.gameObject, picture);
            _controller.Changed += Refresh;
            Refresh();
        }

        /// <summary>Caches the graphics whose alpha the photo animation drives so no search runs per frame.</summary>
        private static void CollectGraphics(RectTransform[] roots, List<Graphic> graphics)
        {
            foreach (var root in roots)
                graphics.AddRange(root.GetComponentsInChildren<Graphic>(true));
        }

        /// <summary>Accepts a tap for early closing only while the photo is fully open and counting; flight taps are swallowed by the backdrop.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            _controller?.TryClosePhoto();
        }

        /// <summary>Keeps a board-anchored hint graphic drawn at the HUD level so anomalies never cover it.</summary>
        private static void RaiseAboveBoard(GameObject target, RectTransform picture)
        {
            var boardCanvas = picture.GetComponentInParent<Canvas>();
            var canvas = target.GetComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingLayerID = boardCanvas.sortingLayerID;
            canvas.sortingOrder = boardCanvas.sortingOrder + 1;
        }

        /// <summary>Drives the attempt-owned clocks and animates the finger without per-frame searches or allocations.</summary>
        private void Update()
        {
            if (_controller == null)
                return;
            _controller.Tick(Time.deltaTime);
            if (_controller.PhotoPhase == AnomalyPhotoPhase.Showing)
            {
                var seconds = Mathf.CeilToInt(_controller.PhotoRemaining);
                _countdown.text = seconds.ToString();
                _backText.text = $"Back to the scene in {seconds} sec";
            }
            _finger.enabled = _controller.HintTarget != null && !_controller.IsPhotoOpen && !_search.IsPaused;
            _ring.enabled = _finger.enabled && _ring.sprite != null;
            if (!_finger.enabled)
                return;
            var data = _controller.HintData;
            _pulseTime += Time.deltaTime;
            var scale = 1 - Mathf.PingPong(_pulseTime / data.PulseDuration * 2, 1) * data.PulseScale;
            _finger.rectTransform.localScale = Vector3.one * scale;
            _ring.rectTransform.localScale = Vector3.one * (2 - scale);
        }

        /// <summary>Maps supplied state to visuals; the sprite fingertip stays on the target at any board scale or aspect ratio.</summary>
        private void Refresh()
        {
            RefreshPhoto();
            _finger.enabled = _controller.HintTarget != null && !_controller.IsPhotoOpen && !_search.IsPaused;
            _ring.enabled = _finger.enabled && _ring.sprite != null;
            if (_controller.HintTarget == null)
                return;
            var ringRect = _ring.rectTransform;
            ringRect.anchorMin = ringRect.anchorMax = _controller.HintTarget.Bounds.center;
            ringRect.sizeDelta = Vector2.one * 150f;
            ringRect.anchoredPosition = Vector2.zero;
            var data = _controller.HintData;
            var rect = _finger.rectTransform;
            rect.anchorMin = rect.anchorMax = _controller.HintTarget.Bounds.center;
            rect.pivot = data.Fingertip;
            rect.sizeDelta = data.FingerSize;
            rect.anchoredPosition = Vector2.zero;
            _finger.sprite = data.FingerSprite;
            _pulseTime = 0;
            _finger.transform.SetAsLastSibling();
        }

        /// <summary>Starts the flight for a newly entered phase; the controller keeps the board blocked and the clock suspended until it ends.</summary>
        private void RefreshPhoto()
        {
            var phase = _controller.PhotoPhase;
            if (phase == _shownPhase)
                return;
            _shownPhase = phase;
            switch (phase)
            {
                case AnomalyPhotoPhase.Opening:
                    PreparePhoto();
                    _photoRoutine = StartCoroutine(OpenPhoto());
                    break;
                case AnomalyPhotoPhase.Closing:
                    StopPhotoRoutine();
                    _photoRoutine = StartCoroutine(ClosePhoto());
                    break;
                case AnomalyPhotoPhase.None:
                    HidePhoto();
                    break;
            }
        }

        /// <summary>Shows the backdrop and measures the thumbnail and final photo in the photo space so both flights are continuous.</summary>
        private void PreparePhoto()
        {
            StopPhotoRoutine();
            _photo.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            var space = _photo.rectTransform;
            var source = (RectTransform)_polaroid.transform;
            var original = _photoOriginal.rectTransform;
            _flight = (RectTransform)Instantiate(_polaroid, space, false).transform;
            _flight.name = "BoosterPhotoFlight";
            _flight.SetAsFirstSibling();
            var center = (Vector2)space.InverseTransformPoint(source.TransformPoint(source.rect.center));
            var size = new Vector2(
                space.InverseTransformVector(source.TransformVector(Vector3.right * source.rect.width)).magnitude,
                space.InverseTransformVector(source.TransformVector(Vector3.up * source.rect.height)).magnitude);
            AnomalyUi.Place(_flight, new Rect(center - size * 0.5f, size));
            _flight.localRotation = Quaternion.Inverse(space.rotation) * source.rotation;
            _flight.localScale = Vector3.one;
            _flight.gameObject.SetActive(true);
            foreach (var graphic in _flight.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            Canvas.ForceUpdateCanvases();
            var photo = _flight.GetComponentInChildren<AspectRatioFitter>(true).GetComponent<RectTransform>();
            var photoCenter = photo.TransformPoint(photo.rect.center);
            _photoOffset = _flight.InverseTransformPoint(photoCenter);
            _startCenter = space.InverseTransformPoint(photoCenter);
            _endCenter = space.InverseTransformPoint(original.TransformPoint(original.rect.center));
            var startWidth = space.InverseTransformVector(photo.TransformVector(Vector3.right * photo.rect.width)).magnitude;
            var endWidth = space.InverseTransformVector(original.TransformVector(Vector3.right * original.rect.width)).magnitude;
            _endScale = startWidth > 0 ? endWidth / startWidth : 1;
            _startAngle = Mathf.DeltaAngle(0, _flight.localEulerAngles.z);
            _polaroid.SetActive(false);
            _isPolaroidHidden = true;
            ApplyFlight(0);
            SetLabelsAlpha(0);
        }

        /// <summary>Flies the photo from the thumbnail to the center, then fades in the captions while the countdown runs.</summary>
        private IEnumerator OpenPhoto()
        {
            yield return Animate(_presentation.BoosterPhotoFlightDuration, progress => ApplyFlight(Mathf.SmoothStep(0, 1, progress)));
            _controller.BeginPhotoShowing();
            yield return Animate(_presentation.BoosterPhotoLabelsFadeDuration, SetLabelsAlpha);
        }

        /// <summary>Returns the photo exactly into the thumbnail while the captions fade out, then releases the board and clock.</summary>
        private IEnumerator ClosePhoto()
        {
            var startAlpha = _labelsAlpha;
            var flight = _presentation.BoosterPhotoFlightDuration;
            var fade = _presentation.BoosterPhotoLabelsFadeDuration;
            yield return Animate(flight, progress =>
            {
                ApplyFlight(1 - Mathf.SmoothStep(0, 1, progress));
                var fadeProgress = fade > 0 ? Mathf.Clamp01(progress * flight / fade) : Mathf.Ceil(progress);
                SetLabelsAlpha(startAlpha * (1 - fadeProgress));
            });
            _controller.CompletePhotoClose();
        }

        /// <summary>Moves the HUD frame clone with its photo between the thumbnail (0) and the viewing place (1), keeping the photo center on the path, and fades the backdrop in.</summary>
        private void ApplyFlight(float fraction)
        {
            var scale = Mathf.Lerp(1, _endScale, fraction);
            var rotation = Quaternion.Euler(0, 0, Mathf.Lerp(_startAngle, 0, fraction));
            var center = Vector2.Lerp(_startCenter, _endCenter, fraction);
            var position = center - (Vector2)(rotation * (Vector3)(_photoOffset * scale));
            _flight.localPosition = new Vector3(position.x, position.y, _flight.localPosition.z);
            _flight.localRotation = rotation;
            _flight.localScale = new Vector3(scale, scale, 1);
            _photo.canvasRenderer.SetAlpha(Mathf.Max(fraction, BackdropMinAlpha));
        }

        /// <summary>Applies one alpha to every caption graphic without touching their authored colors.</summary>
        private void SetLabelsAlpha(float alpha)
        {
            _labelsAlpha = alpha;
            SetAlpha(_labelGraphics, alpha);
        }

        /// <summary>Multiplies the authored alpha of cached graphics through their canvas renderers.</summary>
        private static void SetAlpha(List<Graphic> graphics, float alpha)
        {
            foreach (var graphic in graphics)
                graphic.canvasRenderer.SetAlpha(alpha);
        }

        /// <summary>Runs a phase in active time: external pause freezes the animation exactly where it stands.</summary>
        private IEnumerator Animate(float duration, Action<float> frame)
        {
            var elapsed = 0f;
            frame(0);
            while (elapsed < duration)
            {
                yield return null;
                if (_search.IsPaused)
                    continue;
                elapsed += Time.deltaTime;
                frame(Mathf.Clamp01(elapsed / duration));
            }
            frame(1);
        }

        /// <summary>Stops the running flight, if any.</summary>
        private void StopPhotoRoutine()
        {
            if (_photoRoutine == null)
                return;
            StopCoroutine(_photoRoutine);
            _photoRoutine = null;
        }

        /// <summary>Removes every photo overlay and returns the thumbnail and photo transforms to their authored state.</summary>
        private void HidePhoto()
        {
            StopPhotoRoutine();
            if (_flight != null)
                Destroy(_flight.gameObject);
            _flight = null;
            _photo.canvasRenderer.SetAlpha(1);
            SetAlpha(_labelGraphics, 1);
            _photo.gameObject.SetActive(false);
            if (!_isPolaroidHidden)
                return;
            if (_polaroid != null)
                _polaroid.SetActive(true);
            _isPolaroidHidden = false;
        }

        /// <summary>Detaches presentation and synchronously ends the old attempt's effects when its hierarchy is disabled.</summary>
        private void OnDisable()
        {
            if (_controller == null)
                return;
            _controller.Dispose();
            _controller.Changed -= Refresh;
        }
    }
}
