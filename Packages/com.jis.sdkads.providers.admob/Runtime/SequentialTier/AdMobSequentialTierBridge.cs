#if UNITY_AD_ADMOB
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Core.Interfaces;
using UnityEngine;

namespace JisSDKAds.Providers.AdMob.SequentialTier
{
    /// <summary>
    /// Entry point for JisAds Core to use sequential tier interstitial without a compile-time Ads→AdMob reference cycle.
    /// </summary>
    public static class AdMobSequentialTierBridge
    {
        public static IAdService TryDecorate(
            IAdService provider,
            MonoBehaviour host,
            SequentialTierConfig interstitialConfig,
            SequentialTierConfig rewardedConfig)
        {
            if (provider == null || host == null)
                return provider;

            SequentialTierInterstitialAd interstitial = null;
            if (interstitialConfig != null && interstitialConfig.enableSequentialLadder)
                interstitial = new SequentialTierInterstitialAd(host, interstitialConfig);

            SequentialTierRewardedAd rewarded = null;
            if (rewardedConfig != null && rewardedConfig.enableSequentialLadder)
                rewarded = new SequentialTierRewardedAd(host, rewardedConfig);

            if (interstitial == null && rewarded == null)
                return provider;

            return new SequentialTierAdServiceDecorator(provider, interstitial, rewarded);
        }
    }
}
#endif
