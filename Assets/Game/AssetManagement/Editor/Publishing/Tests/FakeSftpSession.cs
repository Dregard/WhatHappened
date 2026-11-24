using System;
using System.Collections.Generic;
using System.IO;

namespace Game.AssetManagement.Publishing.Tests
{
    /// <summary>In-memory SFTP server session that records every operation. Opens no sockets.</summary>
    public sealed class FakeSftpSession : ISftpSession
    {
        public readonly List<string> OperationsList = new List<string>();
        public readonly Dictionary<string, string> FilesByPath = new Dictionary<string, string>();
        public readonly HashSet<string> DirectoriesSet = new HashSet<string> { "/" };
        public string FailUploadContaining = string.Empty;
        public bool RenameFailsWhenTargetExists;
        public bool Disposed;

        /// <summary>Records a listing and fails when the folder is unknown.</summary>
        public void ListDirectory(string remotePath)
        {
            OperationsList.Add("list " + remotePath);
            if (!DirectoriesSet.Contains(remotePath))
            {
                throw new InvalidOperationException("Папка не найдена: " + remotePath);
            }
        }

        /// <summary>Returns whether a fake file or folder exists.</summary>
        public bool PathExists(string remotePath)
        {
            return FilesByPath.ContainsKey(remotePath) || DirectoriesSet.Contains(remotePath);
        }

        /// <summary>Creates a fake folder.</summary>
        public void CreateDirectory(string remotePath)
        {
            OperationsList.Add("mkdir " + remotePath);
            DirectoriesSet.Add(remotePath);
        }

        /// <summary>Copies the local text into the fake file system, or fails for configured names.</summary>
        public void UploadFile(string localPath, string remotePath)
        {
            OperationsList.Add("upload " + remotePath);
            if (!string.IsNullOrEmpty(FailUploadContaining) && remotePath.Contains(FailUploadContaining))
            {
                throw new IOException("Сбой передачи");
            }
            FilesByPath[remotePath] = File.ReadAllText(localPath);
        }

        /// <summary>Renames a fake file, optionally refusing to overwrite like a strict SFTP v3 server.</summary>
        public void MoveFile(string sourcePath, string targetPath)
        {
            OperationsList.Add("move " + sourcePath + " -> " + targetPath);
            if (RenameFailsWhenTargetExists && FilesByPath.ContainsKey(targetPath))
            {
                throw new IOException("Целевой файл существует");
            }
            FilesByPath[targetPath] = FilesByPath[sourcePath];
            FilesByPath.Remove(sourcePath);
        }

        /// <summary>Deletes a fake file.</summary>
        public void DeleteFile(string remotePath)
        {
            OperationsList.Add("delete " + remotePath);
            FilesByPath.Remove(remotePath);
        }

        /// <summary>Marks the session closed.</summary>
        public void Dispose()
        {
            Disposed = true;
        }
    }
}
