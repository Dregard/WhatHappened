using System.Collections.Generic;
using Game.Analytics.Data;

namespace Game.Analytics
{
    public abstract class AnalyticsController<TData> : IAnalyticsController
        where TData : AnalyticsProviderData
    {
        public void Initialize(AnalyticsProviderData data)
        {
            Initialize((TData)data);
        }
        
        
        public abstract AnalyticsAlias Alias { get; }
        public abstract void Initialize(TData data);
        public abstract void UpdateParameters(Dictionary<string, object> parameters);
        public abstract void FireEvent(string eventName, Dictionary<string, object> eventData = null);
    }
}