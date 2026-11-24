using System.Collections.Generic;

namespace Game.AssetManagement.Publishing.Tests
{
    /// <summary>Collects log lines so tests can assert their content.</summary>
    public sealed class RecordingPublishLog : IPublishLog
    {
        public readonly List<string> InfoList = new List<string>();
        public readonly List<string> WarningList = new List<string>();
        public readonly List<string> ErrorList = new List<string>();

        /// <summary>Records an info line.</summary>
        public void Info(string message)
        {
            InfoList.Add(message);
        }

        /// <summary>Records a warning line.</summary>
        public void Warning(string message)
        {
            WarningList.Add(message);
        }

        /// <summary>Records an error line.</summary>
        public void Error(string message)
        {
            ErrorList.Add(message);
        }
    }
}
