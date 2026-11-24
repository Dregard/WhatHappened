namespace Game.AssetManagement.Publishing
{
    /// <summary>Provides publication secrets from local storage outside the repository.</summary>
    public interface IRemoteContentSecretSource
    {
        /// <summary>Returns the SFTP password or an empty string when it is not configured.</summary>
        string GetPassword();

        /// <summary>Returns the trusted SSH host key fingerprint or an empty string when it is not configured.</summary>
        string GetHostKeyFingerprint();
    }
}
