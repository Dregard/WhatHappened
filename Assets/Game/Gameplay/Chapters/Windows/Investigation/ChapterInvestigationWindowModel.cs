using System;
using Game.UI.Windows.General;
using UnityEngine;

namespace Game.Gameplay.Chapters
{
    /// <summary>Found evidence of the current chapter (null for unfound slots) and the close action.</summary>
    public sealed class ChapterInvestigationWindowModel : IWindowModel
    {
        public Sprite[] CluesArray;
        public Action CloseAction;
    }
}
