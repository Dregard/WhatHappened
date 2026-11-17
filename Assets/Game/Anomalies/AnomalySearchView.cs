using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Anomalies
{
    public sealed class AnomalySearchView : MonoBehaviour, IPointerClickHandler
    {
        // Mockup tilt of the polaroid (clockwise) and of the ORIGINAL tape over the board, in degrees.
        private const float PolaroidTilt = 4.6f;
        private const float BoardTapeTilt = 3f;
        public event Action Ready;
        public event Action<string> SfxRequested;
        public AnomalyBoosterController Boosters { get; private set; }
        private AnomalySearchSystem _search;
        private AnomalyLevelData _data;
        private UiThemeData _theme;
        private RectTransform _picture;
        private RectTransform _stageRoot;
        private RectTransform _slotsRoot;
        private Image _thumbnail;
        private Image _veil;
        private Image _board;
        private Button _readyButton;
        private GameObject _originalTape;
        private GameObject _polaroid;
        private TMP_Text _status;
        private GameObject _taskPlate;
        private TMP_Text _taskLabel;
        private GameObject _narrativePlate;
        private TMP_Text _narrativeLabel;
        private Coroutine _narrativeRoutine;
        private AnomalyFindingEffects _effects;
        private readonly Dictionary<string, Image> _imagesById = new();
        private readonly Dictionary<string, Image> _slotsById = new();
        private int _displayedStage = -1;
        private int _transitionStage = -1;
        private bool _isPhotographing;
        private bool _isTransitioning;

        /// <summary>Creates the owned presentation on the HUD canvas: HUD elements share its level, the board sits just beneath it.</summary>
        public static AnomalySearchView Create(Canvas canvas, AnomalyLevelData data, AnomalySearchSystem search)
        {
            var root = new GameObject("AnomalySearch", typeof(RectTransform));
            root.layer = canvas.gameObject.layer;
            root.transform.SetParent(canvas.transform, false);
            AnomalyUi.Bounds((RectTransform)root.transform, Vector2.zero, Vector2.one);
            var view = root.AddComponent<AnomalySearchView>();
            view.Initialize(canvas, data, search);
            return view;
        }

        /// <summary>Builds the themed board, original thumbnail, task/narrative plates and slots; tapping the start prompt begins the photo sequence.</summary>
        private void Initialize(Canvas hudCanvas, AnomalyLevelData data, AnomalySearchSystem search)
        {
            _data = data;
            _search = search;
            _theme = UiThemeData.LoadOrDefault();
            var boardRoot = new GameObject("Board", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            boardRoot.layer = gameObject.layer;
            boardRoot.transform.SetParent(transform, false);
            AnomalyUi.Bounds((RectTransform)boardRoot.transform, Vector2.zero, Vector2.one);
            var boardCanvas = boardRoot.GetComponent<Canvas>();
            boardCanvas.overrideSorting = true;
            boardCanvas.sortingLayerID = hudCanvas.sortingLayerID;
            boardCanvas.sortingOrder = hudCanvas.sortingOrder - 1;
            var background = UiKit.Image("Background", boardRoot.transform, _theme.Background);
            background.preserveAspect = false;
            background.color = _theme.Background != null ? Color.white : _theme.VeilColor;
            background.enabled = true;
            var frame = UiKit.Image("BoardFrame", boardRoot.transform, _theme.BoardFrame, true);
            UiKit.PlacePadded(frame.rectTransform, 25, 440, 1055, 1470, UiKit.FramePad);
            var area = AnomalyUi.Image("BoardArea", boardRoot.transform);
            area.color = Color.clear;
            UiKit.Place(area.rectTransform, 45, 460, 1035, 1450);
            _board = AnomalyUi.Image("Original", area.transform, data.MainImage);
            _board.raycastTarget = true;
            AnomalyUi.Fit(_board);
            _picture = _board.rectTransform;
            var stage = AnomalyUi.Image("Stage", _picture);
            stage.color = Color.clear;
            _stageRoot = stage.rectTransform;

            BuildThumbnail(data);
            BuildStatus();
            BuildPlates();
            var slots = AnomalyUi.Image("Slots", transform);
            slots.color = Color.clear;
            _slotsRoot = slots.rectTransform;
            UiKit.Place(_slotsRoot, 100, 1620, 980, 1735);
            BuildStartPrompt(boardRoot.transform);

            var effectRoot = AnomalyUi.Image("Effects", transform);
            effectRoot.color = Color.clear;
            _effects = effectRoot.gameObject.AddComponent<AnomalyFindingEffects>();
            _effects.Initialize(search, data);
            _veil = AnomalyUi.Image("ShutterAndBlackout", transform);
            _veil.color = Color.clear;
            Boosters = new AnomalyBoosterController(search);
            gameObject.AddComponent<AnomalyBoosterView>().Initialize(Boosters, search, _picture, data.MainImage);
            _search.Changed += Refresh;
            _search.Found += PresentFinding;
            Refresh();
        }

        /// <summary>Places the polaroid, its pin and ORIGINAL tape; the photo itself arrives in the thumbnail after Ready.</summary>
        private void BuildThumbnail(AnomalyLevelData data)
        {
            var polaroid = UiKit.Image("Polaroid", transform, _theme.Polaroid);
            UiKit.Place(polaroid.rectTransform, 724, 43, 1055, 421);
            polaroid.rectTransform.localRotation = Quaternion.Euler(0, 0, -PolaroidTilt);
            _polaroid = polaroid.gameObject;
            var thumbArea = AnomalyUi.Image("ThumbnailArea", polaroid.transform);
            thumbArea.color = Color.clear;
            AnomalyUi.Bounds(thumbArea.rectTransform, new Vector2(0.105f, 0.224f), new Vector2(0.894f, 0.922f));
            _thumbnail = AnomalyUi.Image("OriginalThumbnail", thumbArea.transform, data.MainImage);
            AnomalyUi.Fit(_thumbnail);
            _thumbnail.enabled = false;
            var tape = UiKit.Image("OriginalTape", polaroid.transform, _theme.TapeRed);
            AnomalyUi.Bounds(tape.rectTransform, new Vector2(0.2f, 0.07f), new Vector2(0.88f, 0.2f));
            UiKit.FillLabel(_theme, tape.transform, "ORIGINAL", UiKit.TextStyleAlias.Light, 32);
            var pin = UiKit.Image("Pin", polaroid.transform, _theme.PinRed);
            AnomalyUi.Bounds(pin.rectTransform, new Vector2(0.45f, 0.93f), new Vector2(0.65f, 1.08f));
            _polaroid.SetActive(false);
        }

        /// <summary>Creates the stage label beneath the chapter title plate.</summary>
        private void BuildStatus()
        {
            _status = UiKit.Label(_theme, "Status", transform, string.Empty, UiKit.TextStyleAlias.Light, 365, 125, 715, 175, 38);
        }

        /// <summary>Creates the task plate, the paper narrative plate and the ORIGINAL tape shown over the unphotographed board.</summary>
        private void BuildPlates()
        {
            var task = UiKit.Image("TaskPlate", transform, _theme.PlateTask, true);
            UiKit.Place(task.rectTransform, 280, 1505, 800, 1582);
            _taskPlate = task.gameObject;
            _taskLabel = UiKit.FillLabel(_theme, task.transform, string.Empty, UiKit.TextStyleAlias.Light, 56);
            var narrative = UiKit.Image("NarrativePlate", transform, _theme.PlateNarrative, true);
            UiKit.Place(narrative.rectTransform, 140, 1492, 940, 1595);
            _narrativePlate = narrative.gameObject;
            _narrativeLabel = UiKit.FillLabel(_theme, narrative.transform, string.Empty, UiKit.TextStyleAlias.Dark, 40);
            _narrativeLabel.margin = new Vector4(70, 0, 70, 0);
            _narrativePlate.SetActive(false);
            var tape = UiKit.Image("BoardTape", transform, _theme.TapeRed);
            UiKit.Place(tape.rectTransform, 420, 405, 660, 478);
            tape.rectTransform.localRotation = Quaternion.Euler(0, 0, BoardTapeTilt);
            UiKit.FillLabel(_theme, tape.transform, "ORIGINAL", UiKit.TextStyleAlias.Light, 40);
            _originalTape = tape.gameObject;
        }

        /// <summary>Creates the full-screen tap target with its prompt and pointing finger; it vanishes after the first tap.</summary>
        private void BuildStartPrompt(Transform boardRoot)
        {
            var ready = AnomalyUi.Image("Ready", transform);
            ready.color = Color.clear;
            ready.raycastTarget = true;
            AnomalyUi.Bounds(ready.rectTransform, Vector2.zero, Vector2.one);
            _readyButton = ready.gameObject.AddComponent<Button>();
            _readyButton.transition = Selectable.Transition.None;
            _readyButton.onClick.AddListener(OnReady);
            UiKit.Label(_theme, "Prompt", ready.transform, "Tap to begin the investigation", UiKit.TextStyleAlias.Light, 200, 1630, 880, 1710, 44);
            var finger = UiKit.Image("Finger", ready.transform, _theme.FingerIcon);
            UiKit.Place(finger.rectTransform, 870, 1650, 1030, 1790);
        }

        /// <summary>Disables duplicate Ready taps; the gameplay clock starts only after photo presentation finishes.</summary>
        private void OnReady()
        {
            if (_search.IsPaused || _isPhotographing || _search.IsStarted)
                return;
            _isPhotographing = true;
            _readyButton.gameObject.SetActive(false);
            _originalTape.SetActive(false);
            _polaroid.SetActive(true);
            StartCoroutine(Photograph());
        }

        /// <summary>Flashes the shutter and moves the original into the upper-right comparison thumbnail.</summary>
        private IEnumerator Photograph()
        {
            RequestSfx(_data.Presentation.PhotoSfx);
            var photo = AnomalyUi.Image("PhotoFlight", _effects.transform, _data.MainImage);
            Canvas.ForceUpdateCanvases();
            yield return _effects.Animate(_data.Presentation.PhotoDuration, progress =>
            {
                var root = (RectTransform)_effects.transform;
                var source = AnomalyUi.LocalRect(_picture, root);
                var target = AnomalyUi.LocalRect(_thumbnail.rectTransform, root);
                var fraction = Mathf.SmoothStep(0, 1, progress);
                var size = Vector2.Lerp(source.size, target.size, fraction);
                AnomalyUi.Place(photo.rectTransform, new Rect(Vector2.Lerp(source.center, target.center, fraction) - size * 0.5f, size));
                _veil.color = new Color(1, 1, 1, Mathf.Clamp01(1 - progress * 3));
            });
            _thumbnail.enabled = true;
            Destroy(photo.gameObject);
            _veil.color = Color.clear;
            _isPhotographing = false;
            Ready?.Invoke();
            Refresh();
        }

        /// <summary>Accepts only the fitted board's own raycast, excluding UI, letterboxing and presentation-locked taps.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.pointerCurrentRaycast.gameObject != _board.gameObject || !_search.IsStarted ||
                _search.IsPaused || Boosters.IsPhotoOpen || _search.IsCompleted || _search.IsTransitionPending || _isTransitioning || _isPhotographing)
                return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_picture, eventData.position, eventData.pressEventCamera, out var local))
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
        }

        /// <summary>Reflects model state; visual replacement is separately staged under full blackout.</summary>
        private void Refresh()
        {
            _readyButton.gameObject.SetActive(!_search.IsStarted && !_isPhotographing);
            _originalTape.SetActive(!_search.IsStarted && !_isPhotographing);
            if (_displayedStage != _search.StageIndex && !_isTransitioning)
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

        /// <summary>Creates exactly one closed slot per actual stage anomaly; called only initially or under opaque black.</summary>
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
            var rootWidth = _slotsRoot.rect.width > 0 ? _slotsRoot.rect.width : 880f;
            for (var i = 0; i < anomalies.Count; i++)
            {
                var anomaly = anomalies[i];
                var image = AnomalyUi.Image(anomaly.Id, _stageRoot, anomaly.Image);
                AnomalyUi.Bounds(image.rectTransform, anomaly.Bounds.min, anomaly.Bounds.max);
                _imagesById.Add(anomaly.Id, image);
                var slot = UiKit.Image("Closed_" + anomaly.Id, _slotsRoot, _theme.SlotEmpty);
                slot.color = _theme.ClosedSlotColor;
                var left = (880f - rowWidth) * 0.5f + i * (slotWidth + slotGap);
                UiKit.PlaceIn(slot.rectTransform, 880f, 115f, left, 0, left + slotWidth, 115f);
                var cover = UiKit.FillLabel(_theme, slot.transform, "?", UiKit.TextStyleAlias.Dark, 80);
                cover.name = "Cover";
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
