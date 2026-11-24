using System;

namespace Game.AssetManagement.Publishing
{
    /// <summary>Publication failure whose message is already user-readable and free of secrets.</summary>
    public sealed class RemoteContentPublishException : Exception
    {
        public PublishStageAlias Stage { get; private set; }

        public RemoteContentPublishException(PublishStageAlias stage, string message) : base(message)
        {
            Stage = stage;
        }
    }
}
