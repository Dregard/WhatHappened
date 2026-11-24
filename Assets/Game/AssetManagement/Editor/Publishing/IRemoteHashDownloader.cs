namespace Game.AssetManagement.Publishing
{
    /// <summary>Downloads the published .hash text from its public HTTPS address.</summary>
    public interface IRemoteHashDownloader
    {
        /// <summary>Returns the file text; throws on transport errors, non-success status or a non-HTTPS URL.</summary>
        string Download(string url, int timeoutSeconds);
    }
}
