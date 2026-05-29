using System;
using System.Collections.Generic;
using System.Reflection;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Ads.Settings;

namespace JisSDKAds.Ads.Integration
{
    /// <summary>
    /// Applies AdMob setup from <see cref="SDKSetup"/> via provider assembly (no compile-time reference).
    /// </summary>
    internal static class AdMobMediationReflection
    {
        const string BridgeTypeName = "JisSDKAds.Providers.AdMob.AdmobMediationConfigBridge, JisSDKAds.Providers.AdMob";

        public static void ApplySdkSetup(AdsManager manager, SDKSetup setup)
        {
            if (manager == null || setup == null) return;

            var bridge = Type.GetType(BridgeTypeName);
            var method = bridge?.GetMethod("ApplyFromSdkSetup", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarning(
                    "[JIS SDK] AdMob provider bridge not available (UNITY_AD_ADMOB undefined or recompile pending). " +
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

            var interMode = AdInventoryRemoteConfigResolver.ReadInterstitialMode();
            var rewardMode = AdInventoryRemoteConfigResolver.ReadRewardedMode();

            bridge.GetMethod("ApplyInventoryModesFromRemoteConfig", BindingFlags.Public | BindingFlags.Static)
                ?.Invoke(null, new object[] { manager, setup, interMode, rewardMode });

            Dictionary<AdTier, string> interstitialIds = null;
            Dictionary<AdTier, string> rewardedIds = null;

            var profile = manager.SdkSettings?.GetActiveProfile();

            if (interMode == AdInventorySetupMode.Tiered)
            {
                SequentialTierRemoteConfigResolver.TryReadTierIds(
                    SequentialTierAdFormat.Interstitial, profile, out interstitialIds);
            }

            if (rewardMode == AdInventorySetupMode.Tiered)
            {
                SequentialTierRemoteConfigResolver.TryReadTierIds(
                    SequentialTierAdFormat.Rewarded, profile, out rewardedIds);
            }

            if (interstitialIds == null && rewardedIds == null) return;

            bridge.GetMethod("ApplyTierRemoteUnitIds", BindingFlags.Public | BindingFlags.Static)
                ?.Invoke(null, new object[] { manager, interstitialIds, rewardedIds });
        }

        [Obsolete("Use ApplyRemoteAdInventorySettings")]
        public static void ApplySequentialTierRemoteConfig(AdsManager manager, SDKSetup setup) =>
            ApplyRemoteAdInventorySettings(manager, setup);
    }
}
