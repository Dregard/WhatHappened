using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Facts about a finished player build, copied from the build report so the decision logic can be tested without one.</summary>
    public sealed class PublishBuildSnapshot
    {
        public BuildResult Result { get; private set; }
        public BuildTarget Platform { get; private set; }
        public string OutputPath { get; private set; }
        public string ProfileGuid { get; private set; }
        public bool OutputFileExists { get; private set; }
        public DateTime OutputWriteUtc { get; private set; }

        /// <summary>Creates a snapshot from explicit values.</summary>
        public PublishBuildSnapshot(BuildResult result, BuildTarget platform, string outputPath, string profileGuid, bool outputFileExists, DateTime outputWriteUtc)
        {
            Result = result;
            Platform = platform;
            OutputPath = outputPath ?? string.Empty;
            ProfileGuid = profileGuid ?? string.Empty;
            OutputFileExists = outputFileExists;
            OutputWriteUtc = outputWriteUtc;
        }

        /// <summary>Copies the result, platform and output file facts from a build report; the profile is the currently active Build Profile.</summary>
        public static PublishBuildSnapshot FromReport(BuildReport report, string activeProfileGuid, PublishRunData run)
        {
            // The summary can still be unpopulated when the postprocessor runs; fall back to what the run recorded at build start.
            var path = string.IsNullOrEmpty(report.summary.outputPath) && run != null ? run.OutputPath : report.summary.outputPath;
            var platform = report.summary.platform == BuildTarget.NoTarget || (int)report.summary.platform == 0
                ? (run != null ? run.Target : report.summary.platform) : report.summary.platform;
            var exists = !string.IsNullOrEmpty(path) && File.Exists(path);
            return new PublishBuildSnapshot(report.summary.result, platform, path, activeProfileGuid, exists,
                exists ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue);
        }
    }
}
