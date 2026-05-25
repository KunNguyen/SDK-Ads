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
            if (method == null) return;

            method.Invoke(null, new object[] { manager, setup });
        }

        public static void ApplySequentialTierRemoteConfig(AdsManager manager, SDKSetup setup)
        {
            if (manager == null || setup?.admobAdsSetup == null) return;

            Dictionary<AdTier, string> interstitialIds = null;
            Dictionary<AdTier, string> rewardedIds = null;

            var admob = setup.admobAdsSetup;
            if (setup.interstitialAdsMediationType == AdsMediationType.ADMOB
                && admob.InterstitialTierConfig != null
                && admob.InterstitialTierConfig.enableSequentialLadder)
            {
                SequentialTierRemoteConfigResolver.TryReadTierIds(
                    SequentialTierAdFormat.Interstitial, out interstitialIds);
            }

            if (setup.rewardedAdsMediationType == AdsMediationType.ADMOB
                && admob.RewardedTierConfig != null
                && admob.RewardedTierConfig.enableSequentialLadder)
            {
                SequentialTierRemoteConfigResolver.TryReadTierIds(
                    SequentialTierAdFormat.Rewarded, out rewardedIds);
            }

            if (interstitialIds == null && rewardedIds == null) return;

            var bridge = Type.GetType(BridgeTypeName);
            var method = bridge?.GetMethod(
                "ApplyTierRemoteUnitIds",
                BindingFlags.Public | BindingFlags.Static);
            method?.Invoke(null, new object[] { manager, interstitialIds, rewardedIds });
        }
    }
}
