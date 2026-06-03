using System;

namespace JisSDKAds.Ads.SequentialTier
{
    /// <summary>Helper for non-tier interstitial/rewarded loads to use <see cref="AdLoadCoordinator"/>.</summary>
    public static class AdLoadPipeline
    {
        public static void RunRewardedLoad(Action beginLoad, bool urgent = false) =>
            AdLoadCoordinator.Instance.RequestGenericLoad(AdLoadFormat.Rewarded, beginLoad, urgent);

        public static void RunInterstitialLoad(Action beginLoad, bool urgent = false) =>
            AdLoadCoordinator.Instance.RequestGenericLoad(AdLoadFormat.Interstitial, beginLoad, urgent);

        public static void NotifyRewardedFinished() =>
            AdLoadCoordinator.Instance.NotifyGenericLoadEnded(AdLoadFormat.Rewarded);

        public static void NotifyInterstitialFinished() =>
            AdLoadCoordinator.Instance.NotifyGenericLoadEnded(AdLoadFormat.Interstitial);
    }
}
