using System;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;

namespace Game.AssetManagement.Publishing
{
    /// <summary>State of one tracked player build: what started it and which Addressables build finished inside it.</summary>
    public sealed class PublishRunData
    {
        public BuildTarget Target { get; private set; }
        public string OutputPath { get; private set; }
        public string ProfileGuid { get; private set; }
        public bool IsGradleExport { get; private set; }
        public DateTime StartedUtc { get; private set; }
        public AddressablesPlayerBuildResult AddressablesResult { get; private set; }

        /// <summary>Starts a run description at the beginning of a player build, before the Addressables player processor.</summary>
        public PublishRunData(BuildTarget target, string outputPath, string profileGuid, bool isGradleExport, DateTime startedUtc)
        {
            Target = target;
            OutputPath = outputPath ?? string.Empty;
            ProfileGuid = profileGuid ?? string.Empty;
            IsGradleExport = isGradleExport;
            StartedUtc = startedUtc;
        }

        /// <summary>Stores the latest Addressables player build result that finished during this run (a later build replaces an earlier one).</summary>
        public void SetAddressablesResult(AddressablesPlayerBuildResult result)
        {
            AddressablesResult = result;
        }
    }
}
