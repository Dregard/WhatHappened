using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Game.Gameplay.Chapters
{
    /// <summary>Reward claim after a level's evidence; the rewards fly out of the found evidence in the shared row.</summary>
    public sealed class ChapterLevelRewardStage : ChapterRewardStageBase
    {
        private static readonly ChapterSharedLayout StageLayout = new()
        {
            Row = new ChapterRowLayout(328f, 170f, 780f, 0.35f, 1920f),
            Sunburst = new ChapterAnchors(0.213f, 0.313f, 0.787f, 0.635f),
            Button = new ChapterAnchors(0.239f, 0.161f, 0.761f, 0.25f),
            ButtonText = "GET",
        };

        public override ChapterSharedLayout Layout { get { return StageLayout; } }

        protected override bool EmergesFromEvidence { get { return true; } }

        protected override List<ChapterRewardModel> Rewards(ChapterResultWindowModel model)
        {
            return model.LevelRewardsList;
        }

        protected override string Heading(ChapterResultWindowModel model)
        {
            return null;
        }

        protected override UniTask Claim(ChapterResultWindowModel model)
        {
            return model.ClaimLevelReward != null ? model.ClaimLevelReward() : UniTask.CompletedTask;
        }
    }
}
