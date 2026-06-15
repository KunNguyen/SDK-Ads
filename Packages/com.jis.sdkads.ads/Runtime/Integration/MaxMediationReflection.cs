using System;
using System.Collections.Generic;
using System.Reflection;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Core.Interfaces;
using UnityEngine;

namespace JisSDKAds.Ads.Integration
{
    internal static class MaxMediationReflection
    {
        const string BridgeTypeName = "JisSDKAds.Providers.Max.MaxMediationConfigBridge, JisSDKAds.Providers.Max";

        public static void ApplySdkSetup(AdsManager manager, SDKSetup setup)
        {
            if (manager == null || setup == null) return;

            var bridge = Type.GetType(BridgeTypeName);
            var method = bridge?.GetMethod("ApplyFromSdkSetup", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarning(
                    "[JIS SDK] MAX provider bridge not available (UNITY_AD_MAX undefined or recompile pending). " +
                    "Click Apply again after Unity finishes recompiling.");
#endif
                return;
            }

            method.Invoke(null, new object[] { manager, setup });
        }

        public static void ApplyRemoteAdInventorySettings(AdsManager manager, SDKSetup setup)
        {
            if (manager == null || setup == null) return;

            var bridge = Type.GetType(BridgeTypeName);
            if (bridge == null) return;

            var interMode = AdInventoryRemoteConfigResolver.ReadInterstitialMode(AdsMediationType.MAX);
            var rewardMode = AdInventoryRemoteConfigResolver.ReadRewardedMode(AdsMediationType.MAX);

            bridge.GetMethod("ApplyInventoryModesFromRemoteConfig", BindingFlags.Public | BindingFlags.Static)
                ?.Invoke(null, new object[] { manager, setup, interMode, rewardMode });

            Dictionary<AdTier, string> interstitialIds = null;
            Dictionary<AdTier, string> rewardedIds = null;

            var profile = manager.SdkSettings?.GetActiveProfile();

            if (interMode == AdInventorySetupMode.Tiered)
            {
                SequentialTierRemoteConfigResolver.TryReadTierIds(
                    SequentialTierAdFormat.Interstitial, profile, AdsMediationType.MAX, out interstitialIds);
            }

            if (rewardMode == AdInventorySetupMode.Tiered)
            {
                SequentialTierRemoteConfigResolver.TryReadTierIds(
                    SequentialTierAdFormat.Rewarded, profile, AdsMediationType.MAX, out rewardedIds);
            }

            if (interstitialIds == null && rewardedIds == null) return;

            bridge.GetMethod("ApplyTierRemoteUnitIds", BindingFlags.Public | BindingFlags.Static)
                ?.Invoke(null, new object[] { manager, interstitialIds, rewardedIds });
        }
    }

    internal static class MaxSequentialTierReflection
    {
        const string BridgeTypeName =
            "JisSDKAds.Providers.Max.SequentialTier.MaxSequentialTierBridge, JisSDKAds.Providers.Max";

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
