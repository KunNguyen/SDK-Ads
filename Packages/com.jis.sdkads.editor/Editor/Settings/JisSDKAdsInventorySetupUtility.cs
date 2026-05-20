#if UNITY_EDITOR
using JisSDKAds.Ads;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Core.Tiered.Config;
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.Editor
{
    public enum AdInventorySetupMode
    {
        SingleUnit = 0,
        Tiered = 1
    }

    static class JisSDKAdsInventorySetupUtility
    {
        const string SettingsFolder = "Assets/JisSDKAds/Settings";

        public static AdInventorySetupMode GetInterstitialMode(PlatformAdsProfile profile)
        {
            return GetFormatMode(profile, isInterstitial: true);
        }

        public static AdInventorySetupMode GetRewardedMode(PlatformAdsProfile profile)
        {
            return GetFormatMode(profile, isInterstitial: false);
        }

        static AdInventorySetupMode GetFormatMode(PlatformAdsProfile profile, bool isInterstitial)
        {
            var tiered = profile?.tieredAdsConfig;
            if (tiered == null || !tiered.EnableTieredInventory)
                return AdInventorySetupMode.SingleUnit;

            var tieredForFormat = isInterstitial
                ? tiered.EnableTieredInventoryForInterstitial
                : tiered.EnableTieredInventoryForRewarded;

            return tieredForFormat ? AdInventorySetupMode.Tiered : AdInventorySetupMode.SingleUnit;
        }

        public static void SetInterstitialMode(
            JisSDKAdsSettings settings,
            BuildTargetPlatform platform,
            AdInventorySetupMode mode)
        {
            SetFormatMode(settings, platform, isInterstitial: true, mode);
        }

        public static void SetRewardedMode(
            JisSDKAdsSettings settings,
            BuildTargetPlatform platform,
            AdInventorySetupMode mode)
        {
            SetFormatMode(settings, platform, isInterstitial: false, mode);
        }

        static void SetFormatMode(
            JisSDKAdsSettings settings,
            BuildTargetPlatform platform,
            bool isInterstitial,
            AdInventorySetupMode mode)
        {
            if (settings == null) return;

            var profile = settings.GetProfile(platform);
            if (profile == null) return;

            var tiered = EnsureTieredConfig(settings, platform);
            if (tiered == null) return;

            Undo.RecordObject(tiered, "Change ad inventory mode");
            if (isInterstitial)
                tiered.EnableTieredInventoryForInterstitial = mode == AdInventorySetupMode.Tiered;
            else
                tiered.EnableTieredInventoryForRewarded = mode == AdInventorySetupMode.Tiered;

            SyncMasterTieredFlag(tiered);
            EditorUtility.SetDirty(tiered);
            EditorUtility.SetDirty(settings);
        }

        static void SyncMasterTieredFlag(TieredAdsConfig tiered)
        {
            tiered.EnableTieredInventory =
                tiered.EnableTieredInventoryForInterstitial
                || tiered.EnableTieredInventoryForRewarded;
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

        public static TieredAdsConfig EnsureTieredConfig(JisSDKAdsSettings settings, BuildTargetPlatform platform)
        {
            if (settings == null) return null;

            var profile = settings.GetProfile(platform);
            if (profile == null) return null;

            if (profile.tieredAdsConfig != null)
                return profile.tieredAdsConfig;

            EnsureSettingsFolder();
            var path = platform == BuildTargetPlatform.Android
                ? $"{SettingsFolder}/AndroidTieredAdsConfig.asset"
                : $"{SettingsFolder}/IOSTieredAdsConfig.asset";

            var tiered = AssetDatabase.LoadAssetAtPath<TieredAdsConfig>(path);
            if (tiered == null)
            {
                tiered = ScriptableObject.CreateInstance<TieredAdsConfig>();
                AssetDatabase.CreateAsset(tiered, path);
            }

            Undo.RecordObject(settings, $"Assign {platform} TieredAdsConfig");
            profile.tieredAdsConfig = tiered;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return tiered;
        }

        public static void TryInitializeSetupDefaults(SDKSetup setup)
        {
            if (setup == null) return;

            var changed = false;
            if (setup.maxAdsSetup == null)
            {
                setup.maxAdsSetup = new MaxAdSetup();
                changed = true;
            }

            if (setup.admobAdsSetup == null)
            {
                setup.admobAdsSetup = new AdmobAdSetup();
                changed = true;
            }

            if (changed)
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
