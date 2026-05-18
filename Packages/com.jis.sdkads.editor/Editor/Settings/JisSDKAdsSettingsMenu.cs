#if UNITY_EDITOR
using JisSDKAds.Ads.Settings;
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.Editor
{
    public static class JisSDKAdsSettingsMenu
    {
        const string DefaultFolder = "Assets/JisSDKAds/Settings";

        [MenuItem("JIS SDK/Create Ads Settings Asset")]
        public static void CreateSettingsAsset()
        {
            if (!AssetDatabase.IsValidFolder("Assets/JisSDKAds"))
                AssetDatabase.CreateFolder("Assets", "JisSDKAds");
            if (!AssetDatabase.IsValidFolder(DefaultFolder))
                AssetDatabase.CreateFolder("Assets/JisSDKAds", "Settings");

            var existing = AssetDatabase.LoadAssetAtPath<JisSDKAdsSettings>(JisSDKHubBridge.DefaultSettingsPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            var asset = ScriptableObject.CreateInstance<JisSDKAdsSettings>();
            AssetDatabase.CreateAsset(asset, JisSDKHubBridge.DefaultSettingsPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        /// <summary>Paths shared with Hub (duplicate constant to avoid hub → editor reference).</summary>
        internal static class JisSDKHubBridge
        {
            public const string DefaultSettingsPath = "Assets/JisSDKAds/Settings/JisSDKAdsSettings.asset";
        }
    }
}
#endif
