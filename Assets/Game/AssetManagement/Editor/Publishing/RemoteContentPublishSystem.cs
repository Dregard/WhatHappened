using System;
using UnityEditor.AddressableAssets.Build;

namespace Game.AssetManagement.Publishing
{
    /// <summary>
    /// Entry point of automatic publication. After a player build it checks the gate, resolves the current build's remote output
    /// and publishes it. It never builds Addressables itself and never throws into the build pipeline: a failure is logged as
    /// «APK/AAB created, but remote content was not published».
    /// </summary>
    public sealed class RemoteContentPublishSystem
    {
        private readonly RemoteContentPublishData _data;
        private readonly IRemoteContentSecretSource _secrets;
        private readonly RemoteContentPublisher _publisher;
        private readonly Func<AddressablesPlayerBuildResult, AddressablesOutputData> _resolveOutput;
        private readonly Func<string, bool> _fileExists;
        private readonly IPublishLog _log;

        /// <summary>Wires the publication collaborators; the output resolver is injected so tests need no real Addressables settings.</summary>
        public RemoteContentPublishSystem(RemoteContentPublishData data, IRemoteContentSecretSource secrets, RemoteContentPublisher publisher,
            Func<AddressablesPlayerBuildResult, AddressablesOutputData> resolveOutput, Func<string, bool> fileExists, IPublishLog log)
        {
            _data = data;
            _secrets = secrets;
            _publisher = publisher;
            _resolveOutput = resolveOutput;
            _fileExists = fileExists;
            _log = log;
        }

        /// <summary>
        /// Handles a finished player build. Returns Skipped when the gate blocks publication, otherwise Succeeded or Failed.
        /// A failure is reported through the log as an incomplete publication and never as a build failure.
        /// </summary>
        public PublishResultData OnPlayerBuilt(PublishRunData run, PublishBuildSnapshot build)
        {
            var gate = RemoteContentPublishGate.Evaluate(run, build, _data);
            if (!gate.ShouldPublish)
            {
                var skipText = "Публикация удалённого контента пропущена: " + gate.Reason;
                if (gate.IsWarning)
                {
                    _log.Warning(skipText);
                }
                else
                {
                    _log.Info(skipText);
                }
                return PublishResultData.Skipped(gate.Reason);
            }
            var connection = SftpConnectionData.Create(_data, _secrets);
            try
            {
                var problemsList = connection.FindProblems();
                if (problemsList.Count > 0)
                {
                    throw new RemoteContentPublishException(PublishStageAlias.Configuration, string.Join(" ", problemsList));
                }
                var output = _resolveOutput(run.AddressablesResult);
                var plan = new RemoteContentPlanBuilder(_fileExists).Build(output, connection.DestinationRoot);
                _log.Info($"Публикация удалённого контента на {connection}: файлов {plan.FilesList.Count}, корень {plan.DestinationRoot}.");
                var result = _publisher.Publish(connection, plan, _data);
                ReportResult(result, build.OutputPath);
                return result;
            }
            catch (Exception exception)
            {
                var stage = exception is RemoteContentPublishException typed ? typed.Stage : PublishStageAlias.Configuration;
                var failed = PublishResultData.Failed(stage, SecretRedactor.Redact(exception.Message, connection.Password, connection.HostKeyFingerprint), new string[0]);
                ReportResult(failed, build.OutputPath);
                return failed;
            }
        }

        /// <summary>Logs success, or an explicit statement that the build artifact exists while publication did not complete.</summary>
        private void ReportResult(PublishResultData result, string outputPath)
        {
            if (result.Status == PublishStatusAlias.Succeeded)
            {
                _log.Info($"Публикация удалённого контента завершена: загружено файлов {result.UploadedRemotePathsList.Count}, .hash на сервере совпадает с локальным.");
                return;
            }
            _log.Error($"APK/AAB создан ({outputPath}), НО публикация удалённого контента НЕ завершена (этап {result.Stage}): {result.Message} " +
                "Содержимое на сервере может быть неполным или устаревшим; повторите публикацию после исправления причины.");
        }
    }
}
