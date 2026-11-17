using System;
using Game.Boosters;
using Game.UI.Windows.General;

namespace Game.UI.Windows.BoosterUnlockWindow
{
    public class BoosterUnlockWindowModel : IWindowModel
    {
        public Action CloseAction;
        public BoosterViewModel BoosterViewModel;
    }
}
