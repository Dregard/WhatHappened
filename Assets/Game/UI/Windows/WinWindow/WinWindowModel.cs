using System;
using Game.UI.Windows.General;

namespace Game.UI.Windows.WinWindow
{
    public class WinWindowModel : IWindowModel
    {
        public Action ContinueAction;
        public int CurrentLevel;
        public Game.Levels.LevelAttemptStatisticsModel Statistics;
    }
}
