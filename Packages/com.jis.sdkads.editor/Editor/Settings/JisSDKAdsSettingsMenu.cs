#if UNITY_EDITOR
using JisSDKAds.Ads;
using JisSDKAds.Ads.Settings;
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.Editor
{
    public static class JisSDKAdsSettingsMenu
    {
        public const string DefaultFolder = "Assets/JisSDKAds/Settings";
        public const string DefaultSettingsPath = DefaultFolder + "/JisSDKAdsSettings.asset";
        const string AndroidSetupPath = DefaultFolder + "/AndroidSDKSetup.asset";
        const string IosSetupPath = DefaultFolder + "/IOSSDKSetup.asset";

        [MenuItem(JisSDKMenuPaths.AdsCreateSettings, false, 100)]
        public static void CreateSettingsAsset()
        {
            EnsureFolders();

            var existing = AssetDatabase.LoadAssetAtPath<JisSDKAdsSettings>(DefaultSettingsPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            var androidSetup = LoadOrCreateSetup<SDKSetup>(AndroidSetupPath);
            var iosSetup = LoadOrCreateSetup<SDKSetup>(IosSetupPath);

            var asset = ScriptableObject.CreateInstance<JisSDKAdsSettings>();
            asset.android.sdkSetup = androidSetup;
            asset.ios.sdkSetup = iosSetup;

            AssetDatabase.CreateAsset(asset, DefaultSettingsPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log("[JIS SDK] Created JisSDKAdsSettings with Android/iOS SDKSetup stubs.");
        }

        [MenuItem(JisSDKMenuPaths.AdsApplyToScene, false, 101)]
        public static void ApplyToScene()
        {
            var settings = AssetDatabase.LoadAssetAtPath<JisSDKAdsSettings>(DefaultSettingsPath);
            if (settings == null)
            {
                var guids = AssetDatabase.FindAssets("t:JisSDKAdsSettings");
                if (guids.Length > 0)
                    settings = AssetDatabase.LoadAssetAtPath<JisSDKAdsSettings>(
                        AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            if (settings == null)
            {
                Debug.LogWarning("[JIS SDK] Create Settings Asset first (JIS SDK → Ads → Create Settings Asset).");
                return;
            }

            JisSDKAdsSettingsApplier.Apply(settings, "Menu Apply");
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/JisSDKAds"))
                AssetDatabase.CreateFolder("Assets", "JisSDKAds");
            if (!AssetDatabase.IsValidFolder(DefaultFolder))
                AssetDatabase.CreateFolder("Assets/JisSDKAds", "Settings");
        }

        static T LoadOrCreateSetup<T>(string path) where T : ScriptableObject
        {
            EnsureFolders();
            var setup = AssetDatabase.LoadAssetAtPath<T>(path);
            if (setup != null) return setup;
            setup = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(setup, path);
            return setup;
        }

        internal static class JisSDKHubBridge
        {
            public const string DefaultSettingsPath = JisSDKAdsSettingsMenu.DefaultSettingsPath;
        }
    }
}
#endif
