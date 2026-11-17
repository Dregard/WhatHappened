using System;
using System.Collections.Generic;
using Game.Purchase;
using UnityEngine;

namespace Game.Levels
{
    [Serializable]
    public sealed class ChapterData
    {
        [Tooltip("Ordered levels in this chapter. Every address must also exist in LevelsData.Levels. Each level contributes its SearchData.ClueImage; empty chapters are invalid.")]
        public LevelReference[] LevelsArray = Array.Empty<LevelReference>();
        [Tooltip("Rewards granted once on the chapter victory screen, again on each new loop.")]
        public List<RewardData> RewardsList = new();
        [Tooltip("Rewards granted once after every non-final level of this chapter on its reward screen. Empty skips the reward screen. The final level goes straight to the culprit and chapter rewards.")]
        public List<RewardData> LevelRewardsList = new();
        [Tooltip("Location name shown in the HUD title plate, for example Laboratory. Empty hides the plate.")]
        public string Title;
        [Tooltip("Round portrait of the culprit revealed after the last evidence. Empty shows the unsolved question mark.")]
        public Sprite CulpritPortrait;
        [Tooltip("Caption of the culprit reveal, for example The octopus chef did it!")]
        public string CulpritCaption;
        [Tooltip("Picture of the culprit scene shown with the CASE SOLVED stamp. Empty skips the scene screen.")]
        public Sprite CulpritSceneImage;
        [Tooltip("Caption of the culprit scene, for example He mixed the potions for his new recipe!")]
        public string CulpritSceneCaption;
    }
}
