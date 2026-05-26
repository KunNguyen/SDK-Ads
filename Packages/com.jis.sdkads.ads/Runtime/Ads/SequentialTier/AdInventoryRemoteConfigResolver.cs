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
    }
}
