using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.UI.Windows.General;
using UnityEngine;

namespace Game.Gameplay.Chapters
{
    public sealed class ChapterWindowModel : IWindowModel
    {
        public ChapterWindowModeAlias Mode;
        public string ChapterTitle;
        public Sprite[] CluesArray;
        public int NewClueIndex;
        public Sprite NewClue;
        public string NewClueName;
        public bool IsFinal;
        public List<ChapterRewardModel> LevelRewardsList = new();
        public List<ChapterRewardModel> ChapterRewardsList = new();
        public Sprite CulpritPortrait;
        public string CulpritCaption;
        public Sprite CulpritSceneImage;
        public string CulpritSceneCaption;
        public Func<UniTask> CommitClue;
        public Func<UniTask> ClaimLevelReward;
        public Func<UniTask> ClaimChapterReward;
        public Action CloseAction;
        public Action FinishAction;
        public Game.Levels.LevelsData PresentationData;
    }
}
