using UnityEditor.AddressableAssets.Build;

namespace Game.AssetManagement.Publishing
{
    /// <summary>
    /// Holds at most one open run. A new player build replaces any leftover run of a failed or cancelled build, so outputs of
    /// earlier runs can never be attached to a later build. Taking the run closes it.
    /// </summary>
    public sealed class PublishRunTracker
    {
        private PublishRunData _currentRun;

        /// <summary>Opens a run, discarding any previous unfinished run.</summary>
        public void Begin(PublishRunData run)
        {
            _currentRun = run;
        }

        /// <summary>Attaches an Addressables player build result to the open run; results outside a run (menu builds, Play Mode) are ignored.</summary>
        public void RegisterAddressablesResult(AddressablesPlayerBuildResult result)
        {
            if (_currentRun != null)
            {
                _currentRun.SetAddressablesResult(result);
            }
        }

        /// <summary>Returns the open run and closes it; returns null when no player build was tracked.</summary>
        public PublishRunData Take()
        {
            var run = _currentRun;
            _currentRun = null;
            return run;
        }
    }
}
