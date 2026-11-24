using UnityEditor.Build;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Starts a tracked run for every player build. Runs before the Addressables processor (order 1), so its build result joins the run.</summary>
    public sealed class RemoteContentPublishPlayerProcessor : BuildPlayerProcessor
    {
        /// <summary>Order 0 is earlier than AddressablesPlayerBuildProcessor (order 1).</summary>
        public override int callbackOrder
        {
            get { return 0; }
        }

        /// <summary>Records the target, requested output path and active Build Profile; changes nothing in the build.</summary>
        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            var options = buildPlayerContext.BuildPlayerOptions;
            RemoteContentPublishHooks.BeginRun(options.target, options.locationPathName);
        }
    }
}
