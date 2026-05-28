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
        public static IAdService TryDecorateInterstitial(
            IAdService provider,
            MonoBehaviour host,
            SequentialTierConfig tierConfig)
        {
            if (provider == null || host == null || tierConfig == null || !tierConfig.enableSequentialLadder)
                return provider;

            var sequential = new SequentialTierInterstitialAd(host, tierConfig);
            return new SequentialTierAdServiceDecorator(provider, sequential);
        }
    }
}
#endif
