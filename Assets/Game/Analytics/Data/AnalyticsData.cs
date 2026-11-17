using UnityEngine;

namespace Game.Analytics.Data
{
    [CreateAssetMenu(fileName = "AnalyticsData", menuName = "Game/Analytics/AnalyticsData")]
    public class AnalyticsData : ScriptableObject
    {
        public AnalyticsProviderData[] ProviderDatas;
    }
}