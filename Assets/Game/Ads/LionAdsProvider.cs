using System;
using System.Collections.Generic;
using System.Threading.Tasks;
//using LionStudios.Suite.Ads;

namespace Game.Ads
{
    public class LionAdsProvider : AdsProvider
    {
        public override void TryShowInterstitial(string placement, Action onComplete = null)
        {
            //LionAds.TryShowInterstitial(placement, onComplete);
        }

        public override Task<bool> TryShowRewarded(string placement,
            Dictionary<string, object> rewardAdditionalData = null)
        {
            var tcs = new TaskCompletionSource<bool>();
            var rewarded = false;

            // LionAds.TryShowRewarded(
            //     placement,
            //     onRewarded: () => rewarded = true,
            //     onClosed: () => tcs.TrySetResult(rewarded),
            //     additionalData: rewardAdditionalData
            // );

            return tcs.Task;
        }

        public override void ShowBanner()
        {
            //LionAds.ShowBanner();
        }

        public override void HideBanner()
        {
            //LionAds.HideBanner();
        }
    }
}
