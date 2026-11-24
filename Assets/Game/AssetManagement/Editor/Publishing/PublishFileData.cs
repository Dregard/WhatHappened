namespace Game.AssetManagement.Publishing
{
    /// <summary>One local file with its remote destination and public URL.</summary>
    public sealed class PublishFileData
    {
        public string LocalPath { get; private set; }
        public string RemotePath { get; private set; }
        public string Url { get; private set; }
        public PublishStageAlias Stage { get; private set; }

        /// <summary>Creates a file entry; paths are already normalized.</summary>
        public PublishFileData(string localPath, string remotePath, string url, PublishStageAlias stage)
        {
            LocalPath = localPath;
            RemotePath = remotePath;
            Url = url;
            Stage = stage;
        }
    }
}
