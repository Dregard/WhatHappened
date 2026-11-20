using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Anomalies;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Chapters
{
    /// <summary>Shows a reward claim next to the shared evidence row; the claim happens only on the shared GET button. Concrete stages differ in layout and in which model rewards they show.</summary>
    public abstract class ChapterRewardStageBase : ChapterResultStage
    {
        [SerializeField] private TMP_Text _heading;
        [SerializeField] private RectTransform _rewards;
        [SerializeField] private Image _iconTemplate;
        [SerializeField] private TMP_Text _amountTemplate;
        [Tooltip("Mockup Y (1080x1920) of the reward icon center.")]
        [SerializeField] private float _rewardCenterY = 1030f;
        [Tooltip("Largest reward icon size in mockup pixels.")]
        [SerializeField] private float _rewardMaxSize = 330f;
        [Tooltip("Mockup pixels between the icon bottom and the top of the amount text.")]
        [SerializeField] private float _amountGap = 30f;
        [Tooltip("Mockup height of the amount text box.")]
        [SerializeField] private float _amountHeight = 110f;

        private readonly List<RectTransform> _itemsList = new();
        private readonly List<ChapterAnchors> _targetsList = new();

        protected abstract List<ChapterRewardModel> Rewards(ChapterResultWindowModel model);
        protected abstract string Heading(ChapterResultWindowModel model);
        protected abstract UniTask Claim(ChapterResultWindowModel model);

        /// <summary>True when the rewards fly out of the evidence just found instead of only fading in.</summary>
        protected virtual bool EmergesFromEvidence { get { return false; } }

        protected override void Prepare(ChapterResultWindowModel model)
        {
            if (_heading != null)
                _heading.text = Heading(model);
            ClearRewards();
            BuildRewards(Rewards(model), _rewardCenterY);
        }

        /// <summary>Lets each reward travel from the found evidence's slot to its place while the stage fades in.</summary>
        public override async UniTask Intro(ChapterResultWindowModel model, CancellationToken token)
        {
            var slots = Shared.Row.Slots;
            if (!EmergesFromEvidence || model.LastFoundIndex >= slots.Count)
                return;
            var origin = slots[model.LastFoundIndex].Rect;
            var center = (origin.anchorMin + origin.anchorMax) * 0.5f;
            var duration = Mathf.Max(0.01f, model.FlightDuration);
            for (var index = 0; index < _itemsList.Count; index++)
            {
                _itemsList[index].anchorMin = center;
                _itemsList[index].anchorMax = center;
            }
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                var t = Mathf.SmoothStep(0, 1, elapsed / duration);
                for (var index = 0; index < _itemsList.Count; index++)
                {
                    var target = _targetsList[index];
                    var targetCenter = (target.Min + target.Max) * 0.5f;
                    var from = Vector2.Lerp(center, targetCenter, t);
                    var half = (target.Max - target.Min) * 0.5f * t;
                    _itemsList[index].anchorMin = from - half;
                    _itemsList[index].anchorMax = from + half;
                }
                await UniTask.NextFrame(token);
            }
            for (var index = 0; index < _itemsList.Count; index++)
            {
                _itemsList[index].anchorMin = _targetsList[index].Min;
                _itemsList[index].anchorMax = _targetsList[index].Max;
            }
        }

        public override async UniTask Play(ChapterResultWindowModel model, CancellationToken token)
        {
            await Shared.WaitButton(token);
            await Claim(model);
        }

        /// <summary>Removes reward objects of an earlier build, keeping the templates.</summary>
        private void ClearRewards()
        {
            _itemsList.Clear();
            _targetsList.Clear();
            for (var index = _rewards.childCount - 1; index >= 0; index--)
            {
                var child = _rewards.GetChild(index).gameObject;
                if (child != _iconTemplate.gameObject && child != _amountTemplate.gameObject)
                    Destroy(child);
            }
        }

        private void Track(RectTransform rect)
        {
            _itemsList.Add(rect);
            _targetsList.Add(new ChapterAnchors(rect.anchorMin.x, rect.anchorMin.y, rect.anchorMax.x, rect.anchorMax.y));
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
                var size = Mathf.Min(_rewardMaxSize, width * 0.95f);
                var copies = Mathf.Max(1, reward.IconCount);
                for (var copy = 0; copy < copies; copy++)
                {
                    var isCenter = copies == 1 || copy == copies - 1;
                    var shift = copies == 1 ? 0f : (isCenter ? 0f : (copy == 0 ? -1f : 1f) * size * 0.48f);
                    var scale = isCenter ? 1f : 0.78f;
                    var icon = Instantiate(_iconTemplate, _rewards, false);
                    icon.sprite = reward.Icon;
                    icon.enabled = reward.Icon != null;
                    var half = size * 0.5f * scale;
                    UiKit.Place(icon.rectTransform, cx + shift - half, centerY - half, cx + shift + half, centerY + half);
                    if (isCenter)
                        icon.transform.SetAsLastSibling();
                    Track(icon.rectTransform);
                }
                var amount = Instantiate(_amountTemplate, _rewards, false);
                UiKit.Place(amount.rectTransform, cx - width * 0.5f, centerY + size * 0.5f + _amountGap, cx + width * 0.5f, centerY + size * 0.5f + _amountGap + _amountHeight);
                amount.text = reward.Text;
                amount.ForceMeshUpdate();
                Track(amount.rectTransform);
            }
        }
    }
}
