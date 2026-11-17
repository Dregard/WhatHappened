using System;
using Game.UI.Windows.General;
using UnityEngine;

namespace Game.UI.Windows.PreloadWindow
{
    public class PreloadWindowModel : ModelParams, IWindowModel, IProgress<float>
    {
        public event Action<float> ProgressChanged;
        public float Progress { get; private set; }

        /// <summary>Publishes monotonic measured stage progress; only Complete may display full readiness. Each preparation owns a fresh model.</summary>
        public void Report(float value)
        {
            if (float.IsNaN(value))
                return;
            var progress = Mathf.Max(Progress, Mathf.Clamp(value, 0f, 0.99f));
            if (progress == Progress)
                return;
            Progress = progress;
            ProgressChanged?.Invoke(Progress);
        }

        /// <summary>Marks readiness after all required preparation has succeeded; failed or cancelled preparation must not call this.</summary>
        public void Complete()
        {
            Progress = 1f;
            ProgressChanged?.Invoke(Progress);
        }
    }
}
