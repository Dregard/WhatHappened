using System.Collections.Generic;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Immutable result of a publication attempt. The message never contains secrets.</summary>
    public sealed class PublishResultData
    {
        public PublishStatusAlias Status { get; private set; }
        public PublishStageAlias Stage { get; private set; }
        public string Message { get; private set; }
        public IReadOnlyList<string> UploadedRemotePathsList { get; private set; }

        private PublishResultData(PublishStatusAlias status, PublishStageAlias stage, string message, IReadOnlyList<string> uploadedList)
        {
            Status = status;
            Stage = stage;
            Message = message;
            UploadedRemotePathsList = uploadedList;
        }

        /// <summary>Creates a result for a publication that was intentionally not started.</summary>
        public static PublishResultData Skipped(string reason)
        {
            return new PublishResultData(PublishStatusAlias.Skipped, PublishStageAlias.None, reason, new List<string>());
        }

        /// <summary>Creates a result for a publication that uploaded and verified everything.</summary>
        public static PublishResultData Succeeded(IReadOnlyList<string> uploadedList)
        {
            return new PublishResultData(PublishStatusAlias.Succeeded, PublishStageAlias.None, "Published and verified.", uploadedList);
        }

        /// <summary>Creates a result for a publication that stopped at the given stage; later stages were not run.</summary>
        public static PublishResultData Failed(PublishStageAlias stage, string message, IReadOnlyList<string> uploadedList)
        {
            return new PublishResultData(PublishStatusAlias.Failed, stage, message, uploadedList);
        }
    }
}
