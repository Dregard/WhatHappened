using Game.UI.Windows.General;

namespace Game.UI.Windows.DownloadWaitWindow
{
    public class DownloadWaitWindowModelProcessor : WindowModelProcessorBase<DownloadWaitWindowModel, DownloadWaitWindowModelParams>
    {
        private DownloadWaitWindowModelParams _modelParams;

        /// <summary>Receives the current content waiter's callback from the existing window system.</summary>
        public override void SetParams(DownloadWaitWindowModelParams modelParams)
        {
            _modelParams = modelParams;
        }

        /// <summary>Creates passive view state for this specific download attempt.</summary>
        public override DownloadWaitWindowModel GetModel()
        {
            return new DownloadWaitWindowModel { RetryAction = _modelParams.RetryAction };
        }
    }
}
