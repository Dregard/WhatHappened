using System.Collections.Generic;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Remote output of one Addressables build: where remote content goes and which files the build itself produced.</summary>
    public sealed class AddressablesOutputData
    {
        public IReadOnlyList<OutputLocationData> LocationsList { get; private set; }
        public IReadOnlyList<string> BuiltFilesList { get; private set; }

        /// <summary>Creates output data from evaluated profile locations and the build's own file registry.</summary>
        public AddressablesOutputData(IReadOnlyList<OutputLocationData> locationsList, IReadOnlyList<string> builtFilesList)
        {
            LocationsList = locationsList;
            BuiltFilesList = builtFilesList;
        }
    }
}
