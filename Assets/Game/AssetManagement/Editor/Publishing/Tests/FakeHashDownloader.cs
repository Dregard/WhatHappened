using System;
using System.Collections.Generic;

namespace Game.AssetManagement.Publishing.Tests
{
    /// <summary>Serves configured .hash texts without any HTTP traffic.</summary>
    public sealed class FakeHashDownloader : IRemoteHashDownloader
    {
        public readonly Dictionary<string, string> TextByUrl = new Dictionary<string, string>();
        public readonly List<string> RequestedUrlsList = new List<string>();

        /// <summary>Returns the configured text or throws like a missing resource.</summary>
        public string Download(string url, int timeoutSeconds)
        {
            RequestedUrlsList.Add(url);
            if (!TextByUrl.TryGetValue(url, out var text))
            {
                throw new InvalidOperationException("404");
            }
            return text;
        }
    }
}
