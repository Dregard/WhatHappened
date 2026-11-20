using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Chapters
{
    /// <summary>Shows the culprit scene with the CASE SOLVED stamp and its caption; the shared NEXT button continues.</summary>
    public sealed class ChapterCulpritSceneStage : ChapterResultStage
    {
        private static readonly ChapterSharedLayout StageLayout = new()
        {
            Button = new ChapterAnchors(0.239f, 0.161f, 0.761f, 0.25f),
            ButtonText = "NEXT",
        };

        [SerializeField] private GameObject _narrative;
        [SerializeField] private TMP_Text _narrativeLabel;
        [SerializeField] private Image _sceneImage;

        public override ChapterSharedLayout Layout { get { return StageLayout; } }

        protected override void Prepare(ChapterResultWindowModel model)
        {
            _narrative.SetActive(!string.IsNullOrEmpty(model.CulpritSceneCaption));
            _narrativeLabel.text = model.CulpritSceneCaption;
            _sceneImage.sprite = model.CulpritSceneImage;
        }

        public override UniTask Play(ChapterResultWindowModel model, CancellationToken token)
        {
            return Shared.WaitButton(token);
        }
    }
}
