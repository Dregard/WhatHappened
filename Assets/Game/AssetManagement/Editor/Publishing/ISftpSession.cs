using System;

namespace Game.AssetManagement.Publishing
{
    /// <summary>An open SFTP session. Every method throws on failure with a message that must not contain secrets.</summary>
    public interface ISftpSession : IDisposable
    {
        /// <summary>Lists a remote folder without changing anything; proves that the folder is browsable.</summary>
        void ListDirectory(string remotePath);

        /// <summary>Returns whether a remote file or folder exists.</summary>
        bool PathExists(string remotePath);

        /// <summary>Creates one remote folder whose parent exists.</summary>
        void CreateDirectory(string remotePath);

        /// <summary>Transfers a local file to the exact remote file path.</summary>
        void UploadFile(string localPath, string remotePath);

        /// <summary>Renames a remote file; fails when the target already exists on servers without overwrite support.</summary>
        void MoveFile(string sourcePath, string targetPath);

        /// <summary>Deletes one remote file. The publisher uses it only for its own temporary files and to replace a same-named target.</summary>
        void DeleteFile(string remotePath);
    }
}
