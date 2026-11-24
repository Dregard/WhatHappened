namespace Game.AssetManagement.Publishing
{
    /// <summary>Pairs a remote group's or catalog's local build folder with its public load URL from the active profile.</summary>
    public sealed class OutputLocationData
    {
        public string LocalDirectory { get; private set; }
        public string LoadUrl { get; private set; }

        /// <summary>Creates a location; the directory is a normalized absolute path with forward slashes and no trailing slash.</summary>
        public OutputLocationData(string localDirectory, string loadUrl)
        {
            LocalDirectory = localDirectory;
            LoadUrl = loadUrl;
        }
    }
}
