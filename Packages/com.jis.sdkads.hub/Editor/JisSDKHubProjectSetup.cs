#if UNITY_EDITOR
using System;
using System.IO;
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

            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(DefaultSettingsPath) != null)
                return;

            var settingsType = Type.GetType("JisSDKAds.Ads.Settings.JisSDKAdsSettings, JisSDKAds.Ads");
            if (settingsType == null)
            {
                Debug.LogWarning(
                    "[JIS SDK Hub] com.jis.sdkads.ads not loaded yet. Import Ads module first, then run JIS SDK → Create Ads Settings Asset.");
                return;
            }

            var asset = ScriptableObject.CreateInstance(settingsType);
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
        }

        static void CreateSdkSetupIfMissing(string path, Type setupType)
        {
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) != null)
                return;

            var asset = ScriptableObject.CreateInstance(setupType);
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[JIS SDK Hub] Created {path} (assign to JisSDKAdsSettings android/ios profiles).");
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
