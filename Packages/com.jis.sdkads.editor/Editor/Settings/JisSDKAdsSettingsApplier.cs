#if UNITY_EDITOR
using JisSDKAds.Ads;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Core.Tiered.Config;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JisSDKAds.Editor
{
    /// <summary>
    /// Central apply / validate logic for JisSDKAdsSettings → scene + scripting defines.
    /// </summary>
    public static class JisSDKAdsSettingsApplier
    {
        public static bool Apply(JisSDKAdsSettings settings, string reason = "Apply")
        {
            if (settings == null)
            {
                Debug.LogWarning("[JIS SDK] No JisSDKAdsSettings to apply.");
                return false;
            }

            var activeTarget = EditorUserBuildSettings.activeBuildTarget;
            var activeProfile = settings.GetActiveProfile();
            var activeSetup = activeProfile?.sdkSetup;

            if (activeSetup == null)
            {
                Debug.LogWarning(
                    $"[JIS SDK] {reason}: Active profile ({activeTarget}) has no SDKSetup assigned.");
                return false;
            }

            var adsManager = Object.FindFirstObjectByType<AdsManager>();
            if (adsManager != null)
            {
                settings.ApplyToAdsManager(adsManager);
                EditorUtility.SetDirty(adsManager);
                EditorSceneManager.MarkSceneDirty(adsManager.gameObject.scene);
            }
            else
            {
                Debug.Log($"[JIS SDK] {reason}: No AdsManager in scene — applied scripting defines only.");
            }

            SyncJisAdsInScene(settings);
            activeSetup.SetupSymbol();

            Debug.Log(
                $"[JIS SDK] {reason}: Applied '{settings.name}' for {activeTarget} " +
                $"(mediation={activeProfile.mediation}, active formats={AdsSetupUtility.CountActiveFormats(activeSetup)}).");
            return true;
        }

        public static void SyncJisAdsInScene(JisSDKAdsSettings settings)
        {
            var jisAds = Object.FindFirstObjectByType<JisAds>();
            if (jisAds == null) return;

            var so = new SerializedObject(jisAds);
            var settingsProp = so.FindProperty("settings");
            if (settingsProp != null)
            {
                settingsProp.objectReferenceValue = settings;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(jisAds);
            }
        }

        public static ValidationResult Validate(JisSDKAdsSettings settings)
        {
            var result = new ValidationResult();
            if (settings == null)
            {
                result.AddError("Settings asset is null.");
                return result;
            }

            ValidateProfile(settings.android, BuildTargetPlatform.Android, result);
            ValidateProfile(settings.ios, BuildTargetPlatform.iOS, result);
            return result;
        }

        static void ValidateProfile(PlatformAdsProfile profile, BuildTargetPlatform platform, ValidationResult result)
        {
            var label = platform.ToString();
            if (profile == null)
            {
                result.AddWarning($"{label}: profile is null.");
                return;
            }

            if (profile.sdkSetup == null)
            {
                result.AddError($"{label}: SDKSetup not assigned.");
                return;
            }

            if (profile.mediation == AdsMediationType.NONE)
                result.AddWarning($"{label}: primary mediation is NONE.");

            if (AdsSetupUtility.CountActiveFormats(profile.sdkSetup) == 0)
                result.AddWarning($"{label}: no ad formats enabled (all mediation = NONE).");

            ValidateSequentialTier(profile, label, result);
        }

        static void ValidateSequentialTier(PlatformAdsProfile profile, string platformLabel, ValidationResult result)
        {
            var setup = profile?.sdkSetup;
            if (setup?.admobAdsSetup == null) return;

            if (setup.interstitialAdsMediationType == AdsMediationType.ADMOB
                && setup.admobAdsSetup.InterstitialTierConfig.enableSequentialLadder
                && !HasAnyTierId(setup.admobAdsSetup.InterstitialTierConfig))
            {
                result.AddWarning(
                    $"{platformLabel}: interstitial tier enabled — set Firebase RC keys inter_premium_id … inter_fill_id.");
            }

            if (setup.rewardedAdsMediationType == AdsMediationType.ADMOB
                && setup.admobAdsSetup.RewardedTierConfig.enableSequentialLadder
                && !HasAnyTierId(setup.admobAdsSetup.RewardedTierConfig))
            {
                result.AddWarning(
                    $"{platformLabel}: rewarded tier enabled — set Firebase RC keys reward_premium_id … reward_fill_id.");
            }
        }

        static bool HasAnyTierId(SequentialTierConfig config)
        {
            if (config == null) return false;
            if (!string.IsNullOrWhiteSpace(config.ResolveDefaultAdUnitId())) return true;
            foreach (var entry in config.Tiers)
            {
                if (entry != null && entry.HasUnitId) return true;
            }

            return false;
        }

        public class ValidationResult
        {
            public System.Collections.Generic.List<string> Errors = new();
            public System.Collections.Generic.List<string> Warnings = new();
            public bool IsValid => Errors.Count == 0;

            public void AddError(string msg) => Errors.Add(msg);
            public void AddWarning(string msg) => Warnings.Add(msg);
        }
    }
}
#endif
