using System;
using System.IO;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Compares the local .hash with the one served over HTTPS. A mismatch or download error means the publication is incomplete.</summary>
    public sealed class RemoteHashVerifier
    {
        private readonly IRemoteHashDownloader _downloader;
        private readonly Action<float> _wait;

        /// <summary>Creates a verifier; the wait callback sleeps between attempts and is replaced in tests.</summary>
        public RemoteHashVerifier(IRemoteHashDownloader downloader, Action<float> wait)
        {
            _downloader = downloader;
            _wait = wait;
        }

        /// <summary>Throws a Verification-stage exception unless one attempt returns text equal to the local file (ignoring surrounding whitespace).</summary>
        public void Verify(PublishFileData hashFile, int attempts, float delaySeconds, int timeoutSeconds)
        {
            var local = File.ReadAllText(hashFile.LocalPath).Trim();
            var lastProblem = string.Empty;
            for (var attempt = 1; attempt <= Math.Max(1, attempts); attempt++)
            {
                try
                {
                    var remote = _downloader.Download(hashFile.Url, timeoutSeconds).Trim();
                    if (string.Equals(local, remote, StringComparison.Ordinal))
                    {
                        return;
                    }
                    lastProblem = "опубликованный .hash не совпадает с локальным";
                }
                catch (Exception exception)
                {
                    lastProblem = "не удалось скачать .hash: " + exception.Message;
                }
                if (attempt < attempts)
                {
                    _wait(delaySeconds);
                }
            }
            throw new RemoteContentPublishException(PublishStageAlias.Verification, $"Проверка {hashFile.Url}: {lastProblem}.");
        }
    }
}
