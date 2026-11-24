namespace Game.AssetManagement.Publishing
{
    /// <summary>Opens SFTP sessions; replaced by an in-memory fake in tests.</summary>
    public interface ISftpSessionFactory
    {
        /// <summary>Opens a session that verifies the configured SSH host key fingerprint. Never accepts an unknown key.</summary>
        ISftpSession Open(SftpConnectionData connection);
    }
}
