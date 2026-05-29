using System;
using System.Reflection;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Core.Interfaces;
using UnityEngine;

namespace JisSDKAds.Ads.Integration
{
    internal static class AdMobSequentialTierReflection
    {
        const string BridgeTypeName =
            "JisSDKAds.Providers.AdMob.SequentialTier.AdMobSequentialTierBridge, JisSDKAds.Providers.AdMob";

        public static IAdService TryDecorate(
            IAdService provider,
            MonoBehaviour host,
            SequentialTierConfig interstitialConfig,
            SequentialTierConfig rewardedConfig)
        {
            if (provider == null || host == null)
                return provider;

            bool useInterstitial = interstitialConfig != null && interstitialConfig.enableSequentialLadder;
            bool useRewarded = rewardedConfig != null && rewardedConfig.enableSequentialLadder;
            if (!useInterstitial && !useRewarded)
                return provider;

            var bridge = Type.GetType(BridgeTypeName);
            var method = bridge?.GetMethod(
                "TryDecorate",
                BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                return provider;

            return method.Invoke(null, new object[] { provider, host, interstitialConfig, rewardedConfig })
                   as IAdService
                   ?? provider;
        }
    }
}
