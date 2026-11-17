using System;
using Cysharp.Threading.Tasks;
using Game.UI.Windows.General;

namespace Game.UI.Windows.MenuWindow
{
    public class MenuWindowModel : IWindowModel
    {
        public Func<UniTask> StartGameAction;
    }
}