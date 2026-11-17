using UnityEngine;

namespace Game.Analytics.Data
{
    public abstract class AnalyticsProviderData : ScriptableObject
    {
        public abstract AnalyticsAlias Alias { get; }
    }
}