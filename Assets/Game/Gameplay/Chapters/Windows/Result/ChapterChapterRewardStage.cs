using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Game.Gameplay.Chapters
{
    /// <summary>Reward claim after the chapter is solved.</summary>
    public sealed class ChapterChapterRewardStage : ChapterRewardStageBase
    {
        private static readonly ChapterSharedLayout StageLayout = new()
        {
            Row = new ChapterRowLayout(468f, 140f, 760f, 0.35f, 1920f),
            Sunburst = new ChapterAnchors(0.13f, 0.255f, 0.87f, 0.672f),
            Button = new ChapterAnchors(0.239f, 0.161f, 0.761f, 0.25f),
            ButtonText = "GET",
        };

        public override ChapterSharedLayout Layout { get { return StageLayout; } }

        protected override List<ChapterRewardModel> Rewards(ChapterResultWindowModel model)
        {
            return model.ChapterRewardsList;
        }

        protected override string Heading(ChapterResultWindowModel model)
        {
            return model.ChapterRewardHeading;
        }

        protected override UniTask Claim(ChapterResultWindowModel model)
        {
            return model.ClaimChapterReward != null ? model.ClaimChapterReward() : UniTask.CompletedTask;
        }
    }
}
