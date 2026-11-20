using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Anomalies
{
    /// <summary>Search HUD of the AnomalySearch prefab: board, original thumbnail, task/narrative plates, slots and the start prompt. Code fills it with level data and builds only the per-stage images and slots from prefab templates.</summary>
    public sealed class AnomalySearchView : MonoBehaviour, IPointerClickHandler
    {
        public event Action Ready;
        public event Action<string> SfxRequested;
        /// <summary>Raised when the HUD photo is tapped; the economy decides whether the comparison booster may be spent.</summary>
        public event Action PhotoRequested;

        [SerializeField] private Canvas _boardCanvas;
        [SerializeField] private Image _board;
        [SerializeField] private AspectRatioFitter _boardFitter;
        [SerializeField] private AnomalyBoardFrame _boardFrame;
        [SerializeField] private RectTransform _stageRoot;
        [SerializeField] private RectTransform _slotsRoot;
        [SerializeField] private GameObject _polaroid;
        [SerializeField] private Image _thumbnail;
        [SerializeField] private AspectRatioFitter _thumbnailFitter;
        [SerializeField] private TMP_Text _status;
        [SerializeField] private GameObject _taskPlate;
        [SerializeField] private TMP_Text _taskLabel;
        [SerializeField] private GameObject _narrativePlate;
        [SerializeField] private TMP_Text _narrativeLabel;
        [SerializeField] private GameObject _originalTape;
        [SerializeField] private Button _readyButton; // Root of the project Button prefab; this assembly cannot reference its GameButton, which adds click SFX and signals on the same object.
        [SerializeField] private AnomalyFindingEffects _effects;
        [SerializeField] private Image _veil;
        [SerializeField] private AnomalyBoosterView _boosterView;
        [SerializeField] private Image _stageImageTemplate;
        [SerializeField] private Image _slotTemplate;

        /// <summary>Sprite pixels by which the photo extends beneath the frame body past the slice border.</summary>
        private const float FrameBleedPixels = 14f;

        public AnomalyBoosterController Boosters { get; private set; }
        private AnomalySearchSystem _search;
        private AnomalyLevelData _data;
        private RectTransform _picture;
        private Coroutine _narrativeRoutine;
        private readonly Dictionary<string, Image> _imagesById = new();
        private readonly Dictionary<string, Image> _slotsById = new();
        private int _displayedStage = -1;
        private int _transitionStage = -1;
        private bool _isPhotographing;
        private bool _isTransitioning;
        private AnomalyMissLimiter _missLimiter;
        private GameObject _lockOverlay;

        /// <summary>Instantiates the prefab on the HUD canvas: HUD elements share its level, the board sits just beneath it.</summary>
        public static AnomalySearchView Create(Canvas canvas, AnomalySearchView prefab, AnomalyLevelData data, AnomalySearchSystem search, Action<GameObject> inject = null)
        {
            var view = Instantiate(prefab, canvas.transform, false);
            view.name = "AnomalySearch";
            inject?.Invoke(view.gameObject);
            view.Initialize(canvas, data, search);
            return view;
        }

        /// <summary>Fills the prefab's board and thumbnail with the level and starts listening to the search; tapping the start prompt begins the photo sequence.</summary>
        private void Initialize(Canvas hudCanvas, AnomalyLevelData data, AnomalySearchSystem search)
        {
            _data = data;
            _search = search;
            _boardCanvas.overrideSorting = true;
            _boardCanvas.sortingLayerID = hudCanvas.sortingLayerID;
            _boardCanvas.sortingOrder = hudCanvas.sortingOrder - 1;
            _board.sprite = data.PhotoImage;
            AnomalyUi.SetAspect(_boardFitter, data.PhotoImage);
            _boardFrame.Fit();
            _picture = _board.rectTransform;
            _thumbnail.sprite = data.PhotoImage;
            AnomalyUi.SetAspect(_thumbnailFitter, data.PhotoImage);
            _thumbnail.raycastTarget = true;
            _readyButton.onClick.AddListener(OnReady);
            Boosters = new AnomalyBoosterController(search);
            _boosterView.Initialize(Boosters, search, _picture, data.PhotoImage, _polaroid, data.Presentation);
            _effects.Initialize(search, data);
            var tuning = data.Presentation;
            _missLimiter = new AnomalyMissLimiter(tuning.MissLockCount, tuning.MissLockWindow, tuning.MissLockDuration);
            _search.Changed += Refresh;
            _search.Found += PresentFinding;
            Refresh();
        }

        /// <summary>Disables duplicate Ready taps; the gameplay clock starts only after photo presentation finishes.</summary>
        private void OnReady()
        {
            if (_search.IsPaused || _isPhotographing || _search.IsStarted)
                return;
            _isPhotographing = true;
            _readyButton.gameObject.SetActive(false);
            _originalTape.SetActive(false);
            StartCoroutine(Photograph());
        }

        /// <summary>Flashes the shutter and flies the original, already inside its frame, from the board into the upper-right comparison thumbnail.</summary>
        private IEnumerator Photograph()
        {
            RequestSfx(_data.Presentation.PhotoSfx);
            var root = (RectTransform)_effects.transform;
            Canvas.ForceUpdateCanvases();
            FitFrameToPhoto();
            var flight = CreateFramedFlight(root, out var photoOffset, out var photoSize);
            // The flying frame covers the board, so the field and its anomaly layers are prepared unseen and are in place the moment the frame leaves.
            _board.sprite = _data.MainImage;
            if (_search.Stages.Count > 0)
                BuildStage(0, _search.Stages[0]);
            Canvas.ForceUpdateCanvases();
            var source = AnomalyUi.LocalRect(_picture, root);
            var endAngle = Mathf.DeltaAngle(0, flight.localEulerAngles.z);
            var endCenter = (Vector2)flight.localPosition + (Vector2)(Quaternion.Euler(0, 0, endAngle) * (Vector3)photoOffset);
            var startScale = source.width / photoSize.x;
            yield return _effects.Animate(_data.Presentation.PhotoDuration, progress =>
            {
                var fraction = Mathf.SmoothStep(0, 1, progress);
                var scale = Mathf.Lerp(startScale, 1, fraction);
                var angle = Mathf.Lerp(0, endAngle, fraction);
                var photoCenter = Vector2.Lerp(source.center, endCenter, fraction);
                var frameCenter = photoCenter - (Vector2)(Quaternion.Euler(0, 0, angle) * (Vector3)(photoOffset * scale));
                flight.localPosition = frameCenter;
                flight.localRotation = Quaternion.Euler(0, 0, angle);
                flight.localScale = new Vector3(scale, scale, 1);
                _veil.color = new Color(1, 1, 1, Mathf.Clamp01(1 - progress * 3));
            });
            _thumbnail.enabled = true;
            _polaroid.SetActive(true);
            Destroy(flight.gameObject);
            _veil.color = Color.clear;
            _isPhotographing = false;
            Ready?.Invoke();
            Refresh();
        }

        /// <summary>Resizes the frame within its authored slot so its sliced borders keep their proportions and the window has exactly the photo's aspect (no gaps), and moves the frame graphic above the photo so it hides the photo's corners.</summary>
        private void FitFrameToPhoto()
        {
            var root = (RectTransform)_polaroid.transform;
            var sprite = _polaroid.GetComponent<Image>().sprite;
            var border = sprite.border;
            var slot = root.rect.size;
            var unit = Mathf.Min(slot.x / sprite.rect.width, slot.y / sprite.rect.height);
            var borders = new Vector2(border.x + border.z, border.y + border.w) * unit;
            var window = slot - borders;
            var aspect = _data.PhotoImage.rect.width / _data.PhotoImage.rect.height;
            var photo = window.x / window.y > aspect ? new Vector2(window.y * aspect, window.y) : new Vector2(window.x, window.x / aspect);
            var frame = photo + borders;
            var inset = (slot - frame) * 0.5f;
            root.offsetMin = inset;
            root.offsetMax = -inset;
            var area = (RectTransform)_thumbnailFitter.transform.parent;
            area.anchorMin = new Vector2(border.x * unit / frame.x, border.y * unit / frame.y);
            area.anchorMax = new Vector2(1 - border.z * unit / frame.x, 1 - border.w * unit / frame.y);
            // The sprite's transparent window is wider than its slice border, so the photo bleeds uniformly under the opaque frame body to leave no gap.
            var bleed = FrameBleedPixels * unit;
            var grow = Mathf.Max((photo.x + 2 * bleed) / photo.x, (photo.y + 2 * bleed) / photo.y) - 1f;
            area.offsetMin = -photo * grow * 0.5f;
            area.offsetMax = photo * grow * 0.5f;
            var label = _polaroid.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                // The caption stays inside the bottom strip, whose thickness is fixed by the border.
                var caption = (RectTransform)label.transform.parent;
                var strip = border.y * unit / frame.y;
                caption.anchorMin = new Vector2(caption.anchorMin.x, strip * 0.3f);
                caption.anchorMax = new Vector2(caption.anchorMax.x, strip * 0.86f);
                caption.offsetMin = caption.offsetMax = Vector2.zero;
            }
            var overlay = AnomalyUi.Image("Frame", root, sprite);
            overlay.type = Image.Type.Sliced;
            overlay.fillCenter = false;
            overlay.pixelsPerUnitMultiplier = 1f / (overlay.pixelsPerUnit * unit);
            overlay.transform.SetSiblingIndex(1);
            _polaroid.GetComponent<Image>().enabled = false;
        }

        /// <summary>Clones the final frame with the photo inside and places it exactly at the final frame's position, size and angle; returns the photo's centre offset and fitted size in the frame's own space.</summary>
        private RectTransform CreateFramedFlight(RectTransform root, out Vector2 photoOffset, out Vector2 photoSize)
        {
            var target = (RectTransform)_polaroid.transform;
            var flight = (RectTransform)Instantiate(_polaroid, root, false).transform;
            flight.name = "PhotoFrameFlight";
            var center = (Vector2)root.InverseTransformPoint(target.TransformPoint(target.rect.center));
            var size = new Vector2(
                root.InverseTransformVector(target.TransformVector(Vector3.right * target.rect.width)).magnitude,
                root.InverseTransformVector(target.TransformVector(Vector3.up * target.rect.height)).magnitude);
            AnomalyUi.Place(flight, new Rect(center - size * 0.5f, size));
            flight.localRotation = Quaternion.Inverse(root.rotation) * target.rotation;
            flight.gameObject.SetActive(true);
            foreach (var graphic in flight.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            flight.GetComponentInChildren<AspectRatioFitter>(true).GetComponent<Image>().enabled = true;
            Canvas.ForceUpdateCanvases();
            var photo = flight.GetComponentInChildren<AspectRatioFitter>(true).GetComponent<RectTransform>();
            photoOffset = flight.InverseTransformPoint(photo.TransformPoint(photo.rect.center));
            photoSize = new Vector2(
                flight.InverseTransformVector(photo.TransformVector(Vector3.right * photo.rect.width)).magnitude,
                flight.InverseTransformVector(photo.TransformVector(Vector3.up * photo.rect.height)).magnitude);
            return flight;
        }

        /// <summary>A tap on the HUD photo requests the comparison view once the search is running; otherwise only the fitted board's own raycast counts, excluding UI, letterboxing and presentation-locked taps.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            var hit = eventData.pointerCurrentRaycast.gameObject;
            if (hit != null && hit.transform.IsChildOf(_polaroid.transform))
            {
                if (_search.IsStarted && !_isPhotographing && !_search.IsPaused && !Boosters.IsPhotoOpen)
                    PhotoRequested?.Invoke();
                return;
            }
            if (eventData.pointerCurrentRaycast.gameObject != _board.gameObject || !_search.IsStarted ||
                _search.IsPaused || Boosters.IsPhotoOpen || _search.IsCompleted || _search.IsTransitionPending || _isTransitioning || _isPhotographing)
                return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_picture, eventData.position, eventData.pressEventCamera, out var local))
                return;
            if (_missLimiter.IsLocked(Time.unscaledTime))
                return;
            var rect = _picture.rect;
            if (!rect.Contains(local) || rect.width <= 0 || rect.height <= 0)
                return;
            var point = (local - rect.min) / rect.size;
            if (_search.TryFind(point))
                return;
            foreach (var anomaly in _search.CurrentAnomalies)
                if (anomaly.Bounds.Contains(point))
                    return;
            _effects.Miss(_picture, local);
            if (_missLimiter.RegisterMiss(Time.unscaledTime))
                ShowLock();
        }

        /// <summary>Dims the picture and shows the warning; both are passive children of the effects root.</summary>
        private void ShowLock()
        {
            HideLock();
            var root = (RectTransform)_effects.transform;
            var dim = AnomalyUi.Image("MissLock", root);
            dim.color = new Color(0, 0, 0, _data.Presentation.MissLockDim);
            AnomalyUi.Place(dim.rectTransform, AnomalyUi.LocalRect(_picture, root));
            var text = AnomalyUi.Text("Warning", dim.transform, new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.6f));
            UiKit.Style(UiThemeData.LoadOrDefault(), text, UiKit.TextStyleAlias.Light);
            text.text = _data.Presentation.MissLockWarning;
            _lockOverlay = dim.gameObject;
        }

        /// <summary>Removes the dimming and warning.</summary>
        private void HideLock()
        {
            if (_lockOverlay != null)
                Destroy(_lockOverlay);
            _lockOverlay = null;
        }

        /// <summary>Ends the lock by real time, with a clean counter.</summary>
        private void Update()
        {
            if (_lockOverlay != null && !_missLimiter.IsLocked(Time.unscaledTime))
            {
                _missLimiter.Reset();
                HideLock();
            }
        }

        /// <summary>Reflects model state; visual replacement is separately staged under full blackout.</summary>
        private void Refresh()
        {
            _readyButton.gameObject.SetActive(!_search.IsStarted && !_isPhotographing);
            _originalTape.SetActive(!_search.IsStarted && !_isPhotographing);
            if (_displayedStage != _search.StageIndex && !_isTransitioning && !_isPhotographing)
                BuildStage(_search.StageIndex, _search.CurrentAnomalies);
            if (_search.Stages.Count > 0)
                _status.text = $"STAGE {Mathf.Clamp(_search.StageIndex + 1, 1, _search.Stages.Count)}/{_search.Stages.Count}";
            RefreshTask();
            if (_search.IsTransitionPending && _transitionStage != _search.StageIndex && !_search.IsCompleted && !_isTransitioning)
            {
                _transitionStage = _search.StageIndex;
                StartCoroutine(PresentTransition(_transitionStage));
            }
        }

        /// <summary>Writes the remaining-changes line, or the level intro before the investigation begins.</summary>
        private void RefreshTask()
        {
            var total = _search.CurrentAnomalies.Count;
            var found = 0;
            foreach (var anomaly in _search.CurrentAnomalies)
                if (_search.IsFound(anomaly.Id))
                    found++;
            var remaining = total - found;
            var isIntro = !_search.IsStarted && !string.IsNullOrEmpty(_data.IntroText);
            _taskLabel.text = found == 0
                ? $"Find {total} {(total == 1 ? "change" : "changes")}!"
                : $"{remaining} {(remaining == 1 ? "change" : "changes")} left!";
            if (isIntro)
                ShowNarrative(_data.IntroText, false);
            else if (_narrativeRoutine == null)
                _narrativePlate.SetActive(false);
            _taskPlate.SetActive(!_narrativePlate.activeSelf);
        }

        /// <summary>Shows a paper-plate narrative in place of the task plate; timed texts return to the task plate afterwards.</summary>
        private void ShowNarrative(string text, bool isTimed)
        {
            if (_narrativeRoutine != null)
                StopCoroutine(_narrativeRoutine);
            _narrativeRoutine = null;
            _narrativeLabel.text = text;
            _narrativePlate.SetActive(!string.IsNullOrEmpty(text));
            _taskPlate.SetActive(!_narrativePlate.activeSelf);
            if (isTimed && _narrativePlate.activeSelf)
                _narrativeRoutine = StartCoroutine(HideNarrativeLater());
        }

        /// <summary>Returns to the task plate after the configured narrative time, paused with the search.</summary>
        private IEnumerator HideNarrativeLater()
        {
            yield return _effects.Animate(_data.Presentation.NarrativeDuration, _ => { });
            _narrativeRoutine = null;
            _narrativePlate.SetActive(false);
            _taskPlate.SetActive(true);
            RefreshTask();
        }

        /// <summary>Creates exactly one closed slot per actual stage anomaly from the prefab templates; called only initially or under opaque black.</summary>
        private void BuildStage(int index, IReadOnlyList<AnomalyData> anomalies)
        {
            _displayedStage = index;
            foreach (Transform child in _stageRoot)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            foreach (Transform child in _slotsRoot)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            _imagesById.Clear();
            _slotsById.Clear();
            ShowNarrative(string.Empty, false);
            const float slotWidth = 115f;
            const float slotGap = 25f;
            var rowWidth = anomalies.Count * slotWidth + Mathf.Max(0, anomalies.Count - 1) * slotGap;
            for (var i = 0; i < anomalies.Count; i++)
            {
                var anomaly = anomalies[i];
                var image = Instantiate(_stageImageTemplate, _stageRoot, false);
                image.name = anomaly.Id;
                image.sprite = anomaly.Image;
                AnomalyUi.Bounds(image.rectTransform, anomaly.Bounds.min, anomaly.Bounds.max);
                _imagesById.Add(anomaly.Id, image);
                var slot = Instantiate(_slotTemplate, _slotsRoot, false);
                slot.name = "Closed_" + anomaly.Id;
                var left = (880f - rowWidth) * 0.5f + i * (slotWidth + slotGap);
                UiKit.PlaceIn(slot.rectTransform, 880f, 115f, left, 0, left + slotWidth, 115f);
                _slotsById.Add(anomaly.Id, slot);
            }
        }

        /// <summary>Starts independent visuals for an accepted finding; text and SFX appear immediately.</summary>
        private void PresentFinding(AnomalyData anomaly)
        {
            ShowNarrative(anomaly.HudText, true);
            RequestSfx(_data.Presentation.FindingSfx);
            StartCoroutine(_effects.Finding(anomaly, _imagesById[anomaly.Id], _slotsById[anomaly.Id],
                () => _search.CompleteFinding(anomaly.Id)));
        }

        /// <summary>Flickers, replaces visuals at full black, fades in, then releases model input and final completion.</summary>
        private IEnumerator PresentTransition(int stage)
        {
            _isTransitioning = true;
            var count = Mathf.Clamp(_data.Presentation.FlickerCount, 0, 8);
            yield return _effects.Animate(_data.TransitionDuration, progress =>
            {
                var alpha = progress >= 1 ? 1 : Mathf.PingPong(progress * (count * 2 + 1), 1);
                _veil.color = new Color(0, 0, 0, alpha);
            });
            _veil.color = Color.black;
            yield return null;
            if (stage + 1 < _search.Stages.Count)
                BuildStage(stage + 1, _search.Stages[stage + 1]);
            yield return _effects.Animate(_data.Presentation.BlackoutDuration, _ => { });
            yield return _effects.Animate(_data.TransitionDuration, progress => _veil.color = new Color(0, 0, 0, 1 - progress));
            _search.CompleteTransition(stage);
            _isTransitioning = false;
            Refresh();
        }

        /// <summary>Delegates audio playback to the existing application audio owner.</summary>
        private void RequestSfx(string id)
        {
            if (!string.IsNullOrEmpty(id))
                SfxRequested?.Invoke(id);
        }

        /// <summary>Cancels unfinished effects before the level releases its images; callbacks cannot reach a later attempt.</summary>
        private void OnDisable()
        {
            Boosters?.Dispose();
            _missLimiter?.Reset();
            HideLock();
            StopAllCoroutines();
            _narrativeRoutine = null;
            if (_search != null)
            {
                _search.Changed -= Refresh;
                _search.Found -= PresentFinding;
            }
        }
    }
}
