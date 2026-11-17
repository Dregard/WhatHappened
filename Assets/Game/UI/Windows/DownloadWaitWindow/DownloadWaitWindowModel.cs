using System;
using Game.UI.Windows.General;

namespace Game.UI.Windows.DownloadWaitWindow
{
    public class DownloadWaitWindowModel : IWindowModel
    {
        public Action RetryAction { get; set; }
    }
}
