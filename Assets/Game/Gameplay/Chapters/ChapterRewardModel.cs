using UnityEngine;

namespace Game.Gameplay.Chapters
{
    /// <summary>Passive description of one reward shown on a claim screen; granting stays with the reward system.</summary>
    public sealed class ChapterRewardModel
    {
        public Sprite Icon;
        public string Text;
        public int IconCount = 1;
    }
}
