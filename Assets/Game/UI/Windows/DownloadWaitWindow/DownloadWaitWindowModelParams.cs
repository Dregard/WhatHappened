using System;
using Game.UI.Windows.General;

namespace Game.UI.Windows.DownloadWaitWindow
{
    public class DownloadWaitWindowModelParams : ModelParams
    {
        public Action RetryAction { get; set; }
    }
}
