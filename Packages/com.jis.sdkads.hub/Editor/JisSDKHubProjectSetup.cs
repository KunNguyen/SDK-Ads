#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.Hub
{
    /// <summary>
    /// Creates default project assets under Assets/JisSDKAds after Hub imports.
    /// Uses reflection so Hub does not reference ads/editor assemblies at compile time.
    /// </summary>
    internal static class JisSDKHubProjectSetup
    {
        public const string SettingsFolder = "Assets/JisSDKAds/Settings";
        public const string DefaultSettingsPath = SettingsFolder + "/JisSDKAdsSettings.asset";

        public static void EnsureAdsSettingsAsset()
        {
            EnsureFolder("Assets", "JisSDKAds");
            EnsureFolder("Assets/JisSDKAds", "Settings");

            var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(DefaultSettingsPath);
            if (existing != null)
            {
                InitializeAdsSettingsDefaults(existing);
                return;
            }

            var settingsType = Type.GetType("JisSDKAds.Ads.Settings.JisSDKAdsSettings, JisSDKAds.Ads");
            if (settingsType == null)
            {
                Debug.LogWarning(
                    "[JIS SDK Hub] com.jis.sdkads.ads not loaded yet. Import Ads module first, then run JIS SDK → Create Ads Settings Asset.");
                return;
            }

            var asset = ScriptableObject.CreateInstance(settingsType);
            InitializeAdsSettingsDefaults(asset);
            AssetDatabase.CreateAsset(asset, DefaultSettingsPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[JIS SDK Hub] Created {DefaultSettingsPath}");
        }

        public static void EnsurePlatformSdkSetupStubs()
        {
            var setupType = Type.GetType("JisSDKAds.Ads.SDKSetup, JisSDKAds.Ads");
            if (setupType == null) return;

            CreateSdkSetupIfMissing($"{SettingsFolder}/AndroidSDKSetup.asset", setupType);
            CreateSdkSetupIfMissing($"{SettingsFolder}/IOSSDKSetup.asset", setupType);
            AssignPlatformSetupsToSettings();
            AssetDatabase.SaveAssets();
        }

        public static ScriptableObject LoadAdsSettingsAsset() =>
            AssetDatabase.LoadAssetAtPath<ScriptableObject>(DefaultSettingsPath);

        public static void OpenAdsSettingsAsset()
        {
            EnsureAdsSettingsAsset();
            EnsurePlatformSdkSetupStubs();

            var settings = LoadAdsSettingsAsset();
            if (settings == null)
            {
                EditorUtility.DisplayDialog("JIS SDK Hub", "Could not create or load JisSDKAdsSettings.", "OK");
                return;
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        public static bool TryApplyAdsSettingsToScene(out string message)
        {
            EnsureAdsSettingsAsset();
            EnsurePlatformSdkSetupStubs();

            var settings = LoadAdsSettingsAsset();
            if (settings == null)
            {
                message = "No JisSDKAdsSettings asset found.";
                return false;
            }

            var applierType = Type.GetType("JisSDKAds.Editor.JisSDKAdsSettingsApplier, JisSDKAds.Editor");
            var applyMethod = applierType?.GetMethod(
                "Apply",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { settings.GetType(), typeof(string) },
                null);

            if (applyMethod == null)
            {
                message = "JisSDKAdsSettingsApplier is not available. Import Editor Tools module, then apply from the settings inspector.";
                return false;
            }

            var result = applyMethod.Invoke(null, new object[] { settings, "Hub Apply" });
            var ok = result is bool applied && applied;
            message = ok ? "Applied JisSDKAdsSettings to scene." : "Apply failed. Check Console for details.";
            return ok;
        }

        public static string GetAdsSettingsSummary()
        {
            var settings = LoadAdsSettingsAsset();
            if (settings == null)
                return "Settings: missing";

            var type = settings.GetType();
            var androidAssigned = IsProfileSetupAssigned(type.GetField("android")?.GetValue(settings));
            var iosAssigned = IsProfileSetupAssigned(type.GetField("ios")?.GetValue(settings));
            var interstitialMode = GetFieldValue(type, settings, "interstitialMediationMode", "Single");
            var rewardedMode = GetFieldValue(type, settings, "rewardedMediationMode", "Single");
            var priority1 = GetFieldValue(type, settings, "autoShowFirstMediation", "-");
            var priority2 = GetFieldValue(type, settings, "autoShowSecondMediation", "-");

            return $"Settings: ready | Android setup: {(androidAssigned ? "assigned" : "missing")} | iOS setup: {(iosAssigned ? "assigned" : "missing")}\n" +
                   $"Interstitial: {interstitialMode} | Rewarded: {rewardedMode} | Fullscreen priority: {priority1} > {priority2}";
        }

        static void CreateSdkSetupIfMissing(string path, Type setupType)
        {
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) != null)
                return;

            var asset = ScriptableObject.CreateInstance(setupType);
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[JIS SDK Hub] Created {path} (assign to JisSDKAdsSettings android/ios profiles).");
        }

        static void AssignPlatformSetupsToSettings()
        {
            var settings = LoadAdsSettingsAsset();
            if (settings == null) return;

            var androidSetup = AssetDatabase.LoadAssetAtPath<ScriptableObject>($"{SettingsFolder}/AndroidSDKSetup.asset");
            var iosSetup = AssetDatabase.LoadAssetAtPath<ScriptableObject>($"{SettingsFolder}/IOSSDKSetup.asset");
            AssignSdkSetup(settings, "android", androidSetup);
            AssignSdkSetup(settings, "ios", iosSetup);
            EditorUtility.SetDirty(settings);
        }

        static void AssignSdkSetup(ScriptableObject settings, string profileFieldName, ScriptableObject setup)
        {
            if (settings == null || setup == null) return;

            var profile = settings.GetType()
                .GetField(profileFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(settings);
            if (profile == null) return;

            var sdkSetupField = profile.GetType()
                .GetField("sdkSetup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (sdkSetupField == null || sdkSetupField.GetValue(profile) != null)
                return;

            sdkSetupField.SetValue(profile, setup);
        }

        static void InitializeAdsSettingsDefaults(ScriptableObject settings)
        {
            if (settings == null) return;

            var type = settings.GetType();
            SetEnumFieldIfDefault(type, settings, "interstitialMediationMode", "Single");
            SetEnumFieldIfDefault(type, settings, "rewardedMediationMode", "Single");
            SetEnumFieldIfDefault(type, settings, "autoShowFirstMediation", "ADMOB");
            SetEnumFieldIfDefault(type, settings, "autoShowSecondMediation", "MAX");
            EditorUtility.SetDirty(settings);
        }

        static void SetEnumFieldIfDefault(Type ownerType, object target, string fieldName, string enumName)
        {
            var field = ownerType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null || !field.FieldType.IsEnum)
                return;

            var value = field.GetValue(target);
            if (value != null && Convert.ToInt32(value) != 0)
                return;

            if (Enum.TryParse(field.FieldType, enumName, out var parsed))
                field.SetValue(target, parsed);
        }

        static bool IsProfileSetupAssigned(object profile)
        {
            if (profile == null) return false;
            return profile.GetType()
                .GetField("sdkSetup", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(profile) != null;
        }

        static string GetFieldValue(Type ownerType, object target, string fieldName, string fallback)
        {
            var value = ownerType
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(target);
            return value?.ToString() ?? fallback;
        }

        static void EnsureFolder(string parent, string child)
        {
            var combined = Path.Combine(parent, child).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(combined))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
