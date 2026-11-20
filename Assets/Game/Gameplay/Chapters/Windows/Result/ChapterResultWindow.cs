using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.UI.Windows.General;
using UnityEngine;

namespace Game.Gameplay.Chapters
{
    /// <summary>One continuously open window for the level result (evidence -> level reward) or the chapter finale (evidence -> all evidences -> culprit -> scene -> chapter reward). Elements that appear in several stages (evidence row, sunburst, logo, culprit circle, continue button) live in ChapterResultShared and move in place; each stage only fades its own elements through alpha, and each continuation button starts the next transition.</summary>
    public sealed class ChapterResultWindow : Window<ChapterResultWindowModel>
    {
        [SerializeField] private ChapterResultShared _shared;
        [SerializeField] private ChapterEvidenceStage _evidence;
        [SerializeField] private ChapterLevelRewardStage _levelReward;
        [SerializeField] private ChapterAllEvidencesStage _allEvidences;
        [SerializeField] private ChapterCulpritRevealStage _culpritReveal;
        [SerializeField] private ChapterCulpritSceneStage _culpritScene;
        [SerializeField] private ChapterChapterRewardStage _chapterReward;

        private ChapterResultStage _current;
        private bool _isSharedReady;

        protected override void Setup(ChapterResultWindowModel model)
        {
            foreach (var stage in new ChapterResultStage[] { _evidence, _levelReward, _allEvidences, _culpritReveal, _culpritScene, _chapterReward })
                stage.gameObject.SetActive(false);
            Run(model, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid Run(ChapterResultWindowModel model, CancellationToken token)
        {
            try
            {
                if (model.HasEvidence)
                    await Present(_evidence, model, token);
                if (!model.IsFinal)
                {
                    if (model.LevelRewardsList.Count > 0)
                        await Present(_levelReward, model, token);
                    else if (model.ClaimLevelReward != null)
                        await model.ClaimLevelReward();
                }
                else
                {
                    await Present(_allEvidences, model, token);
                    if (model.CulpritPortrait != null || !string.IsNullOrEmpty(model.CulpritCaption))
                        await Present(_culpritReveal, model, token);
                    if (model.CulpritSceneImage != null)
                        await Present(_culpritScene, model, token);
                    if (model.ChapterRewardsList.Count > 0)
                        await Present(_chapterReward, model, token);
                    else if (model.ClaimChapterReward != null)
                        await model.ClaimChapterReward();
                }
                model.ContinueAction?.Invoke();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        /// <summary>Moves the shared elements and fades the previous stage out together, prepares the next stage while transparent, then fades it in (with its own intro motion) and plays it.</summary>
        private async UniTask Present(ChapterResultStage stage, ChapterResultWindowModel model, CancellationToken token)
        {
            var layout = stage.Layout;
            var seconds = model.FadeSeconds;
            if (!_isSharedReady)
            {
                _shared.Setup(RowSprites(model), layout, model.SunburstTurnSeconds);
                _isSharedReady = true;
            }
            else
                await UniTask.WhenAll(_current.FadeOut(seconds, token), _shared.Exit(layout, seconds, token));
            stage.Begin(model, _shared);
            _current = stage;
            await UniTask.WhenAll(stage.FadeIn(seconds, token), _shared.Enter(layout, seconds, token), stage.Intro(model, token));
            await stage.Play(model, token);
        }

        /// <summary>Row pictures for the very first stage: the new evidence still shows as an empty slot until it lands.</summary>
        private static Sprite[] RowSprites(ChapterResultWindowModel model)
        {
            var clues = model.GetClues();
            if (!model.HasEvidence)
                return clues;
            var shown = new Sprite[clues.Length];
            for (var index = 0; index < model.NewClueIndex; index++)
                shown[index] = clues[index];
            return shown;
        }
    }
}
