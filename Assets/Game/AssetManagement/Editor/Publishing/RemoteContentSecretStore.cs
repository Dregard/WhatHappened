using System;
using System.IO;
using UnityEngine;

namespace Game.AssetManagement.Publishing
{
    /// <summary>
    /// Reads secrets from environment variables first, then from a per-user file outside the project
    /// (%APPDATA%/WhatHappened/RemoteContentPublish.secrets.json). The file is plain text protected only by the user profile ACL.
    /// </summary>
    public sealed class RemoteContentSecretStore : IRemoteContentSecretSource
    {
        public const string PasswordVariableName = "WHATHAPPENED_SFTP_PASSWORD";
        public const string FingerprintVariableName = "WHATHAPPENED_SFTP_HOST_KEY";

        [Serializable]
        private sealed class SecretFileData
        {
            public string Password = string.Empty;
            public string HostKeyFingerprint = string.Empty;
        }

        private readonly Func<string, string> _readVariable;
        private readonly string _filePath;

        /// <summary>Creates a store over the real environment and the per-user file.</summary>
        public RemoteContentSecretStore() : this(Environment.GetEnvironmentVariable, GetDefaultFilePath())
        {
        }

        /// <summary>Creates a store over injected sources; used by tests so no real user data is touched.</summary>
        public RemoteContentSecretStore(Func<string, string> readVariable, string filePath)
        {
            _readVariable = readVariable;
            _filePath = filePath;
        }

        public string FilePath { get { return _filePath; } }

        /// <summary>Returns the per-user secrets file path, located outside the Unity project and Git.</summary>
        public static string GetDefaultFilePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WhatHappened", "RemoteContentPublish.secrets.json");
        }

        /// <summary>Returns the password from the environment variable, else from the local file, else an empty string.</summary>
        public string GetPassword()
        {
            var fromEnvironment = _readVariable(PasswordVariableName);
            return string.IsNullOrEmpty(fromEnvironment) ? ReadFile().Password : fromEnvironment;
        }

        /// <summary>Returns the trusted host key fingerprint from the environment variable, else from the local file, else an empty string.</summary>
        public string GetHostKeyFingerprint()
        {
            var fromEnvironment = _readVariable(FingerprintVariableName);
            return string.IsNullOrEmpty(fromEnvironment) ? ReadFile().HostKeyFingerprint : fromEnvironment.Trim();
        }

        /// <summary>Stores the password in the local file. An empty value clears it.</summary>
        public void SavePassword(string password)
        {
            var data = ReadFile();
            data.Password = password ?? string.Empty;
            WriteFile(data);
        }

        /// <summary>Stores the trusted host key fingerprint in the local file. An empty value clears it.</summary>
        public void SaveHostKeyFingerprint(string fingerprint)
        {
            var data = ReadFile();
            data.HostKeyFingerprint = (fingerprint ?? string.Empty).Trim();
            WriteFile(data);
        }

        /// <summary>Describes where each secret currently comes from, without revealing values.</summary>
        public string DescribeSources()
        {
            return $"Password: {DescribeSource(PasswordVariableName, ReadFile().Password)}; SSH fingerprint: {DescribeSource(FingerprintVariableName, ReadFile().HostKeyFingerprint)}.";
        }

        /// <summary>Names the source of one secret: environment variable, local file or not set.</summary>
        private string DescribeSource(string variableName, string fileValue)
        {
            if (!string.IsNullOrEmpty(_readVariable(variableName)))
            {
                return "environment variable " + variableName;
            }
            return string.IsNullOrEmpty(fileValue) ? "NOT SET" : "local file";
        }

        /// <summary>Reads the secrets file; a missing or unreadable file yields empty values.</summary>
        private SecretFileData ReadFile()
        {
            try
            {
                if (!string.IsNullOrEmpty(_filePath) && File.Exists(_filePath))
                {
                    return JsonUtility.FromJson<SecretFileData>(File.ReadAllText(_filePath)) ?? new SecretFileData();
                }
            }
            catch (Exception)
            {
                // Corrupt secret files are treated as unset; the validation message tells the user what to configure.
            }
            return new SecretFileData();
        }

        /// <summary>Writes the secrets file, creating its per-user folder when needed.</summary>
        private void WriteFile(SecretFileData data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
            File.WriteAllText(_filePath, JsonUtility.ToJson(data));
        }
    }
}
