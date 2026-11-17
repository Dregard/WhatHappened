using System.Collections.Generic;
using Game.Analytics.Data;

namespace Game.Analytics
{
    public interface IAnalyticsController
    {
        public AnalyticsAlias Alias { get; }
        public void Initialize(AnalyticsProviderData data);
        public void UpdateParameters(Dictionary<string, object> parameters);
        public void FireEvent(string eventName, Dictionary<string, object> eventData = null);
    }
}