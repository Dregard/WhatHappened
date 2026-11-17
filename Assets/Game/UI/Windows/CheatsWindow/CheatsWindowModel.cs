using System;
using Game.Boosters;
using Game.UI.Windows.General;

namespace Game.UI.Windows.CheatsWindow
{
    public class CheatsWindowModel : IWindowModel
    {
        public Action<int> GoToLevelAction;
        public Action<int> AddMoneyAction;
        public Action<BoosterKind, int> AddBoostersAction;
        public Action CloseAction;
    }
}
