using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Game.AssetManagement.Publishing
{
    /// <summary>
    /// Pure decision: publication starts only for a succeeded Android APK/AAB file, produced by the tracked run of an explicitly
    /// enabled Build Profile, together with a successful Addressables player build of the same run.
    /// </summary>
    public static class RemoteContentPublishGate
    {
        /// <summary>Evaluates all start conditions in order and returns the first reason that blocks publication.</summary>
        public static PublishGateResult Evaluate(PublishRunData run, PublishBuildSnapshot build, RemoteContentPublishData settings)
        {
            if (run == null)
            {
                return PublishGateResult.Skip("Сборка плеера не была отслежена в этом запуске.", true);
            }
            // Unity may call the postprocessor before the report result is finalized (Unknown); a failed build never reaches it,
            // and the fresh-artifact checks below still apply. Explicit Failed/Cancelled always block.
            if (build.Result != BuildResult.Succeeded && build.Result != BuildResult.Unknown)
            {
                return PublishGateResult.Skip($"Сборка плеера завершилась со статусом {build.Result}.", false);
            }
            if (!settings.IsProfileEnabled(run.ProfileGuid) || run.ProfileGuid != build.ProfileGuid)
            {
                return PublishGateResult.Skip("Автопубликация не включена для Build Profile этого запуска.", false);
            }
            if (run.Target != BuildTarget.Android || build.Platform != BuildTarget.Android)
            {
                return PublishGateResult.Skip("Публикация выполняется только после сборки Android.", false);
            }
            var extension = Path.GetExtension(build.OutputPath).ToLowerInvariant();
            if (run.IsGradleExport || !build.OutputFileExists || (extension != ".apk" && extension != ".aab"))
            {
                return PublishGateResult.Skip("Экспорт Gradle-проекта или отсутствие файла APK/AAB: публикация пропущена.", false);
            }
            if (string.IsNullOrEmpty(run.OutputPath)
                || !string.Equals(Path.GetFullPath(run.OutputPath), Path.GetFullPath(build.OutputPath), StringComparison.OrdinalIgnoreCase)
                || build.OutputWriteUtc < run.StartedUtc.AddSeconds(-2))
            {
                return PublishGateResult.Skip("Файл APK/AAB не относится к текущему запуску сборки.", true);
            }
            if (run.AddressablesResult == null)
            {
                return PublishGateResult.Skip("В этом запуске не было сборки Addressables (проверьте «Build Addressables on Player Build»); результаты прошлых сборок не публикуются.", true);
            }
            if (!string.IsNullOrEmpty(run.AddressablesResult.Error))
            {
                return PublishGateResult.Skip("Сборка Addressables этого запуска завершилась с ошибкой.", true);
            }
            return PublishGateResult.Allow();
        }
    }
}
