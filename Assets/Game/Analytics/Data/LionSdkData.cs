using UnityEngine;

namespace Game.Analytics.Data
{
    [CreateAssetMenu(fileName = "LionSdkData", menuName = "Game/Analytics/LionSdkData")]
    public class LionSdkData : AnalyticsProviderData
    {
        public override AnalyticsAlias Alias => AnalyticsAlias.LionSdk;
    }
}