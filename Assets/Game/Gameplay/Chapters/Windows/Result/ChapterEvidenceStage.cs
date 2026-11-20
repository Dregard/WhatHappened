using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Anomalies;
using TMPro;
using UnityEngine;

namespace Game.Gameplay.Chapters
{
    /// <summary>Shows the new evidence, waits for NEXT, fades the decoration away while the evidence flies into its slot of the shared row, then commits it.</summary>
    public sealed class ChapterEvidenceStage : ChapterResultStage
    {
        // Layout of the 1080x2400 mockup 07_Win_Evidence_Found_1080x2400 (pixels, origin top-left; diameters include the art shadow margin).
        private const float MockupHeight = 2400f;
        private const float BigCenterY = 1258f;
        private const float BigDiameter = 433f;

        private static readonly ChapterSharedLayout StageLayout = new()
        {
            Row = new ChapterRowLayout(566f, 182f, 780f, 0.264f, MockupHeight),
            Sunburst = new ChapterAnchors(0.148f, 0.319f, 0.852f, 0.636f),
            Button = new ChapterAnchors(0.239f, 0.231f, 0.761f, 0.3f),
            ButtonText = "NEXT",
        };

        [SerializeField] private RectTransform _content;
        [SerializeField] private ChapterEvidenceSlotView _slotPrefab;
        [SerializeField] private GameObject _title;
        [SerializeField] private TMP_Text _nameLabel;

        private ChapterEvidenceSlotView _big;

        public override ChapterSharedLayout Layout { get { return StageLayout; } }

        protected override void Prepare(ChapterResultWindowModel model)
        {
            _title.SetActive(true);
            _nameLabel.gameObject.SetActive(!string.IsNullOrEmpty(model.NewClueName));
            _nameLabel.text = model.NewClueName;
            _big = Instantiate(_slotPrefab, _content, false);
            _big.transform.SetAsFirstSibling();
            _big.name = "NewEvidence";
            _big.Place(540f, BigCenterY, BigDiameter, model.NewClue, MockupHeight);
        }

        public override async UniTask Play(ChapterResultWindowModel model, CancellationToken token)
        {
            await Shared.WaitButton(token);
            var slot = Shared.Row.Slots[model.NewClueIndex];
            await UniTask.WhenAll(
                ChapterFade.Out(_title, model.FadeSeconds, token),
                ChapterFade.Out(_nameLabel.gameObject, model.FadeSeconds, token),
                Shared.HideButton(model.FadeSeconds, token),
                FlyTo(_big.Rect, slot.Rect, model.FlightDuration, token));
            slot.SetFound(model.NewClue);
            Destroy(_big.gameObject);
            if (model.CommitClue != null)
                await model.CommitClue();
        }

        /// <summary>Moves and resizes a layer onto a target rectangle over the given seconds.</summary>
        private async UniTask FlyTo(RectTransform layer, RectTransform target, float seconds, CancellationToken token)
        {
            var source = AnomalyUi.LocalRect(layer, _content);
            var destination = AnomalyUi.LocalRect(target, _content);
            for (var elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                var t = Mathf.SmoothStep(0, 1, elapsed / seconds);
                var size = Vector2.Lerp(source.size, destination.size, t);
                AnomalyUi.Place(layer, new Rect(Vector2.Lerp(source.center, destination.center, t) - size * 0.5f, size));
                await UniTask.NextFrame(token);
            }
            AnomalyUi.Place(layer, destination);
        }
    }
}
