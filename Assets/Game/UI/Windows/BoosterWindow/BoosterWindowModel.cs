using System;
using Cysharp.Threading.Tasks;
using Game.Boosters;
using Game.UI.Windows.General;

namespace Game.UI.Windows.BoosterWindow
{
    public class BoosterWindowModel : IWindowModel
    {
        public Action BuyBoosterAction;
        public Action CloseAction;
        public Func<UniTask<bool>> AdsAction;
        public bool CanBuyBooster;
        public BoosterViewModel BoosterViewModel;
    }
}