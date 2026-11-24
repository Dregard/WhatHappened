using System.Collections.Generic;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Ordered upload plan: bundles, then catalog, then .hash. Contains only files of the current build.</summary>
    public sealed class PublishPlanData
    {
        public string DestinationRoot { get; private set; }
        public IReadOnlyList<PublishFileData> FilesList { get; private set; }

        /// <summary>Creates a plan whose files are already in stage order.</summary>
        public PublishPlanData(string destinationRoot, IReadOnlyList<PublishFileData> filesList)
        {
            DestinationRoot = destinationRoot;
            FilesList = filesList;
        }
    }
}
