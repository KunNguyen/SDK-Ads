#if UNITY_EDITOR
using System.Collections.Generic;
using JisSDKAds.Ads;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Common;
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
        public static JisSDKAdsSettings TryLoadDefaultSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<JisSDKAdsSettings>(
                JisSDKAdsSettingsMenu.DefaultSettingsPath);
            if (settings != null)
                return settings;

            var guids = AssetDatabase.FindAssets("t:JisSDKAdsSettings");
            if (guids.Length == 0)
                return null;

            return AssetDatabase.LoadAssetAtPath<JisSDKAdsSettings>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }

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

            settings.ApplyRuntimeDebugSettings();
            settings.SyncAllProfileMediationToSdkSetups();
            settings.ApplyScriptingDefinesForAllPlatforms();

            TryInitializeSetupDefaults(settings.android?.sdkSetup);
            TryInitializeSetupDefaults(settings.ios?.sdkSetup);
            TryInitializeSetupDefaults(activeSetup);

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

            Debug.Log(
                $"[JIS SDK] {reason}: Applied '{settings.name}' for {activeTarget} " +
                $"(mediation={activeProfile.mediation}, active formats={AdsSetupUtility.CountActiveFormats(activeSetup)}).");
            return true;
        }

        static void TryInitializeSetupDefaults(SDKSetup setup) =>
            JisSDKAdsInventorySetupUtility.TryInitializeSetupDefaults(setup);

        public static void SyncJisAdsInScene(JisSDKAdsSettings settings)
        {
            var jisAds = Object.FindFirstObjectByType<JisAds>();
            if (jisAds == null) return;

            var so = new SerializedObject(jisAds);

            var settingsProp = so.FindProperty("settings");
            if (settingsProp != null)
                settingsProp.objectReferenceValue = settings;

            // Keep autoInitializeOnStart in sync with adsInitializationMode:
            // Manual → JisAds should NOT auto-init (game calls InitializeAsync explicitly).
            // AutoOnStart → JisAds auto-inits on Start.
            var autoInitProp = so.FindProperty("autoInitializeOnStart");
            if (autoInitProp != null)
                autoInitProp.boolValue =
                    settings.adsInitializationMode == AdsManager.AdsInitializationMode.AutoOnStart;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(jisAds);
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
            ValidateScriptingDefines(settings.android, BuildTargetGroup.Android, "Android", result);
            ValidateScriptingDefines(settings.ios, BuildTargetGroup.iOS, "iOS", result);
            return result;
        }

        static void ValidateScriptingDefines(
            PlatformAdsProfile profile,
            BuildTargetGroup group,
            string label,
            ValidationResult result)
        {
            if (profile?.sdkSetup == null)
                return;

            var expected = new HashSet<string>(profile.sdkSetup.GetExpectedScriptingDefineSymbols());
            var actual = SymbolHelper.GetDefineSymbols(group);

            foreach (var sym in expected)
            {
                if (!actual.Contains(sym))
                {
                    result.AddWarning(
                        $"{label}: missing scripting define '{sym}' on {group} (click Apply to Scene).");
                }
            }

            foreach (var sym in new[] { "UNITY_AD_MAX", "UNITY_AD_ADMOB" })
            {
                if (!expected.Contains(sym) && actual.Contains(sym))
                {
                    result.AddWarning(
                        $"{label}: stale scripting define '{sym}' on {group} — Apply to Scene to sync.");
                }
            }
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

            if (profile.sdkSetup.adsMediationType != profile.mediation)
            {
                result.AddWarning(
                    $"{label}: SDKSetup.adsMediationType ({profile.sdkSetup.adsMediationType}) " +
                    $"differs from profile mediation ({profile.mediation}). Use Apply to sync.");
            }

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
                && !HasFallbackId(setup.admobAdsSetup.InterstitialTierConfig))
            {
                result.AddWarning(
                    $"{platformLabel}: interstitial tier enabled - set a local fallback ID. Tier IDs must come from Firebase RC keys inter_premium_id ... inter_fill_id.");
            }

            if (setup.rewardedAdsMediationType == AdsMediationType.ADMOB
                && setup.admobAdsSetup.RewardedTierConfig.enableSequentialLadder
                && !HasFallbackId(setup.admobAdsSetup.RewardedTierConfig))
            {
                result.AddWarning(
                    $"{platformLabel}: rewarded tier enabled - set a local fallback ID. Tier IDs must come from Firebase RC keys reward_premium_id ... reward_fill_id.");
            }
        }

        static bool HasFallbackId(SequentialTierConfig config)
        {
            if (config == null) return false;
            return !string.IsNullOrWhiteSpace(config.ResolveDefaultAdUnitId());
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
