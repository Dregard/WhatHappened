using System;

namespace Game.AssetManagement.Publishing.Tests
{
    /// <summary>Returns a shared fake session, or throws a configured error to simulate a wrong SSH fingerprint.</summary>
    public sealed class FakeSftpSessionFactory : ISftpSessionFactory
    {
        public readonly FakeSftpSession Session = new FakeSftpSession();
        public string OpenError = string.Empty;
        public int OpenCount;

        /// <summary>Counts the open attempt and returns the shared session.</summary>
        public ISftpSession Open(SftpConnectionData connection)
        {
            OpenCount++;
            if (!string.IsNullOrEmpty(OpenError))
            {
                throw new RemoteContentPublishException(PublishStageAlias.Connection, OpenError);
            }
            return Session;
        }
    }
}
