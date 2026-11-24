using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.AssetManagement.Publishing
{
    /// <summary>
    /// Uploads a plan stage by stage (bundles, catalog, .hash). Each file goes to a unique temporary name and is renamed only after
    /// the transfer succeeded. The first failure stops everything; nothing already on the server except an own temporary file is deleted.
    /// </summary>
    public sealed class RemoteContentUploader
    {
        private const string TemporarySuffix = ".uploading";

        private static readonly PublishStageAlias[] StageOrderArray = { PublishStageAlias.Bundles, PublishStageAlias.Catalog, PublishStageAlias.Hash };

        private readonly Action<string> _reportProgress;

        /// <summary>Creates an uploader; the progress callback receives short secret-free status lines and may be a no-op.</summary>
        public RemoteContentUploader(Action<string> reportProgress)
        {
            _reportProgress = reportProgress ?? (_ => { });
        }

        /// <summary>
        /// Uploads all files in stage order using an open session. Returns the published remote paths on success; throws
        /// <see cref="RemoteContentPublishException"/> naming the failed stage, leaving later stages untouched.
        /// </summary>
        public List<string> Upload(ISftpSession session, PublishPlanData plan, string uploadToken)
        {
            var uploadedList = new List<string>();
            var knownDirectoriesSet = new HashSet<string> { plan.DestinationRoot };
            foreach (var stage in StageOrderArray)
            {
                foreach (var file in plan.FilesList.Where(candidate => candidate.Stage == stage))
                {
                    try
                    {
                        EnsureDirectory(session, plan.DestinationRoot, RemoteContentPathMapper.GetDirectory(file.RemotePath), knownDirectoriesSet);
                        UploadOne(session, file, uploadToken);
                    }
                    catch (Exception exception)
                    {
                        throw new RemoteContentPublishException(stage, $"Не удалось опубликовать '{file.RemotePath}': {exception.Message}");
                    }
                    uploadedList.Add(file.RemotePath);
                    _reportProgress($"{stage}: {uploadedList.Count}/{plan.FilesList.Count} {file.RemotePath}");
                }
            }
            return uploadedList;
        }

        /// <summary>Transfers to a temporary name, then renames; removes only its own temporary file when something fails.</summary>
        private static void UploadOne(ISftpSession session, PublishFileData file, string uploadToken)
        {
            var temporaryPath = file.RemotePath + "." + uploadToken + TemporarySuffix;
            try
            {
                session.UploadFile(file.LocalPath, temporaryPath);
                RenameReplacing(session, temporaryPath, file.RemotePath);
            }
            catch (Exception)
            {
                TryDelete(session, temporaryPath);
                throw;
            }
        }

        /// <summary>Renames the finished upload. If the server refuses to overwrite, the same-named target is replaced after the new copy is complete.</summary>
        private static void RenameReplacing(ISftpSession session, string temporaryPath, string finalPath)
        {
            try
            {
                session.MoveFile(temporaryPath, finalPath);
            }
            catch (Exception) when (session.PathExists(finalPath))
            {
                session.DeleteFile(finalPath);
                session.MoveFile(temporaryPath, finalPath);
            }
        }

        /// <summary>Best-effort removal of an own temporary file; secondary failures must not hide the original error.</summary>
        private static void TryDelete(ISftpSession session, string temporaryPath)
        {
            try
            {
                if (session.PathExists(temporaryPath))
                {
                    session.DeleteFile(temporaryPath);
                }
            }
            catch (Exception)
            {
                // The original upload error is more useful than a cleanup failure.
            }
        }

        /// <summary>Creates missing folders below the destination root. The root itself must already exist (checked beforehand).</summary>
        private static void EnsureDirectory(ISftpSession session, string root, string directory, HashSet<string> knownSet)
        {
            if (knownSet.Contains(directory))
            {
                return;
            }
            var relative = directory.Substring(root.Length).Trim('/');
            var current = root;
            foreach (var segment in relative.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                current += "/" + segment;
                if (knownSet.Add(current) && !session.PathExists(current))
                {
                    session.CreateDirectory(current);
                }
            }
        }
    }
}
