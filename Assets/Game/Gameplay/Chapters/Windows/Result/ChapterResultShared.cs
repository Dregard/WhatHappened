using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.UI.Buttons;
using UnityEngine;

namespace Game.Gameplay.Chapters
{
    /// <summary>Elements that stay on screen across result stages: the evidence row, sunburst, logo, culprit contour/question and the single continue button. A stage declares where each belongs; moving between stages slides the elements and fades only those that appear or disappear.</summary>
    public sealed class ChapterResultShared : MonoBehaviour
    {
        [SerializeField] private ChapterEvidenceRow _row;
        [SerializeField] private ChapterSunburstSpinner _sunburst;
        [SerializeField] private RectTransform _logo;
        [SerializeField] private RectTransform _contour;
        [SerializeField] private RectTransform _question;
        [SerializeField] private GameButtonWithText _button;

        private CanvasGroup _rowGroup;
        private CanvasGroup _sunburstGroup;
        private CanvasGroup _logoGroup;
        private CanvasGroup _contourGroup;
        private CanvasGroup _questionGroup;
        private CanvasGroup _buttonGroup;
        private bool _rowVisible;
        private bool _sunburstVisible;
        private bool _logoVisible;
        private bool _contourVisible;
        private bool _questionVisible;
        private bool _buttonVisible;
        private string _buttonText;

        public ChapterEvidenceRow Row { get { return _row; } }
        public RectTransform Contour { get { return _contour; } }

        /// <summary>Builds the row for the first stage and hides every shared element, ready for the first Enter.</summary>
        public void Setup(Sprite[] rowSprites, ChapterSharedLayout first, float sunburstTurnSeconds)
        {
            _sunburst.Initialize(sunburstTurnSeconds);
            _rowGroup = Prepare(_row.gameObject);
            _sunburstGroup = Prepare(_sunburst.gameObject);
            _logoGroup = Prepare(_logo.gameObject);
            _contourGroup = Prepare(_contour.gameObject);
            _questionGroup = Prepare(_question.gameObject);
            _buttonGroup = Prepare(_button.gameObject);
            _button.SetInteractable(false);
            if (first.Row.HasValue)
                _row.Build(first.Row.Value, rowSprites);
        }

        /// <summary>Moves what stays and fades out what the next stage no longer shows; runs together with the old stage's fade-out.</summary>
        public UniTask Exit(ChapterSharedLayout next, float seconds, CancellationToken token)
        {
            _button.SetInteractable(false);
            _buttonGroup.blocksRaycasts = false;
            var tasks = new List<UniTask>();
            if (_rowVisible)
                tasks.Add(next.Row.HasValue ? _row.MoveTo(next.Row.Value, seconds, token) : ChapterFade.To(_rowGroup, 0f, seconds, token));
            tasks.Add(ExitItem(_sunburstGroup, _sunburst.transform as RectTransform, _sunburstVisible, next.Sunburst, false, seconds, token));
            tasks.Add(ExitItem(_logoGroup, _logo, _logoVisible, next.Logo, false, seconds, token));
            tasks.Add(ExitItem(_contourGroup, _contour, _contourVisible, next.Contour, next.ContourFadesWithStage, seconds, token));
            tasks.Add(ExitItem(_questionGroup, _question, _questionVisible, next.Question, false, seconds, token));
            var buttonStays = _buttonVisible && next.Button.HasValue && next.ButtonText == _buttonText;
            tasks.Add(buttonStays
                ? Move(_button.transform as RectTransform, next.Button.Value, seconds, token)
                : _buttonVisible ? ChapterFade.To(_buttonGroup, 0f, seconds, token) : UniTask.CompletedTask);
            return UniTask.WhenAll(tasks);
        }

