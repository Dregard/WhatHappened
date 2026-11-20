using System.Threading;
using Cysharp.Threading.Tasks;
using Game.UI.Buttons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Chapters
{
    /// <summary>Shows the culprit portrait and caption inside the circle left by the all-evidences effect; a tap or the configured delay continues.</summary>
    public sealed class ChapterCulpritRevealStage : ChapterResultStage
    {
        private static readonly ChapterSharedLayout StageLayout = new()
        {
            Sunburst = new ChapterAnchors(0.082f, 0.239f, 0.918f, 0.708f),
            Logo = new ChapterAnchors(0.11f, 0.149f, 0.908f, 0.312f),
            Contour = new ChapterAnchors(0.247f, 0.331f, 0.753f, 0.616f),
            ContourFadesWithStage = true,
        };

        [SerializeField] private GameObject _narrative;
        [SerializeField] private TMP_Text _narrativeLabel;
        [SerializeField] private Image _portrait;
        [SerializeField] private GameButton _tapButton;

        public override ChapterSharedLayout Layout { get { return StageLayout; } }

        protected override void Prepare(ChapterResultWindowModel model)
        {
            _narrative.SetActive(!string.IsNullOrEmpty(model.CulpritCaption));
            _narrativeLabel.text = model.CulpritCaption;
            _portrait.sprite = model.CulpritPortrait;
            _portrait.enabled = model.CulpritPortrait != null;
            _tapButton.SetInteractable(true);
        }

        public override async UniTask Play(ChapterResultWindowModel model, CancellationToken token)
        {
            await _tapButton.WaitPress(token, model.RevealDuration);
            _tapButton.SetInteractable(false);
        }
    }
}
