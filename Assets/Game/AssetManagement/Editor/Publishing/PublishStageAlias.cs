namespace Game.AssetManagement.Publishing
{
    /// <summary>Publication stages in execution order. Upload stages (Bundles, Catalog, Hash) are strictly sequential.</summary>
    public enum PublishStageAlias
    {
        Unknown = -1,
        None = 0,
        Configuration = 1,
        Connection = 2,
        Bundles = 3,
        Catalog = 4,
        Hash = 5,
        Verification = 6
    }
}
