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
            if (setup?.admobAdsSetup == null) return AdInventorySetupMode.SingleUnit;
            return setup.admobAdsSetup.InterstitialTierConfig.enableSequentialLadder
                ? AdInventorySetupMode.Tiered
                : AdInventorySetupMode.SingleUnit;
        }

        public static AdInventorySetupMode GetRewardedMode(PlatformAdsProfile profile)
        {
            var setup = profile?.sdkSetup;
            if (setup?.admobAdsSetup == null) return AdInventorySetupMode.SingleUnit;
            return setup.admobAdsSetup.RewardedTierConfig.enableSequentialLadder
                ? AdInventorySetupMode.Tiered
                : AdInventorySetupMode.SingleUnit;
        }

        public static void SetInterstitialMode(
            JisSDKAdsSettings settings,
            BuildTargetPlatform platform,
            AdInventorySetupMode mode) =>
            SetFormatMode(settings, platform, isInterstitial: true, mode);

        public static void SetRewardedMode(
            JisSDKAdsSettings settings,
            BuildTargetPlatform platform,
            AdInventorySetupMode mode) =>
            SetFormatMode(settings, platform, isInterstitial: false, mode);

        static void SetFormatMode(
            JisSDKAdsSettings settings,
            BuildTargetPlatform platform,
            bool isInterstitial,
            AdInventorySetupMode mode)
        {
            if (settings == null) return;

            var setup = settings.GetProfile(platform)?.sdkSetup;
            if (setup?.admobAdsSetup == null) return;

            var tierConfig = isInterstitial
                ? setup.admobAdsSetup.InterstitialTierConfig
                : setup.admobAdsSetup.RewardedTierConfig;

            if (mode == AdInventorySetupMode.Tiered)
            {
                if (isInterstitial)
                    setup.interstitialAdsMediationType = AdsMediationType.ADMOB;
                else
                    setup.rewardedAdsMediationType = AdsMediationType.ADMOB;

                tierConfig.enableSequentialLadder = true;
            }
            else
            {
                tierConfig.enableSequentialLadder = false;
            }

            EditorUtility.SetDirty(setup);
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