        /// <summary>Brings in what the new stage adds and removes the contour handed over to the stage; runs together with the new stage's fade-in.</summary>
        public UniTask Enter(ChapterSharedLayout next, float seconds, CancellationToken token)
        {
            var tasks = new List<UniTask>();
            if (next.Row.HasValue)
            {
                if (!_rowVisible)
                    _row.Arrange(next.Row.Value);
                tasks.Add(ChapterFade.To(_rowGroup, 1f, seconds, token));
            }
            tasks.Add(EnterItem(_sunburstGroup, _sunburst.transform as RectTransform, next.Sunburst, seconds, token));
            tasks.Add(EnterItem(_logoGroup, _logo, next.Logo, seconds, token));
            tasks.Add(EnterItem(_questionGroup, _question, next.Question, seconds, token));
            if (next.Contour.HasValue && next.ContourFadesWithStage)
                tasks.Add(ChapterFade.To(_contourGroup, 0f, seconds, token));
            else
                tasks.Add(EnterItem(_contourGroup, _contour, next.Contour, seconds, token));
            if (next.Button.HasValue)
            {
                if (!_buttonVisible || next.ButtonText != _buttonText)
                {
                    Place(_button.transform as RectTransform, next.Button.Value);
                    _button.SetText(next.ButtonText);
                    _buttonText = next.ButtonText;
                }
                _buttonGroup.blocksRaycasts = true;
                tasks.Add(ChapterFade.To(_buttonGroup, 1f, seconds, token));
            }
            _rowVisible = next.Row.HasValue;
            _sunburstVisible = next.Sunburst.HasValue;
            _logoVisible = next.Logo.HasValue;
            _questionVisible = next.Question.HasValue;
            _contourVisible = next.Contour.HasValue && !next.ContourFadesWithStage;
            _buttonVisible = next.Button.HasValue;
            return UniTask.WhenAll(tasks);
        }

        /// <summary>Enables the continue button, waits for one press, then disables it so repeated presses cannot advance again.</summary>
        public async UniTask WaitButton(CancellationToken token)
        {
            _button.SetInteractable(true);
            await _button.WaitPress(token);
            _button.SetInteractable(false);
        }

        /// <summary>Fades the continue button away, e.g. while the new evidence flies to its slot.</summary>
        public UniTask HideButton(float seconds, CancellationToken token)
        {
            _buttonVisible = false;
            return ChapterFade.To(_buttonGroup, 0f, seconds, token);
        }

        private UniTask ExitItem(CanvasGroup group, RectTransform rect, bool visible, ChapterAnchors? next, bool keepForStage, float seconds, CancellationToken token)
        {
            if (!visible)
                return UniTask.CompletedTask;
            if (next.HasValue)
                return Move(rect, next.Value, seconds, token);
            return keepForStage ? UniTask.CompletedTask : ChapterFade.To(group, 0f, seconds, token);
        }

        private UniTask EnterItem(CanvasGroup group, RectTransform rect, ChapterAnchors? next, float seconds, CancellationToken token)
        {
            if (!next.HasValue || group.alpha >= 1f)
                return UniTask.CompletedTask;
            Place(rect, next.Value);
            return ChapterFade.To(group, 1f, seconds, token);
        }

        private static CanvasGroup Prepare(GameObject target)
        {
            target.SetActive(true);
            var group = ChapterFade.Group(target);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            return group;
        }

        private static void Place(RectTransform rect, ChapterAnchors anchors)
        {
            rect.anchorMin = anchors.Min;
            rect.anchorMax = anchors.Max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static async UniTask Move(RectTransform rect, ChapterAnchors target, float seconds, CancellationToken token)
        {
            var min = rect.anchorMin;
            var max = rect.anchorMax;
            for (var elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                var t = Mathf.SmoothStep(0, 1, elapsed / seconds);
                rect.anchorMin = Vector2.Lerp(min, target.Min, t);
                rect.anchorMax = Vector2.Lerp(max, target.Max, t);
                await UniTask.NextFrame(token);
            }
            Place(rect, target);
        }
    }
}
