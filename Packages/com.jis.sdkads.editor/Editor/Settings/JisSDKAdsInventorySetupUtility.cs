#if UNITY_EDITOR
using JisSDKAds.Ads;
using JisSDKAds.Ads.Settings;
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.Editor
{
    static class JisSDKAdsInventorySetupUtility
    {
        const string SettingsFolder = "Assets/JisSDKAds/Settings";

        public static AdInventorySetupMode GetInterstitialMode(PlatformAdsProfile profile)
        {
            var setup = profile?.sdkSetup;
            var tier = GetTierConfig(setup, setup?.GetAdsMediationType(AdsType.INTERSTITIAL) ?? AdsMediationType.NONE, isInterstitial: true);
            return tier != null && tier.enableSequentialLadder
                ? AdInventorySetupMode.Tiered
                : AdInventorySetupMode.SingleUnit;
        }

        public static AdInventorySetupMode GetInterstitialMode(PlatformAdsProfile profile, AdsMediationType mediation)
        {
            var tier = GetTierConfig(profile?.sdkSetup, mediation, isInterstitial: true);
            return tier != null && tier.enableSequentialLadder
                ? AdInventorySetupMode.Tiered
                : AdInventorySetupMode.SingleUnit;
        }

        public static AdInventorySetupMode GetRewardedMode(PlatformAdsProfile profile)
        {
            var setup = profile?.sdkSetup;
            var tier = GetTierConfig(setup, setup?.GetAdsMediationType(AdsType.REWARDED) ?? AdsMediationType.NONE, isInterstitial: false);
            return tier != null && tier.enableSequentialLadder
                ? AdInventorySetupMode.Tiered
                : AdInventorySetupMode.SingleUnit;
        }

        public static AdInventorySetupMode GetRewardedMode(PlatformAdsProfile profile, AdsMediationType mediation)
        {
            var tier = GetTierConfig(profile?.sdkSetup, mediation, isInterstitial: false);
            return tier != null && tier.enableSequentialLadder
                ? AdInventorySetupMode.Tiered
                : AdInventorySetupMode.SingleUnit;
        }

        public static void SetInterstitialMode(
            JisSDKAdsSettings settings,
            BuildTargetPlatform platform,
            AdInventorySetupMode mode) =>
            SetFormatMode(settings, platform, isInterstitial: true, mode);

        public static void SetInterstitialMode(
            JisSDKAdsSettings settings,
            BuildTargetPlatform platform,
            AdsMediationType mediation,
            AdInventorySetupMode mode) =>
            SetFormatMode(settings, platform, mediation, isInterstitial: true, mode);

        public static void SetRewardedMode(
            JisSDKAdsSettings settings,
            BuildTargetPlatform platform,
            AdInventorySetupMode mode) =>
            SetFormatMode(settings, platform, isInterstitial: false, mode);

        public static void SetRewardedMode(
            JisSDKAdsSettings settings,
            BuildTargetPlatform platform,
            AdsMediationType mediation,
            AdInventorySetupMode mode) =>
            SetFormatMode(settings, platform, mediation, isInterstitial: false, mode);

        static void SetFormatMode(
            JisSDKAdsSettings settings,
            BuildTargetPlatform platform,
            bool isInterstitial,
            AdInventorySetupMode mode)
        {
            if (settings == null) return;

            var setup = settings.GetProfile(platform)?.sdkSetup;
            if (setup == null) return;

            var mediation = setup.GetAdsMediationType(isInterstitial ? AdsType.INTERSTITIAL : AdsType.REWARDED);
            var tierConfig = GetTierConfig(setup, mediation, isInterstitial);
            if (tierConfig == null) return;

            if (mode == AdInventorySetupMode.Tiered)
                tierConfig.enableSequentialLadder = true;
            else
                tierConfig.enableSequentialLadder = false;

            EditorUtility.SetDirty(setup);
        }

        static void SetFormatMode(
            JisSDKAdsSettings settings,
            BuildTargetPlatform platform,
            AdsMediationType mediation,
            bool isInterstitial,
            AdInventorySetupMode mode)
        {
            if (settings == null) return;

            var setup = settings.GetProfile(platform)?.sdkSetup;
            var tierConfig = GetTierConfig(setup, mediation, isInterstitial);
            if (tierConfig == null) return;

            tierConfig.enableSequentialLadder = mode == AdInventorySetupMode.Tiered;
            EditorUtility.SetDirty(setup);
        }

        public static JisSDKAds.Ads.SequentialTier.SequentialTierConfig GetInterstitialTierConfig(PlatformAdsProfile profile)
        {
            var setup = profile?.sdkSetup;
            return GetTierConfig(setup, setup?.GetAdsMediationType(AdsType.INTERSTITIAL) ?? AdsMediationType.NONE, isInterstitial: true);
        }

        public static JisSDKAds.Ads.SequentialTier.SequentialTierConfig GetInterstitialTierConfig(
            PlatformAdsProfile profile,
            AdsMediationType mediation) =>
            GetTierConfig(profile?.sdkSetup, mediation, isInterstitial: true);

        public static JisSDKAds.Ads.SequentialTier.SequentialTierConfig GetRewardedTierConfig(PlatformAdsProfile profile)
        {
            var setup = profile?.sdkSetup;
            return GetTierConfig(setup, setup?.GetAdsMediationType(AdsType.REWARDED) ?? AdsMediationType.NONE, isInterstitial: false);
        }

        public static JisSDKAds.Ads.SequentialTier.SequentialTierConfig GetRewardedTierConfig(
            PlatformAdsProfile profile,
            AdsMediationType mediation) =>
            GetTierConfig(profile?.sdkSetup, mediation, isInterstitial: false);

        static JisSDKAds.Ads.SequentialTier.SequentialTierConfig GetTierConfig(
            SDKSetup setup,
            AdsMediationType mediation,
            bool isInterstitial)
        {
            if (setup == null) return null;
            return mediation switch
            {
                AdsMediationType.MAX => isInterstitial
                    ? setup.maxAdsSetup?.InterstitialTierConfig
                    : setup.maxAdsSetup?.RewardedTierConfig,
                AdsMediationType.ADMOB => isInterstitial
                    ? setup.admobAdsSetup?.InterstitialTierConfig
                    : setup.admobAdsSetup?.RewardedTierConfig,
                _ => null
            };
        }

        public static SDKSetup EnsureSdkSetup(JisSDKAdsSettings settings, BuildTargetPlatform platform)
        {
            if (settings == null) return null;

            var profile = settings.GetProfile(platform);
            if (profile?.sdkSetup != null)
            {
                TryInitializeSetupDefaults(profile.sdkSetup);
                return profile.sdkSetup;
            }

            EnsureSettingsFolder();
            var path = platform == BuildTargetPlatform.Android
                ? $"{SettingsFolder}/AndroidSDKSetup.asset"
                : $"{SettingsFolder}/IOSSDKSetup.asset";

            var setup = AssetDatabase.LoadAssetAtPath<SDKSetup>(path);
            if (setup == null)
            {
                setup = ScriptableObject.CreateInstance<SDKSetup>();
                TryInitializeSetupDefaults(setup);
                AssetDatabase.CreateAsset(setup, path);
            }
            else
            {
                TryInitializeSetupDefaults(setup);
            }

            if (profile != null && profile.sdkSetup != setup)
            {
                Undo.RecordObject(settings, $"Assign {platform} SDKSetup");
                profile.sdkSetup = setup;
                EditorUtility.SetDirty(settings);
            }

            AssetDatabase.SaveAssets();
            return setup;
        }

        public static void TryInitializeSetupDefaults(SDKSetup setup)
        {
            if (setup == null) return;

            var hadMax = setup.maxAdsSetup != null;
            var hadAdmob = setup.admobAdsSetup != null;
            setup.EnsureMediationSetups();

            if (!hadMax || !hadAdmob)
                EditorUtility.SetDirty(setup);
        }

        static void EnsureSettingsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/JisSDKAds"))
                AssetDatabase.CreateFolder("Assets", "JisSDKAds");
            if (!AssetDatabase.IsValidFolder(SettingsFolder))
                AssetDatabase.CreateFolder("Assets/JisSDKAds", "Settings");
        }
    }
}
#endif
