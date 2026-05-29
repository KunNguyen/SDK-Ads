using JisSDKAds.Ads.Settings;
using JisSDKAds.Common;
using JisSDKAds.Firebase;
using UnityEngine;

namespace JisSDKAds.Ads.SequentialTier
{
    /// <summary>
    /// Reads interstitial/rewarded Single vs Tiered mode from Firebase Remote Config (default: Single).
    /// </summary>
    public static class AdInventoryRemoteConfigResolver
    {
        public static bool RequiresFetchBeforeAds(SDKSetup setup)
        {
            if (setup == null) return false;
            return setup.IsActiveAdsType(AdsType.INTERSTITIAL)
                   || setup.IsActiveAdsType(AdsType.REWARDED);
        }

        public static AdInventorySetupMode ReadInterstitialMode()
        {
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
                return AdInventorySetupMode.SingleUnit;

            return ParseInventoryMode(
                FirebaseManager.Instance.GetConfigString(Keys.key_remote_interstitial_inventory_mode));
        }

        public static AdInventorySetupMode ReadRewardedMode()
        {
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
                return AdInventorySetupMode.SingleUnit;

            return ParseInventoryMode(
                FirebaseManager.Instance.GetConfigString(Keys.key_remote_rewarded_inventory_mode));
        }

        public static AdInventorySetupMode ParseInventoryMode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return AdInventorySetupMode.SingleUnit;

            switch (value.Trim().ToLowerInvariant())
            {
                case "tiered":
                case "tier":
                case "sequential":
                case "ladder":
                case "1":
                case "true":
                case "yes":
                    return AdInventorySetupMode.Tiered;
                default:
                    return AdInventorySetupMode.SingleUnit;
            }
        }

        public static void LogResolvedModes(AdInventorySetupMode interstitial, AdInventorySetupMode rewarded)
        {
            DebugAds.Log(
                $"[RemoteConfig] Inventory mode — interstitial: {interstitial}, rewarded: {rewarded}");
        }

        /// <summary>
        /// Applies RC inventory mode to <see cref="SDKSetup"/> before Core / mediation init (JisAds + AdsManager).
        /// </summary>
        public static void ApplyInventoryModesToSdkSetup(
            SDKSetup setup,
            AdInventorySetupMode interstitialMode,
            AdInventorySetupMode rewardedMode)
        {
            if (setup?.admobAdsSetup == null)
                return;

            var admob = setup.admobAdsSetup;
            ApplyInventoryMode(setup, admob, isInterstitial: true, interstitialMode);
            ApplyInventoryMode(setup, admob, isInterstitial: false, rewardedMode);
            LogResolvedModes(interstitialMode, rewardedMode);
        }

        /// <summary>Reads RC when ready; otherwise keeps editor defaults on <paramref name="setup"/>.</summary>
        public static void ApplyInventoryModesFromRemoteConfig(SDKSetup setup)
        {
            if (setup == null)
                return;

            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
            {
                DebugAds.LogWarning(
                    "[RemoteConfig] Inventory mode not ready — using editor defaults on SDKSetup.");
                return;
            }

            ApplyInventoryModesToSdkSetup(
                setup,
                ReadInterstitialMode(),
                ReadRewardedMode());
        }

        static void ApplyInventoryMode(
            SDKSetup setup,
            AdmobAdSetup admob,
            bool isInterstitial,
            AdInventorySetupMode mode)
        {
            var tierConfig = isInterstitial
                ? admob.InterstitialTierConfig
                : admob.RewardedTierConfig;
            if (tierConfig == null)
                return;

            var tiered = mode == AdInventorySetupMode.Tiered;
            tierConfig.enableSequentialLadder = tiered;

            if (!tiered)
                return;

            if (isInterstitial)
                setup.interstitialAdsMediationType = AdsMediationType.ADMOB;
            else
                setup.rewardedAdsMediationType = AdsMediationType.ADMOB;
        }
    }
}
