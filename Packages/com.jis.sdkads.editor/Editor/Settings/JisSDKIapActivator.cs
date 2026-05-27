#if UNITY_EDITOR
using JisSDKAds.Ads;
using JisSDKAds.Ads.Settings;
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.Editor
{
    /// <summary>
    /// Keeps IAP visible on the menu bar when inactive and offers a one-click enable path.
    /// </summary>
    public static class JisSDKIapActivator
    {
        const string IapDefineSymbol = "UNITY_IAP_ACTIVE";

        [MenuItem(JisSDKMenuPaths.IapEnable, false, 0)]
        public static void EnableIapFromMenu()
        {
            if (TryEnableIap(out var message))
                Debug.Log($"[JIS SDK IAP] {message}");
            else
                Debug.LogWarning($"[JIS SDK IAP] {message}");
        }

        [MenuItem(JisSDKMenuPaths.IapEnable, true)]
        public static bool EnableIapFromMenuValidate() => !IsIapActive();

        [MenuItem(JisSDKMenuPaths.GameObjectEnableIap, false, 0)]
        public static void EnableIapFromGameObjectMenu() => EnableIapFromMenu();

        [MenuItem(JisSDKMenuPaths.GameObjectEnableIap, true)]
        public static bool EnableIapFromGameObjectMenuValidate() => !IsIapActive();

        public static bool IsIapActive()
        {
#if UNITY_IAP_ACTIVE
            return true;
#else
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);
            return defines.Contains(IapDefineSymbol);
#endif
        }

        public static bool TryEnableIap(out string message)
        {
            if (IsIapActive())
            {
                message = "IAP is already enabled.";
                return true;
            }

            var settings = LoadSettingsAsset();
            if (settings == null)
            {
                message =
                    "No JisSDKAdsSettings found. Create one via JIS SDK → Ads → Create Settings Asset, then try again.";
                return false;
            }

            var hasSetup = false;
            if (settings.android?.sdkSetup != null)
            {
                settings.android.sdkSetup.IsActiveIAP = true;
                EditorUtility.SetDirty(settings.android.sdkSetup);
                hasSetup = true;
            }

            if (settings.ios?.sdkSetup != null)
            {
                settings.ios.sdkSetup.IsActiveIAP = true;
                EditorUtility.SetDirty(settings.ios.sdkSetup);
                hasSetup = true;
            }

            if (!hasSetup)
            {
                message = "JisSDKAdsSettings has no SDKSetup assigned — assign Android/iOS setup first.";
                return false;
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            settings.ApplyScriptingDefinesForAllPlatforms();

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);

            message =
                "IAP enabled on JisSDKAdsSettings. Unity will recompile with UNITY_IAP_ACTIVE; " +
                "full IAP menu items appear after reload.";
            return true;
        }

        static JisSDKAdsSettings LoadSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<JisSDKAdsSettings>(
                JisSDKAdsSettingsMenu.DefaultSettingsPath);
            if (settings != null) return settings;

            var guids = AssetDatabase.FindAssets("t:JisSDKAdsSettings");
            if (guids.Length == 0) return null;

            return AssetDatabase.LoadAssetAtPath<JisSDKAdsSettings>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
#endif
