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

        public static AdInventorySetupMode ReadInterstitialMode(AdsMediationType mediation)
        {
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
                return AdInventorySetupMode.SingleUnit;

            var providerValue = FirebaseManager.Instance.GetConfigString(GetInventoryModeKey(mediation, AdsType.INTERSTITIAL));
            if (!string.IsNullOrWhiteSpace(providerValue))
                return ParseInventoryMode(providerValue);

            return ReadInterstitialMode();
        }

        public static AdInventorySetupMode ReadRewardedMode()
        {
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
                return AdInventorySetupMode.SingleUnit;

            return ParseInventoryMode(
                FirebaseManager.Instance.GetConfigString(Keys.key_remote_rewarded_inventory_mode));
        }

        public static AdInventorySetupMode ReadRewardedMode(AdsMediationType mediation)
        {
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
                return AdInventorySetupMode.SingleUnit;

            var providerValue = FirebaseManager.Instance.GetConfigString(GetInventoryModeKey(mediation, AdsType.REWARDED));
            if (!string.IsNullOrWhiteSpace(providerValue))
                return ParseInventoryMode(providerValue);

            return ReadRewardedMode();
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
            if (setup == null)
                return;

            ApplyInventoryMode(setup, setup.GetAdsMediationType(AdsType.INTERSTITIAL), isInterstitial: true, interstitialMode);
            ApplyInventoryMode(setup, setup.GetAdsMediationType(AdsType.REWARDED), isInterstitial: false, rewardedMode);
            LogResolvedModes(interstitialMode, rewardedMode);
        }

        /// <summary>Reads RC when ready; otherwise keeps editor defaults on <paramref name="setup"/>.</summary>
        public static void ApplyInventoryModesFromRemoteConfig(SDKSetup setup)
        {
            ApplyInventoryModesFromRemoteConfig(setup, profile: null, settings: null);
        }

        public static void ApplyInventoryModesFromRemoteConfig(
            SDKSetup setup,
            PlatformAdsProfile profile,
            JisSDKAdsSettings settings)
        {
            if (setup == null)
                return;

            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
            {
                DebugAds.LogWarning(
                    "[RemoteConfig] Inventory mode not ready — using editor defaults on SDKSetup.");
                return;
            }

            var primaryInterstitialMediation = setup.GetAdsMediationType(AdsType.INTERSTITIAL);
            var primaryRewardedMediation = setup.GetAdsMediationType(AdsType.REWARDED);

            ApplyInventoryMode(
                setup,
                primaryInterstitialMediation,
                isInterstitial: true,
                ReadInterstitialMode(primaryInterstitialMediation));
            ApplyInventoryMode(
                setup,
                primaryRewardedMediation,
                isInterstitial: false,
                ReadRewardedMode(primaryRewardedMediation));

            var secondInterstitialMediation = ResolveSecondMediation(profile, settings, AdsType.INTERSTITIAL, primaryInterstitialMediation);
            var secondRewardedMediation = ResolveSecondMediation(profile, settings, AdsType.REWARDED, primaryRewardedMediation);

            if (secondInterstitialMediation != AdsMediationType.NONE)
            {
                ApplyInventoryMode(
                    setup,
                    secondInterstitialMediation,
                    isInterstitial: true,
                    ReadInterstitialMode(secondInterstitialMediation));
            }

            if (secondRewardedMediation != AdsMediationType.NONE)
            {
                ApplyInventoryMode(
                    setup,
                    secondRewardedMediation,
                    isInterstitial: false,
                    ReadRewardedMode(secondRewardedMediation));
            }

            LogResolvedProviderModes(setup, primaryInterstitialMediation, secondInterstitialMediation, primaryRewardedMediation, secondRewardedMediation);
        }

        static void ApplyInventoryMode(
            SDKSetup setup,
            AdsMediationType mediation,
            bool isInterstitial,
            AdInventorySetupMode mode)
        {
            var tierConfig = GetTierConfig(setup, mediation, isInterstitial);
            if (tierConfig == null)
                return;

            tierConfig.enableSequentialLadder = mode == AdInventorySetupMode.Tiered;
        }

        static SequentialTierConfig GetTierConfig(SDKSetup setup, AdsMediationType mediation, bool isInterstitial)
        {
            return mediation switch
            {
                AdsMediationType.ADMOB => isInterstitial
                    ? setup.admobAdsSetup?.InterstitialTierConfig
                    : setup.admobAdsSetup?.RewardedTierConfig,
                AdsMediationType.MAX => isInterstitial
                    ? setup.maxAdsSetup?.InterstitialTierConfig
                    : setup.maxAdsSetup?.RewardedTierConfig,
                _ => null
            };
        }

        static AdsMediationType ResolveSecondMediation(
            PlatformAdsProfile profile,
            JisSDKAdsSettings settings,
            AdsType adsType,
            AdsMediationType primary)
        {
            if (settings == null || !settings.IsMultipleMediationEnabled(adsType))
                return AdsMediationType.NONE;

            foreach (var mediation in settings.GetFullscreenAutoShowPriority(adsType))
            {
                if (mediation != AdsMediationType.NONE && mediation != primary)
                    return mediation;
            }

            var active = profile?.mediation ?? settings.GetActiveMediation();
            if (active != AdsMediationType.NONE && active != primary)
                return active;

            return primary == AdsMediationType.ADMOB
                ? AdsMediationType.MAX
                : primary == AdsMediationType.MAX
                    ? AdsMediationType.ADMOB
                    : AdsMediationType.NONE;
        }

        static void LogResolvedProviderModes(
            SDKSetup setup,
            AdsMediationType primaryInterstitial,
            AdsMediationType secondInterstitial,
            AdsMediationType primaryRewarded,
            AdsMediationType secondRewarded)
        {
            DebugAds.Log(
                "[RemoteConfig] Inventory mode - " +
                $"interstitial {primaryInterstitial}: {ModeFor(setup, primaryInterstitial, true)}, " +
                $"interstitial second {secondInterstitial}: {ModeFor(setup, secondInterstitial, true)}, " +
                $"rewarded {primaryRewarded}: {ModeFor(setup, primaryRewarded, false)}, " +
                $"rewarded second {secondRewarded}: {ModeFor(setup, secondRewarded, false)}");
        }

        static AdInventorySetupMode ModeFor(SDKSetup setup, AdsMediationType mediation, bool isInterstitial)
        {
            var tier = GetTierConfig(setup, mediation, isInterstitial);
            return tier != null && tier.enableSequentialLadder
                ? AdInventorySetupMode.Tiered
                : AdInventorySetupMode.SingleUnit;
        }

        public static string GetInventoryModeKey(AdsMediationType mediation, AdsType adsType)
        {
            return mediation switch
            {
                AdsMediationType.MAX when adsType == AdsType.INTERSTITIAL => Keys.key_remote_max_interstitial_inventory_mode,
                AdsMediationType.MAX when adsType == AdsType.REWARDED => Keys.key_remote_max_rewarded_inventory_mode,
                AdsMediationType.ADMOB when adsType == AdsType.INTERSTITIAL => Keys.key_remote_admob_interstitial_inventory_mode,
                AdsMediationType.ADMOB when adsType == AdsType.REWARDED => Keys.key_remote_admob_rewarded_inventory_mode,
                _ => adsType == AdsType.REWARDED
                    ? Keys.key_remote_rewarded_inventory_mode
                    : Keys.key_remote_interstitial_inventory_mode
            };
        }
    }
}
