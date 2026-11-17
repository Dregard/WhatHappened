using System;
using Cysharp.Threading.Tasks;
using Game.UI.Windows.General;

namespace Game.UI.Windows.TimeLoseWindow
{
    public class TimeLoseWindowModel : IWindowModel
    {
        public Action RestartAction;
        public Action PayAndContinueAction;
        public Func<UniTask<bool>> AdsAction;
        public bool CanPayAndContinue;
        public int CurrencyPriceValue;
    }
}
