using System.Linq;
using Game.Profile;
using Game.Levels;
using UnityEngine;
using Zenject;

namespace Game.Gameplay
{
    public class GameplayProgressController : ProfileHolderBase<GameplayProgressProfile>
    {
        private LevelsData _levelsData;

        public bool UsesChapters { get { return _levelsData?.ChaptersArray?.Length > 0; } }
        public ChapterData CurrentChapter { get { return _levelsData.ChaptersArray[Profile.ChapterIteration % _levelsData.ChaptersArray.Length]; } }
        public int ChapterNumber { get { return Profile.ChapterIteration + 1; } }
        public int ClueCount { get { return Profile.ChapterClueCount; } }
        public bool IsLevelVictoryPending { get { return Profile.IsLevelVictoryPending; } }
        public bool IsChapterComplete { get { return UsesChapters && ClueCount == CurrentChapter.LevelsArray.Length; } }
        public bool IsChapterRewardClaimed { get { return Profile.IsChapterRewardClaimed; } }

        /// <summary>Records one completed level in the active chapter. Duplicate events remain inert until continuation.</summary>
        public bool CollectClue()
        {
            if (!UsesChapters || IsLevelVictoryPending || IsChapterComplete)
                return false;
            Profile.ChapterClueCount++;
            Profile.IsLevelVictoryPending = true;
            HandleLevelCompleted();
            return true;
        }

        /// <summary>Acknowledges the mandatory level victory without clearing collected clues.</summary>
        public void AcknowledgeLevelVictory()
        {
            Profile.IsLevelVictoryPending = false;
        }

        /// <summary>Reserves the active chapter reward before invoking processors; repeated window opens cannot grant again.</summary>
        public bool TryClaimChapterReward()
        {
            if (!IsChapterComplete || IsLevelVictoryPending || Profile.IsChapterRewardClaimed)
                return false;
            Profile.IsChapterRewardClaimed = true;
            return true;
        }

        /// <summary>Starts a fresh chapter only after all clues, both screens and the reward claim; looping clears every slot.</summary>
        public bool StartNextChapter()
        {
            if (!IsChapterComplete || IsLevelVictoryPending || !IsChapterRewardClaimed)
                return false;
            Profile.ChapterIteration++;
            Profile.ChapterClueCount = 0;
            Profile.IsChapterRewardClaimed = false;
            return true;
        }


        /// <summary>Validates chapter references and saved prefix progress, or repairs the legacy loop permutation when chapters are absent.</summary>
        public void Configure(LevelsData levelsData)
        {
            _levelsData = levelsData;
            if (levelsData.Levels.Length == 0)
            {
                throw new System.InvalidOperationException("The level catalog must contain at least one level.");
            }
            if (UsesChapters)
            {
                foreach (var chapter in levelsData.ChaptersArray)
                {
                    if (chapter?.LevelsArray == null || chapter.LevelsArray.Length == 0 || chapter.RewardsList == null)
                        throw new System.InvalidOperationException("Every chapter requires levels and a reward list.");
                    foreach (var level in chapter.LevelsArray)
                        GetCatalogIndex(level);
                }
                if (Profile.ChapterIteration < 0 || ClueCount < 0 || ClueCount > CurrentChapter.LevelsArray.Length)
                    throw new System.InvalidOperationException("Saved chapter progress does not match the chapter configuration.");
                return;
            }
            var startIndex = levelsData.Levels.Length - levelsData.LoopLevelCount;
            var savedIndicesList = Profile.RandomLevelIndices;
            var isValid = savedIndicesList != null && savedIndicesList.Count == levelsData.LoopLevelCount &&
                savedIndicesList.Distinct().Count() == savedIndicesList.Count &&
                savedIndicesList.All(index => index >= startIndex && index < levelsData.Levels.Length);
            var hasOrderChanged = Profile.LoopSelectionVersion > 0 && Profile.IsLoopOrderRandomized != levelsData.RandomizeLoopOrder;
            if (!isValid || hasOrderChanged || (!levelsData.RandomizeLoopOrder && !savedIndicesList.SequenceEqual(Enumerable.Range(startIndex, levelsData.LoopLevelCount))))
            {
                GenerateLoopIndices();
            }
            else if (Profile.LoopSelectionVersion == 0 && levelsData.RandomizeLoopOrder && Profile.LevelIndex >= levelsData.Levels.Length)
            {
                var rotation = levelsData.Levels.Length % savedIndicesList.Count;
                Profile.RandomLevelIndices = savedIndicesList.Skip(rotation).Concat(savedIndicesList.Take(rotation)).ToList();
            }
            Profile.LoopSelectionVersion = 1;
            Profile.IsLoopOrderRandomized = levelsData.RandomizeLoopOrder;
        }

