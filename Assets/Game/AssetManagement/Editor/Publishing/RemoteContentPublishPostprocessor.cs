using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Starts publication after the APK/AAB is built. Unity does not call this for failed builds; the gate also checks the result.</summary>
    public sealed class RemoteContentPublishPostprocessor : IPostprocessBuildWithReport
    {
        /// <summary>Runs late so other postprocessors finish first.</summary>
        public int callbackOrder
        {
            get { return 1000; }
        }

        /// <summary>Passes the finished build facts to the publication system.</summary>
        public void OnPostprocessBuild(BuildReport report)
        {
            RemoteContentPublishHooks.FinishRun(report);
        }
    }
}
