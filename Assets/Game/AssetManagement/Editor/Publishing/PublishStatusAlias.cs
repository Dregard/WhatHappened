namespace Game.AssetManagement.Publishing
{
    /// <summary>Outcome class of one publication attempt after a player build.</summary>
    public enum PublishStatusAlias
    {
        Unknown = -1,
        None = 0,
        Skipped = 1,
        Succeeded = 2,
        Failed = 3
    }
}
