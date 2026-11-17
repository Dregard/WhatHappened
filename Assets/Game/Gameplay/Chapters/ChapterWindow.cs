using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Anomalies;
using Game.UI.Windows.General;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Chapters
{
    public sealed class ChapterWindow : Window<ChapterWindowModel>
    {
        private const float RowCenterX = 540f;

        private UiThemeData _theme;
        private ChapterWindowModel _model;
        private RectTransform _content;
        private RectTransform _stage;
        private RectTransform _rowRoot;
        private readonly List<ChapterEvidenceSlotView> _slotsList = new();
        private readonly List<Image> _sunburstsList = new();
        private CancellationTokenSource _cancellation;
        private int _generation;

        /// <summary>Builds the investigation or starts the staged result sequence; repeated setup replaces the previous screen.</summary>
        protected override void Setup(ChapterWindowModel model)
        {
            Clear();
            _model = model;
            _theme = UiThemeData.LoadOrDefault();
            _cancellation = new CancellationTokenSource();
            var veil = UiKit.Image("Veil", transform, null);
            veil.enabled = true;
            veil.color = _theme.VeilColor;
            veil.raycastTarget = true;
            _content = veil.rectTransform;
            if (model.Mode == ChapterWindowModeAlias.Investigation)
            {
                BuildInvestigation();
                return;
            }
            RunResult(_generation, _cancellation.Token).Forget();
        }

        /// <summary>Rotates sunbursts at the configured speed; zero leaves them still.</summary>
        private void Update()
        {
            if (_model?.PresentationData == null || _model.PresentationData.SunburstTurnSeconds <= 0)
                return;
            var step = 360f / _model.PresentationData.SunburstTurnSeconds * Time.unscaledDeltaTime;
            foreach (var sunburst in _sunburstsList)
                if (sunburst != null)
                    sunburst.rectTransform.Rotate(0, 0, -step);
        }

        /// <summary>Destroys the current screen and invalidates its running sequence.</summary>
        private void Clear()
        {
            _generation++;
            _cancellation?.Cancel();
            _cancellation?.Dispose();
            _cancellation = null;
            if (_content != null)
                Destroy(_content.gameObject);
            _content = null;
            _stage = null;
            _rowRoot = null;
            _slotsList.Clear();
            _sunburstsList.Clear();
        }

        /// <summary>Cancels the sequence with the window instance.</summary>
        private void OnDestroy()
        {
            _cancellation?.Cancel();
            _cancellation?.Dispose();
        }

        #region Investigation

        /// <summary>Shows found evidence and the unsolved culprit with the remaining-evidence prompt.</summary>
        private void BuildInvestigation()
        {
            var total = _model.CluesArray.Length;
            var found = 0;
            foreach (var clue in _model.CluesArray)
                if (clue != null)
                    found++;
            var body = UiKit.Image("PopupBody", _content, _theme.PopupBodyTall, true);
            UiKit.PlacePadded(body.rectTransform, 80, 340, 1000, 1560, UiKit.BodyPad);
            var ribbon = UiKit.Image("Ribbon", _content, _theme.RibbonRed);
            UiKit.PlacePadded(ribbon.rectTransform, 130, 282, 950, 434, UiKit.RibbonPad);
            UiKit.FillLabel(_theme, ribbon.transform, "INVESTIGATION", UiKit.TextStyleAlias.Light, 80, new Vector2(0.14f, 0.22f), new Vector2(0.86f, 0.72f));
            UiKit.Label(_theme, "Found", _content, $"You found {found}/{total} evidences", UiKit.TextStyleAlias.Dark, 160, 430, 920, 495, 56);
            AddOpaquePanel(113, 513, 967, 1327);
            const float contourCenterY = 1080f;
            var contour = UiKit.Image("Contour", _content, _theme.EvidenceBigContour);
            UiKit.Place(contour.rectTransform, 318, contourCenterY - 222, 762, contourCenterY + 222);
            UiKit.Label(_theme, "Question", contour.transform, "?", UiKit.TextStyleAlias.Light, 0, 0, 1, 1, 300);
            FillAnchors(contour.transform.Find("Question"), 0.15f, 0.15f, 0.85f, 0.85f);
            BuildRow(_content, 658f, FitDiameter(total, 140f, 760f), _model.CluesArray, total);
            for (var i = 0; i < _slotsList.Count; i++)
                if (_model.CluesArray[i] != null)
                    DrawBeam(_content, _slotsList[i].Rect, contour.rectTransform, 200f);
            var remaining = total - found;
            var prompt = remaining == 0 ? "All evidences found!" : remaining == 1 ? "Find the last evidence to solve the case!" : $"Find {remaining} more evidences to solve the case!";
            UiKit.Label(_theme, "Prompt", _content, prompt, UiKit.TextStyleAlias.Light, 140, 1262, 940, 1318, 40);
            var logo = UiKit.Image("Logo", _content, _theme.Logo);
            UiKit.Place(logo.rectTransform, 370, 1350, 720, 1490);
            var close = UiKit.Button(_theme, "Close", _content, _theme.CloseIcon, null, 935, 335, 1025, 420, () => _model.CloseAction?.Invoke());
            close.transition = Selectable.Transition.None;
        }

        /// <summary>Stacks the translucent dark panel art so it reads as the opaque dark panel of the mockup on the cream popup.</summary>
        private void AddOpaquePanel(float left, float top, float right, float bottom)
        {
            for (var layer = 0; layer < 3; layer++)
            {
                var panel = UiKit.Image("Panel" + layer, _content, _theme.PanelDark, true);
                UiKit.PlacePadded(panel.rectTransform, left, top, right, bottom, new Vector4(3f, 3f, 3f, 4f));
            }
        }

        /// <summary>Draws a thin gold beam from an evidence circle toward the culprit circle's edge.</summary>
        private void DrawBeam(Transform parent, RectTransform from, RectTransform to, float stopRadius)
        {
            var beam = AnomalyUi.Image("Beam", parent);
            beam.color = new Color(_theme.GoldTextColor.r, _theme.GoldTextColor.g, _theme.GoldTextColor.b, 0.7f);
            var root = (RectTransform)_content;
            var start = AnomalyUi.LocalRect(from, root).center;
            var end = AnomalyUi.LocalRect(to, root).center;
            var direction = (end - start).normalized;
            start += direction * (AnomalyUi.LocalRect(from, root).width * 0.5f);
            end -= direction * stopRadius * (AnomalyUi.LocalRect(to, root).width / 400f);
            var rect = beam.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(6f, Vector2.Distance(start, end));
            rect.localPosition = start;
            rect.localRotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.up, end - start));
        }

        #endregion

        #region Result sequence

        /// <summary>Runs the screens appropriate for the opening mode, then hands control back to the chapter system.</summary>
        private async UniTaskVoid RunResult(int generation, CancellationToken token)
        {
            try
            {
                if (_model.Mode == ChapterWindowModeAlias.EvidenceFound)
                {
                    await StepEvidence(token);
                    if (_model.IsFinal)
                    {
                        await Finale(token);
                        return;
                    }
                }
                if (_model.Mode != ChapterWindowModeAlias.AllEvidence && (_model.Mode != ChapterWindowModeAlias.EvidenceFound || !_model.IsFinal))
                {
                    if (_model.LevelRewardsList.Count > 0)
                        await StepReward(token, _model.LevelRewardsList, null, _model.ClaimLevelReward, false);
                    else if (_model.ClaimLevelReward != null)
                        await _model.ClaimLevelReward();
                    if (generation == _generation)
                        _model.FinishAction?.Invoke();
                    return;
                }
                await Finale(token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>Shows the new evidence, waits for NEXT, flies it into its slot and commits it.</summary>
        private async UniTask StepEvidence(CancellationToken token)
        {
            var total = _model.CluesArray.Length;
            var diameter = FitDiameter(total, 170f, 780f);
            var shown = new Sprite[total];
            for (var i = 0; i < _model.NewClueIndex; i++)
                shown[i] = _model.CluesArray[i];
            BuildStage();
            BuildRow(_stage, 328f, diameter, shown, total);
            UiKit.Label(_theme, "Title", _stage, "You found the evidence!", UiKit.TextStyleAlias.Gold, 70, 575, 1010, 665, 80);
            AddSunburst(_stage, 540f, 1011f, 760f);
            var big = new ChapterEvidenceSlotView(_theme, _stage, "NewEvidence", 540f, 1011f, 340f, _model.NewClue);
            if (!string.IsNullOrEmpty(_model.NewClueName))
                UiKit.Label(_theme, "Name", _stage, _model.NewClueName, UiKit.TextStyleAlias.Light, 120, 1290, 960, 1355, 48);
            var next = AddActionButton("NEXT");
            await WaitTap(next, token);
            next.interactable = false;
            foreach (Transform child in _stage)
                if (child.name is "Title" or "Name" or "Sunburst" or "NEXT")
                    child.gameObject.SetActive(false);
            await FlyTo(big.Rect, _slotsList[_model.NewClueIndex].Rect, _model.PresentationData.EvidenceFlightDuration, token);
            _slotsList[_model.NewClueIndex].SetFound(_model.NewClue);
            Destroy(big.Rect.gameObject);
            if (_model.CommitClue != null)
                await _model.CommitClue();
            _model.CluesArray[_model.NewClueIndex] = _model.NewClue;
        }

        /// <summary>Shows a reward claim screen over the evidence row; the claim happens only on GET.</summary>
        private async UniTask StepReward(CancellationToken token, List<ChapterRewardModel> rewards, string heading, Func<UniTask> claim, bool isChapter)
        {
            var total = _model.CluesArray.Length;
            BuildStage();
            if (isChapter)
            {
                UiKit.Label(_theme, "Heading", _stage, heading, UiKit.TextStyleAlias.Gold, 150, 245, 930, 355, 100);
                BuildRow(_stage, 468f, FitDiameter(total, 135f, 760f), _model.CluesArray, total);
                UiKit.Label(_theme, "Claim", _stage, "Claim your reward", UiKit.TextStyleAlias.Light, 160, 640, 920, 720, 72);
            }
            else
            {
                BuildRow(_stage, 328f, FitDiameter(total, 170f, 780f), _model.CluesArray, total);
                UiKit.Label(_theme, "Claim", _stage, "Claim your reward", UiKit.TextStyleAlias.Gold, 120, 575, 960, 665, 80);
            }
            AddSunburst(_stage, 540f, 1030f, 800f);
            BuildRewards(rewards, 1030f);
            var get = AddActionButton("GET");
            await WaitTap(get, token);
            get.interactable = false;
            if (claim != null)
                await claim();
        }

        /// <summary>Plays the all-evidences effect, reveals the culprit and scene, then offers the chapter reward.</summary>
        private async UniTask Finale(CancellationToken token)
        {
            var total = _model.CluesArray.Length;
            BuildStage();
            BuildRow(_stage, 328f, FitDiameter(total, 170f, 780f), _model.CluesArray, total);
            UiKit.Label(_theme, "Title", _stage, "You found all evidences!", UiKit.TextStyleAlias.Gold, 100, 535, 980, 625, 80);
            var contour = UiKit.Image("Contour", _stage, _theme.EvidenceBigContour);
            UiKit.Place(contour.rectTransform, 248, 718, 832, 1302);
            UiKit.Label(_theme, "Question", _stage, "?", UiKit.TextStyleAlias.Light, 360, 880, 720, 1140, 340);
            AddLogo(_stage);
            await PlayConvergence(token);
            if (_model.CulpritPortrait != null || !string.IsNullOrEmpty(_model.CulpritCaption))
                await StepCulpritReveal(token);
            if (_model.CulpritSceneImage != null)
                await StepCulpritScene(token);
            if (_model.ClaimChapterReward == null)
            {
                _model.FinishAction?.Invoke();
                return;
            }
            if (_model.ChapterRewardsList.Count > 0)
                await StepReward(token, _model.ChapterRewardsList, "Chapter complete!", _model.ClaimChapterReward, true);
            else
                await _model.ClaimChapterReward();
            _model.FinishAction?.Invoke();
        }

        /// <summary>Sends a glowing streak from every evidence to the unsolved circle, then bursts particles over it.</summary>
        private async UniTask PlayConvergence(CancellationToken token)
        {
            var duration = Mathf.Max(0.01f, _model.PresentationData.AllEvidenceDuration);
            var root = (RectTransform)_content;
            var target = AnomalyUi.LocalRect(((RectTransform)_stage.Find("Contour")), root).center;
            var streaks = new List<Image>();
            var starts = new List<Vector2>();
            foreach (var slot in _slotsList)
            {
                var streak = AnomalyUi.Image("Streak", _stage, _theme.TrailGlow);
                streak.preserveAspect = true;
                streaks.Add(streak);
                starts.Add(AnomalyUi.LocalRect(slot.Rect, root).center);
            }
            var burst = new List<Image>();
            for (var i = 0; i < 14; i++)
                burst.Add(AnomalyUi.Image("Particle", _stage, _theme.Particle));
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                var t = Mathf.Clamp01(elapsed / duration);
                for (var i = 0; i < streaks.Count; i++)
                {
                    var local = Mathf.Clamp01((t - 0.05f * i) / 0.6f);
                    var position = Vector2.Lerp(starts[i], target, Mathf.SmoothStep(0, 1, local));
                    AnomalyUi.Place(streaks[i].rectTransform, new Rect(position - Vector2.one * 50f, Vector2.one * 100f));
                    streaks[i].color = new Color(1, 1, 1, local > 0 && local < 1 ? 1 : 0);
                }
                var burstT = Mathf.Clamp01((t - 0.6f) / 0.4f);
                for (var i = 0; i < burst.Count; i++)
                {
                    var angle = Mathf.PI * 2 * i / burst.Count;
                    var position = target + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 260f * burstT;
                    AnomalyUi.Place(burst[i].rectTransform, new Rect(position - Vector2.one * 30f, Vector2.one * 60f));
                    burst[i].color = new Color(1, 1, 1, burstT > 0 ? 1 - burstT : 0);
                }
                await UniTask.NextFrame(token);
            }
            foreach (var image in streaks)
                Destroy(image.gameObject);
            foreach (var image in burst)
                Destroy(image.gameObject);
        }

        /// <summary>Shows the culprit portrait and caption; a tap or the configured delay continues.</summary>
        private async UniTask StepCulpritReveal(CancellationToken token)
        {
            BuildStage();
            if (!string.IsNullOrEmpty(_model.CulpritCaption))
                AddNarrative(_stage, _model.CulpritCaption, 265f);
            AddSunburst(_stage, 540f, 1011f, 820f);
            var ring = UiKit.Image("Ring", _stage, _theme.EvidenceRing);
            UiKit.Place(ring.rectTransform, 305, 776, 775, 1246);
            var maskRoot = UiKit.Image("PortraitMask", _stage, _theme.EvidenceDisc);
            UiKit.Place(maskRoot.rectTransform, 322, 793, 758, 1229);
            maskRoot.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var portrait = UiKit.Image("Portrait", maskRoot.transform, _model.CulpritPortrait);
            portrait.preserveAspect = false;
            ring.transform.SetAsLastSibling();
            AddLogo(_stage);
            var tap = UiKit.Image("TapCatcher", _stage, null);
            tap.enabled = true;
            tap.color = Color.clear;
            tap.raycastTarget = true;
            tap.rectTransform.anchorMin = Vector2.zero;
            tap.rectTransform.anchorMax = Vector2.one;
            var button = tap.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            await WaitTap(button, token, _model.PresentationData.CulpritRevealDuration);
        }

        /// <summary>Shows the culprit scene with the CASE SOLVED stamp and its caption; NEXT continues.</summary>
        private async UniTask StepCulpritScene(CancellationToken token)
        {
            BuildStage();
            if (!string.IsNullOrEmpty(_model.CulpritSceneCaption))
                AddNarrative(_stage, _model.CulpritSceneCaption, 265f);
            var frame = UiKit.Image("SceneFrame", _stage, null);
            frame.enabled = true;
            frame.color = Color.white;
            UiKit.Place(frame.rectTransform, 0, 515, 1080, 1280);
            var area = UiKit.Group("SceneArea", frame.transform, 0, 0, 1080, 765);
            area.anchorMin = new Vector2(0, 0);
            area.anchorMax = new Vector2(1, 1);
            area.offsetMin = new Vector2(0, 12);
            area.offsetMax = new Vector2(0, -12);
            var scene = AnomalyUi.Image("Scene", area, _model.CulpritSceneImage);
            scene.preserveAspect = false;
            var stamp = UiKit.Image("Stamp", _stage, _theme.CaseSolvedStamp);
            UiKit.Place(stamp.rectTransform, 85, 1090, 640, 1320);
            var next = AddActionButton("NEXT");
            await WaitTap(next, token);
        }

        /// <summary>Draws one claim icon group per reward with its amount underneath.</summary>
        private void BuildRewards(List<ChapterRewardModel> rewards, float centerY)
        {
            var count = Mathf.Max(1, rewards.Count);
            var width = 900f / count;
            for (var index = 0; index < rewards.Count; index++)
            {
                var reward = rewards[index];
                var cx = 540f - 450f + width * (index + 0.5f);
                var size = Mathf.Min(330f, width * 0.95f);
                var copies = Mathf.Max(1, reward.IconCount);
                for (var copy = 0; copy < copies; copy++)
                {
                    var isCenter = copies == 1 || copy == copies - 1;
                    var shift = copies == 1 ? 0f : (isCenter ? 0f : (copy == 0 ? -1f : 1f) * size * 0.48f);
                    var scale = isCenter ? 1f : 0.78f;
                    var icon = UiKit.Image("Reward", _stage, reward.Icon);
                    var half = size * 0.5f * scale;
                    UiKit.Place(icon.rectTransform, cx + shift - half, centerY - half, cx + shift + half, centerY + half);
                    if (isCenter)
                        icon.transform.SetAsLastSibling();
                }
                UiKit.Label(_theme, "Amount", _stage, reward.Text, UiKit.TextStyleAlias.Gold, cx - width * 0.5f, centerY + size * 0.5f + 30f, cx + width * 0.5f, centerY + size * 0.5f + 140f, 100);
            }
        }

        #endregion

        #region Building blocks

        /// <summary>Replaces the per-step content, keeping the veil.</summary>
        private void BuildStage()
        {
            if (_stage != null)
                Destroy(_stage.gameObject);
            _sunburstsList.Clear();
            _slotsList.Clear();
            var stage = UiKit.Group("Stage", _content, 0, 0, UiKit.ReferenceWidth, UiKit.ReferenceHeight);
            stage.gameObject.layer = gameObject.layer;
            _stage = stage;
        }

        /// <summary>Creates the evidence row centered horizontally, found evidences first, remembering each slot.</summary>
        private void BuildRow(Transform parent, float centerY, float diameter, Sprite[] sprites, int count)
        {
            _slotsList.Clear();
            var gap = diameter * 0.35f;
            var rowWidth = count * diameter + (count - 1) * gap;
            for (var index = 0; index < count; index++)
            {
                var centerX = RowCenterX - rowWidth * 0.5f + diameter * 0.5f + index * (diameter + gap);
                var sprite = index < sprites.Length ? sprites[index] : null;
                _slotsList.Add(new ChapterEvidenceSlotView(_theme, parent, "Evidence" + index, centerX, centerY, diameter, sprite));
            }
        }

        /// <summary>Shrinks the slot diameter so a row of any length fits the available width.</summary>
        private static float FitDiameter(int count, float preferred, float availableWidth)
        {
            if (count <= 0)
                return preferred;
            return Mathf.Min(preferred, availableWidth / (count + (count - 1) * 0.35f));
        }

        /// <summary>Adds a rotating white sunburst behind a celebrated item, tinted warm like the mockups.</summary>
        private void AddSunburst(Transform parent, float cx, float cy, float size)
        {
            var sunburst = UiKit.Image("Sunburst", parent, _theme.Sunburst);
            UiKit.Place(sunburst.rectTransform, cx - size * 0.5f, cy - size * 0.5f, cx + size * 0.5f, cy + size * 0.5f);
            sunburst.color = new Color(1f, 1f, 1f, 0.75f);
            _sunburstsList.Add(sunburst);
        }

        /// <summary>Adds the game logo beneath a result screen's main item.</summary>
        private void AddLogo(Transform parent)
        {
            var logo = UiKit.Image("Logo", parent, _theme.Logo);
            UiKit.Place(logo.rectTransform, 210, 1350, 900, 1600);
        }

        /// <summary>Adds a paper plate carrying narrative text near the top.</summary>
        private void AddNarrative(Transform parent, string text, float top)
        {
            var plate = UiKit.Image("Narrative", parent, _theme.PlateNarrative, true);
            UiKit.Place(plate.rectTransform, 70, top, 1010, top + 155);
            var label = UiKit.FillLabel(_theme, plate.transform, text, UiKit.TextStyleAlias.Dark, 56);
            label.margin = new Vector4(90, 0, 90, 0);
        }

        /// <summary>Creates the large green primary action beneath the screen's main content.</summary>
        private Button AddActionButton(string text)
        {
            var button = UiKit.Button(_theme, text, _stage, _theme.ButtonGreen, text, 265, 1445, 815, 1595, null, 80, UiKit.ButtonPad);
            button.gameObject.name = text;
            return button;
        }

        /// <summary>Waits for a single press of a button, or the optional timeout when a positive one is supplied.</summary>
        private async UniTask WaitTap(Button button, CancellationToken token, float timeout = -1f)
        {
            var source = new UniTaskCompletionSource();
            UnityEngine.Events.UnityAction release = () => source.TrySetResult();
            button.onClick.AddListener(release);
            try
            {
                if (timeout > 0)
                    await UniTask.WhenAny(source.Task, UniTask.Delay(TimeSpan.FromSeconds(timeout), ignoreTimeScale: true, cancellationToken: token));
                else
                    await source.Task.AttachExternalCancellation(token);
            }
            finally
            {
                if (button != null)
                    button.onClick.RemoveListener(release);
            }
        }

        /// <summary>Moves and resizes a layer onto a target rectangle over the given seconds.</summary>
        private async UniTask FlyTo(RectTransform layer, RectTransform target, float seconds, CancellationToken token)
        {
            var root = (RectTransform)_content;
            var source = AnomalyUi.LocalRect(layer, root);
            var destination = AnomalyUi.LocalRect(target, root);
            for (var elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                var t = Mathf.SmoothStep(0, 1, elapsed / seconds);
                var size = Vector2.Lerp(source.size, destination.size, t);
                AnomalyUi.Place(layer, new Rect(Vector2.Lerp(source.center, destination.center, t) - size * 0.5f, size));
                await UniTask.NextFrame(token);
            }
            AnomalyUi.Place(layer, destination);
        }

        /// <summary>Overrides a child's normalized anchors, used for text that must fill part of its parent.</summary>
        private static void FillAnchors(Transform child, float minX, float minY, float maxX, float maxY)
        {
            var rect = (RectTransform)child;
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        #endregion
    }
}
