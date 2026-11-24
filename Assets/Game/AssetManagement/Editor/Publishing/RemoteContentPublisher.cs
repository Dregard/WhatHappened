using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.AssetManagement.Publishing
{
    /// <summary>
    /// Runs one publication: open session, check the destination, upload bundles, catalog and .hash in order, close the session,
    /// then verify the published .hash over HTTPS. Stops at the first failure and reports it without throwing.
    /// </summary>
    public sealed class RemoteContentPublisher
    {
        private readonly ISftpSessionFactory _sessionFactory;
        private readonly RemoteHashVerifier _verifier;
        private readonly Action<string> _reportProgress;

        /// <summary>Creates a publisher over injectable transport and verification services.</summary>
        public RemoteContentPublisher(ISftpSessionFactory sessionFactory, RemoteHashVerifier verifier, Action<string> reportProgress)
        {
            _sessionFactory = sessionFactory;
            _verifier = verifier;
            _reportProgress = reportProgress;
        }

        /// <summary>Publishes the plan. The result never contains the password; Failed names the stage that stopped the publication.</summary>
        public PublishResultData Publish(SftpConnectionData connection, PublishPlanData plan, RemoteContentPublishData options)
        {
            var uploadedList = new List<string>();
            var stage = PublishStageAlias.Connection;
            try
            {
                using (var session = _sessionFactory.Open(connection))
                {
                    session.ListDirectory(plan.DestinationRoot);
                    uploadedList = new RemoteContentUploader(_reportProgress).Upload(session, plan, Guid.NewGuid().ToString("N").Substring(0, 8));
                }
                stage = PublishStageAlias.Verification;
                foreach (var hashFile in plan.FilesList.Where(file => file.Stage == PublishStageAlias.Hash))
                {
                    _verifier.Verify(hashFile, options.HashCheckAttempts, options.HashCheckDelaySeconds, connection.TimeoutSeconds);
                }
                return PublishResultData.Succeeded(uploadedList);
            }
            catch (RemoteContentPublishException exception)
            {
                return PublishResultData.Failed(exception.Stage == PublishStageAlias.Unknown ? stage : exception.Stage,
                    SecretRedactor.Redact(exception.Message, connection.Password, connection.HostKeyFingerprint), uploadedList);
            }
            catch (Exception exception)
            {
                return PublishResultData.Failed(stage, SecretRedactor.Redact(exception.Message, connection.Password), uploadedList);
            }
        }
    }
}
