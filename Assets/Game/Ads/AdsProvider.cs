using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Game.Ads
{
    public abstract class AdsProvider
    {
        public abstract void TryShowInterstitial(string placement, Action onComplete = null);
        public abstract Task<bool> TryShowRewarded(string placement, Dictionary<string, object> rewardAdditionalData = null);
        public abstract void ShowBanner();
        public abstract void HideBanner();
    }
}
