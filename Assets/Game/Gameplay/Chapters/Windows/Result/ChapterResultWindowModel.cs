using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.UI.Windows.General;
using UnityEngine;

namespace Game.Gameplay.Chapters
{
    /// <summary>Everything the single chapter result window needs: the optional new evidence, then either the level reward or the chapter finale. Progress and reward effects are supplied callbacks owned by the chapter system.</summary>
    public sealed class ChapterResultWindowModel : IWindowModel
    {
        public bool HasEvidence;
        public int NewClueIndex;
        /// <summary>Row slot of the evidence the level reward emerges from.</summary>
        public int LastFoundIndex;
        public Sprite NewClue;
        public string NewClueName;
        public bool IsFinal;
        public Func<Sprite[]> GetClues;
        public Func<UniTask> CommitClue;
        public List<ChapterRewardModel> LevelRewardsList = new();
        public Func<UniTask> ClaimLevelReward;
        public Sprite CulpritPortrait;
        public string CulpritCaption;
        public Sprite CulpritSceneImage;
        public string CulpritSceneCaption;
        public string ChapterRewardHeading;
        public List<ChapterRewardModel> ChapterRewardsList = new();
        public Func<UniTask> ClaimChapterReward;
        public float FlightDuration;
        public float ConvergenceDuration;
        public float RevealDuration;
        public float SunburstTurnSeconds;
        public float FadeSeconds;
        public Action ContinueAction;
    }
}
