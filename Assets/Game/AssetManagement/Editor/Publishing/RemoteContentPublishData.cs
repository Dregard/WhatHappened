using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Non-secret, Git-shareable publication settings. The password and the trusted SSH fingerprint are never stored here.</summary>
    [Serializable]
    public sealed class RemoteContentPublishData
    {
        [Tooltip("SFTP server host name. Changing it points publication to another server; the trusted SSH fingerprint must match the new host.")]
        [SerializeField] private string _hostName = "vh101.hoster.by";

        [Tooltip("SFTP port, normally 22. Valid range 1-65535.")]
        [SerializeField, Range(1, 65535)] private int _portNumber = 22;

        [Tooltip("SFTP login. The password is read from the local secret store or the environment variable, never from this file.")]
        [SerializeField] private string _userName = "h140875";

        [Tooltip("Remote root folder exactly as shown in the FileZilla 'Remote site' line while it is opened in the RemoteContent folder, for example /www/sunstormstudio.com/RemoteContent/{project name}. It is the source of truth; segments shared with Remote.LoadPath are not duplicated. Empty value blocks publication.")]
        [SerializeField] private string _destinationRoot = string.Empty;

        [Tooltip("Folder containing WinSCPnet.dll and WinSCP.exe (a normal WinSCP installation). Empty value searches the standard install locations.")]
        [SerializeField] private string _winScpFolder = string.Empty;

        [Tooltip("Seconds to wait for the SFTP server to answer while opening a session and during transfers. Larger values tolerate slow links.")]
        [SerializeField, Min(5)] private int _connectionTimeoutSeconds = 30;

        [Tooltip("How many times the published .hash is downloaded over HTTPS before the publication is reported incomplete. 1 means a single check.")]
        [SerializeField, Min(1)] private int _hashCheckAttempts = 3;

        [Tooltip("Seconds to wait between .hash download attempts, giving the web server time to serve the new file. 0 disables the wait.")]
        [SerializeField, Min(0f)] private float _hashCheckDelaySeconds = 3f;

        [Tooltip("GUIDs of Build Profiles that publish remote content automatically after a successful Android APK/AAB build. Profiles that are not listed never publish.")]
        [SerializeField] private List<string> _enabledProfileGuidsList = new List<string>();

        public string HostName { get { return _hostName; } }
        public int PortNumber { get { return _portNumber; } }
        public string UserName { get { return _userName; } }
        public string DestinationRoot { get { return _destinationRoot; } }
        public string WinScpFolder { get { return _winScpFolder; } }
        public int ConnectionTimeoutSeconds { get { return _connectionTimeoutSeconds; } }
        public int HashCheckAttempts { get { return _hashCheckAttempts; } }
        public float HashCheckDelaySeconds { get { return _hashCheckDelaySeconds; } }
        public IReadOnlyList<string> EnabledProfileGuidsList { get { return _enabledProfileGuidsList; } }

        /// <summary>Returns whether the profile with this GUID has explicitly enabled automatic publication.</summary>
        public bool IsProfileEnabled(string profileGuid)
        {
            return !string.IsNullOrEmpty(profileGuid) && _enabledProfileGuidsList.Contains(profileGuid);
        }

        /// <summary>Creates settings for tests and tools; the same values are serialized by the project settings asset.</summary>
        public static RemoteContentPublishData Create(string hostName, int portNumber, string userName, string destinationRoot,
            IEnumerable<string> enabledProfileGuids, int hashCheckAttempts, float hashCheckDelaySeconds)
        {
            var data = new RemoteContentPublishData
            {
                _hostName = hostName,
                _portNumber = portNumber,
                _userName = userName,
                _destinationRoot = destinationRoot,
                _hashCheckAttempts = hashCheckAttempts,
                _hashCheckDelaySeconds = hashCheckDelaySeconds
            };
            data._enabledProfileGuidsList.AddRange(enabledProfileGuids ?? new string[0]);
            return data;
        }
    }
}
