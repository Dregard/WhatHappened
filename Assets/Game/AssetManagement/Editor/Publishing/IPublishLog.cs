namespace Game.AssetManagement.Publishing
{
    /// <summary>Sink for publication messages. Implementations receive text that is already free of secrets.</summary>
    public interface IPublishLog
    {
        /// <summary>Writes an informational line.</summary>
        void Info(string message);

        /// <summary>Writes a warning line.</summary>
        void Warning(string message);

        /// <summary>Writes an error line.</summary>
        void Error(string message);
    }
}