        /// <summary>Returns a current/future catalog index in chapter order, or the retained legacy loop when chapters are absent. Pending victories retain their completed level.</summary>
        public int GetLevelIndexAtOffset(int offset)
        {
            if (UsesChapters)
            {
                var chapterIndex = Profile.ChapterIteration % _levelsData.ChaptersArray.Length;
                var levelIndex = System.Math.Min(ClueCount, CurrentChapter.LevelsArray.Length - 1);
                if (IsLevelVictoryPending)
                    levelIndex = ClueCount - 1;
                levelIndex += offset;
                while (levelIndex >= _levelsData.ChaptersArray[chapterIndex].LevelsArray.Length)
                {
                    levelIndex -= _levelsData.ChaptersArray[chapterIndex].LevelsArray.Length;
                    chapterIndex = (chapterIndex + 1) % _levelsData.ChaptersArray.Length;
                }
                return GetCatalogIndex(_levelsData.ChaptersArray[chapterIndex].LevelsArray[levelIndex]);
            }
            var progressionIndex = Profile.LevelIndex + offset;
            if (_levelsData == null || progressionIndex < _levelsData.Levels.Length)
            {
                return progressionIndex;
            }
            var loopIndex = (progressionIndex - _levelsData.Levels.Length) % Profile.RandomLevelIndices.Count;
            return Profile.RandomLevelIndices[loopIndex];
        }

        /// <summary>Resolves chapter addresses against the existing loading catalog without changing authored assets.</summary>
        private int GetCatalogIndex(LevelReference level)
        {
            var index = System.Array.FindIndex(_levelsData.Levels, entry => entry.LevelId == level?.LevelId);
            if (index < 0 || string.IsNullOrWhiteSpace(level?.LevelId))
                throw new System.InvalidOperationException("Chapter level address is missing from LevelsData.Levels.");
            return index;
        }

        /// <summary>Builds the loop's saved permutation without mutating the shared catalog configuration.</summary>
        private void GenerateLoopIndices()
        {
            var startIndex = _levelsData.Levels.Length - _levelsData.LoopLevelCount;
            var indicesList = Enumerable.Range(startIndex, _levelsData.LoopLevelCount).ToList();
            if (_levelsData.RandomizeLoopOrder)
            {
                for (var index = indicesList.Count - 1; index > 0; index--)
                {
                    var otherIndex = Random.Range(0, index + 1);
                    (indicesList[index], indicesList[otherIndex]) = (indicesList[otherIndex], indicesList[index]);
                }
            }
            Profile.RandomLevelIndices = indicesList;
        }
        public GameplayProgressController(SignalBus signalBus) : base(signalBus, "GameplayProgress")
        {
        }

        public int CurrentLevel => Profile.LevelIndex + 1;
        public int CurrentAttempt => Profile.CurrentAttempt;

        public int ActualLevelIndex
        {
            get
            {
                if (_levelsData != null)
                {
                    return GetLevelIndexAtOffset(0);
                }

                return Profile.LevelIndex;
            }
        }

        public int ActualLevelNumber => ActualLevelIndex + 1;

        protected override void OnProfileSet()
        {
        }

        public void SetLevelIndex(int levelIndex)
        {
            Profile.LevelIndex = levelIndex;
            Profile.CurrentAttempt = 1;
            Profile.RandomLevelIndices = null;
        }

        /// <summary>Advances the saved progression step; catalog selection wraps the configured final set after the main sequence.</summary>
        public void HandleLevelCompleted()
        {
            Profile.LevelIndex++;
            Profile.CurrentAttempt = 1;
        }

        public void HandleLevelFailed()
        {
            Profile.CurrentAttempt++;
        }

        protected override GameplayProgressProfile CreateDefaultProfile()
        {
            return new GameplayProgressProfile
            {
                LevelIndex = 0,
                CurrentAttempt = 1,
                RandomLevelIndices = null,
            };
        }

        public void SetAbandonedMissionData(int currentLevel, int currentAttempt, float getLevelProgress)
        {
            Profile.AbandonedMissionData = new AbandonedMissionData
            {
                LevelIndex = currentLevel,
                LevelAttempt = currentAttempt,
                LevelProgress = getLevelProgress,
            };
        }

        public void SetAbandonedMissionData(AbandonedMissionData abandonedMissionData)
        {
            Profile.AbandonedMissionData = abandonedMissionData;
        }

        public void CheckAbandonedMissionData()
        {
            var abandonedMissionData = Profile.AbandonedMissionData;
            if (abandonedMissionData == null)
                return;

            SignalBus.Fire(new Signals.LevelAbandoned
            {
                LevelNumber = abandonedMissionData.LevelIndex + 1,
                LevelAttempt = abandonedMissionData.LevelAttempt,
                LevelProgress = abandonedMissionData.LevelProgress,
            });
            Profile.AbandonedMissionData = null;
        }
    }
}
