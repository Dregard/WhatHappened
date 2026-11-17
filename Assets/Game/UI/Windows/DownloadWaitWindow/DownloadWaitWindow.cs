using System;
using Game.UI.Buttons;
using Game.UI.Windows.General;
using UnityEngine;

namespace Game.UI.Windows.DownloadWaitWindow
{
    public class DownloadWaitWindow : Window<DownloadWaitWindowModel>
    {
        [SerializeField, Tooltip("Button that requests another download attempt. Disabled after clicking until this waiting window closes.")]
        private GameButton _retryButton;
        private Action _retryAction;

        /// <summary>Displays the supplied retry interaction; the owning content system performs the download.</summary>
        protected override void Setup(DownloadWaitWindowModel model)
        {
            _retryAction = model.RetryAction;
            _retryButton.ClickEvent -= HandleRetry;
            _retryButton.ClickEvent += HandleRetry;
            _retryButton.SetInteractable(true);
        }

        /// <summary>Allows one retry request and lets the presenter close the window before downloading again.</summary>
        private void HandleRetry()
        {
            _retryButton.SetInteractable(false);
            _retryAction?.Invoke();
        }

        /// <summary>Removes the view's button subscription and callback when the window is destroyed.</summary>
        private void OnDestroy()
        {
            _retryButton.ClickEvent -= HandleRetry;
            _retryAction = null;
        }
    }
}
