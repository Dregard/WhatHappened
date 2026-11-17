using System.Collections.Generic;
using Game.Profile;

namespace Game.Gameplay
{
    public class GameplayProgressProfile : IProfile
    {
        public int LevelIndex;
        public int CurrentAttempt = 1;
        
        public List<int> RandomLevelIndices;
        public int LoopSelectionVersion;
        public bool IsLoopOrderRandomized;
        public int ChapterIteration;
        public int ChapterClueCount;
        public bool IsLevelVictoryPending;
        public bool IsChapterRewardClaimed;

        public AbandonedMissionData AbandonedMissionData;
    }
}
