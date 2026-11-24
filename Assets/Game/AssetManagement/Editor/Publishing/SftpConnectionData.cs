using System.Collections.Generic;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Resolved SFTP connection parameters. ToString never exposes the password or fingerprint.</summary>
    public sealed class SftpConnectionData
    {
        public string HostName { get; private set; }
        public int PortNumber { get; private set; }
        public string UserName { get; private set; }
        public string Password { get; private set; }
        public string HostKeyFingerprint { get; private set; }
        public string DestinationRoot { get; private set; }
        public string WinScpFolder { get; private set; }
        public int TimeoutSeconds { get; private set; }

        /// <summary>Creates connection data from explicit values.</summary>
        public SftpConnectionData(string hostName, int portNumber, string userName, string password, string hostKeyFingerprint,
            string destinationRoot, string winScpFolder, int timeoutSeconds)
        {
            HostName = hostName ?? string.Empty;
            PortNumber = portNumber;
            UserName = userName ?? string.Empty;
            Password = password ?? string.Empty;
            HostKeyFingerprint = hostKeyFingerprint ?? string.Empty;
            DestinationRoot = RemoteContentPathMapper.NormalizeRoot(destinationRoot);
            WinScpFolder = winScpFolder ?? string.Empty;
            TimeoutSeconds = timeoutSeconds;
        }

        /// <summary>Combines project settings with locally stored secrets.</summary>
        public static SftpConnectionData Create(RemoteContentPublishData data, IRemoteContentSecretSource secrets)
        {
            return new SftpConnectionData(data.HostName, data.PortNumber, data.UserName, secrets.GetPassword(),
                secrets.GetHostKeyFingerprint(), data.DestinationRoot, data.WinScpFolder, data.ConnectionTimeoutSeconds);
        }

        /// <summary>Lists every missing or invalid setting as a user-readable line. An empty list means the connection can be attempted.</summary>
        public List<string> FindProblems()
        {
            var problemsList = new List<string>();
            if (string.IsNullOrWhiteSpace(HostName))
            {
                problemsList.Add("Не задан сервер SFTP (Project Settings > Remote Content Publishing).");
            }
            if (PortNumber < 1 || PortNumber > 65535)
            {
                problemsList.Add("Порт SFTP должен быть в диапазоне 1-65535.");
            }
            if (string.IsNullOrWhiteSpace(UserName))
            {
                problemsList.Add("Не задан логин SFTP.");
            }
            if (string.IsNullOrEmpty(DestinationRoot))
            {
                problemsList.Add("Не задан путь назначения: впишите путь из строки «Удалённый сайт» FileZilla (папка RemoteContent) в настройках.");
            }
            if (string.IsNullOrEmpty(Password))
            {
                problemsList.Add($"Не задан пароль SFTP: укажите его в переменной окружения {RemoteContentSecretStore.PasswordVariableName} или в локальном хранилище секретов настроек.");
            }
            if (string.IsNullOrWhiteSpace(HostKeyFingerprint))
            {
                problemsList.Add($"Не задан доверенный отпечаток SSH-ключа сервера: укажите его в переменной окружения {RemoteContentSecretStore.FingerprintVariableName} или в локальном хранилище секретов настроек. Неизвестный ключ автоматически не принимается.");
            }
            return problemsList;
        }

        /// <summary>Returns a secret-free description such as login@host:port.</summary>
        public override string ToString()
        {
            return $"{UserName}@{HostName}:{PortNumber}";
        }
    }
}
