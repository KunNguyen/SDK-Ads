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

        public static IAdService TryDecorateInterstitial(
            IAdService provider,
            MonoBehaviour host,
            SequentialTierConfig tierConfig)
        {
            if (provider == null || host == null || tierConfig == null || !tierConfig.enableSequentialLadder)
                return provider;

            var bridge = Type.GetType(BridgeTypeName);
            var method = bridge?.GetMethod(
                "TryDecorateInterstitial",
                BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                return provider;

            return method.Invoke(null, new object[] { provider, host, tierConfig }) as IAdService ?? provider;
        }
    }
}
