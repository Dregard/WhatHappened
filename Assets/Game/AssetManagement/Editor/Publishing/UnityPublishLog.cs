using UnityEngine;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Writes publication messages to the Unity Console with a common prefix.</summary>
    public sealed class UnityPublishLog : IPublishLog
    {
        private const string Prefix = "[RemoteContentPublish] ";

        /// <summary>Logs an informational message.</summary>
        public void Info(string message)
        {
            Debug.Log(Prefix + message);
        }

        /// <summary>Logs a warning.</summary>
        public void Warning(string message)
        {
            Debug.LogWarning(Prefix + message);
        }

        /// <summary>Logs an error.</summary>
        public void Error(string message)
        {
            Debug.LogError(Prefix + message);
        }
    }
}
