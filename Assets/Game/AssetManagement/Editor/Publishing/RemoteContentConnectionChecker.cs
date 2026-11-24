using System;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Implements «Проверить подключение»: opens a session and lists the destination folder. It never writes, creates, renames or deletes.</summary>
    public sealed class RemoteContentConnectionChecker
    {
        private readonly ISftpSessionFactory _sessionFactory;

        /// <summary>Creates a checker over the injectable session factory.</summary>
        public RemoteContentConnectionChecker(ISftpSessionFactory sessionFactory)
        {
            _sessionFactory = sessionFactory;
        }

        /// <summary>Returns a user-readable result line; Succeeded is false for missing settings, a wrong fingerprint or an unreadable folder.</summary>
        public PublishResultData Check(SftpConnectionData connection)
        {
            var problemsList = connection.FindProblems();
            if (problemsList.Count > 0)
            {
                return PublishResultData.Failed(PublishStageAlias.Configuration, string.Join(Environment.NewLine, problemsList), new string[0]);
            }
            try
            {
                using (var session = _sessionFactory.Open(connection))
                {
                    session.ListDirectory(connection.DestinationRoot);
                }
                return PublishResultData.Succeeded(new string[0]);
            }
            catch (Exception exception)
            {
                var stage = exception is RemoteContentPublishException typed && typed.Stage != PublishStageAlias.Unknown ? typed.Stage : PublishStageAlias.Connection;
                return PublishResultData.Failed(stage,
                    SecretRedactor.Redact(exception.Message, connection.Password, connection.HostKeyFingerprint), new string[0]);
            }
        }
    }
}
