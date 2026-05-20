#if UNITY_EDITOR
using JisSDKAds.Ads;
using JisSDKAds.Ads.Settings;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JisSDKAds.Editor
{
    [InitializeOnLoad]
    public static class AdsManagerAutoPlatformSetup
    {
        const string PrefSwitch = "SDK_ADS_AUTO_SETUP_ON_PLATFORM_SWITCH";
        const string PrefPlay = "SDK_ADS_AUTO_SETUP_ON_PLAY";
        const string PrefBuild = "SDK_ADS_AUTO_SETUP_ON_BUILD";

        const string MenuSwitch = JisSDKMenuPaths.AdsAutoApplyPlatformSwitch;
        const string MenuPlay = JisSDKMenuPaths.AdsAutoApplyOnPlay;
        const string MenuBuild = JisSDKMenuPaths.AdsAutoApplyOnBuild;
        const string MenuApplyNow = JisSDKMenuPaths.AdsAutoApplyNow;

        static AdsManagerAutoPlatformSetup()
        {
            EnsureDefaultPrefs();
            EditorUserBuildSettings.activeBuildTargetChanged += OnActiveBuildTargetChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void EnsureDefaultPrefs()
        {
            if (!EditorPrefs.HasKey(PrefSwitch)) EditorPrefs.SetBool(PrefSwitch, true);
            if (!EditorPrefs.HasKey(PrefBuild)) EditorPrefs.SetBool(PrefBuild, true);
            if (!EditorPrefs.HasKey(PrefPlay)) EditorPrefs.SetBool(PrefPlay, true);
        }

        public static bool TryApplyForActiveBuildTarget(string reason, bool requireAdsManagerInScene)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return false;

            var settings = LoadSettings();
            if (settings != null)
            {
                if (requireAdsManagerInScene && Object.FindFirstObjectByType<AdsManager>() == null)
                {
                    Debug.Log($"[JIS SDK] {reason}: No AdsManager in scene, skipped.");
                    return false;
                }

                return JisSDKAdsSettingsApplier.Apply(settings, reason);
            }

            return TryApplyLegacyContainer(reason, requireAdsManagerInScene);
        }

        public static bool TryApplyForBuildTarget(BuildTarget target, string reason)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return false;

            var settings = LoadSettings();
            if (settings != null)
            {
                var previous = EditorUserBuildSettings.activeBuildTarget;
                try
                {
                    if (previous != target)
                        EditorUserBuildSettings.SwitchActiveBuildTarget(
                            BuildPipeline.GetBuildTargetGroup(target), target);
                    return JisSDKAdsSettingsApplier.Apply(settings, reason);
                }
                finally
                {
                    if (previous != target)
                        EditorUserBuildSettings.SwitchActiveBuildTarget(
                            BuildPipeline.GetBuildTargetGroup(previous), previous);
                }
            }

            return TryApplyLegacyContainerForTarget(target, reason);
        }

        static bool TryApplyLegacyContainer(string reason, bool requireAdsManagerInScene)
        {
            var container = LoadContainer();
            if (container == null)
            {
                Debug.LogWarning($"[JIS SDK] {reason}: No JisSDKAdsSettings or legacy Container found.");
                return false;
            }

            if (requireAdsManagerInScene && Object.FindFirstObjectByType<AdsManager>() == null)
                return false;

            container.Setup();
            Debug.Log($"[JIS SDK] {reason}: Applied legacy Container.");
            return true;
        }

        static bool TryApplyLegacyContainerForTarget(BuildTarget target, string reason)
        {
            var container = LoadContainer();
            if (container == null) return false;

            var setup = target switch
            {
                BuildTarget.Android => container.GetAndroidSetup(),
                BuildTarget.iOS => container.GetIosSetup(),
                _ => null
            };

            if (setup == null) return false;

            var adsManager = Object.FindFirstObjectByType<AdsManager>();
            adsManager?.ApplyFromContainer(container);
            setup.SetupSymbol();
            Debug.Log($"[JIS SDK] {reason}: Applied legacy Container for {target}.");
            return true;
        }

        static void OnActiveBuildTargetChanged()
        {
            if (!EditorPrefs.GetBool(PrefSwitch, true)) return;
            EditorApplication.delayCall += () =>
                TryApplyForActiveBuildTarget("OnPlatformSwitch", requireAdsManagerInScene: false);
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!EditorPrefs.GetBool(PrefPlay, true)) return;
            if (state == PlayModeStateChange.ExitingEditMode)
                TryApplyForActiveBuildTarget("OnPlay", requireAdsManagerInScene: true);
        }

        [MenuItem(MenuSwitch, false, 300)]
        static void ToggleOnSwitch() => TogglePref(PrefSwitch, "Auto apply on platform switch");

        [MenuItem(MenuPlay, false, 301)]
        static void ToggleOnPlay() => TogglePref(PrefPlay, "Auto apply on play");

        [MenuItem(MenuBuild, false, 302)]
        static void ToggleOnBuild() => TogglePref(PrefBuild, "Auto apply on build");

        [MenuItem(MenuSwitch, true)]
        static bool ToggleOnSwitch_Validate()
        {
            Menu.SetChecked(MenuSwitch, EditorPrefs.GetBool(PrefSwitch, true));
            return true;
        }

        [MenuItem(MenuPlay, true)]
        static bool ToggleOnPlay_Validate()
        {
            Menu.SetChecked(MenuPlay, EditorPrefs.GetBool(PrefPlay, true));
            return true;
        }

        [MenuItem(MenuBuild, true)]
        static bool ToggleOnBuild_Validate()
        {
            Menu.SetChecked(MenuBuild, EditorPrefs.GetBool(PrefBuild, true));
            return true;
        }

        [MenuItem(MenuApplyNow, false, 303)]
        static void ApplyNow() =>
            TryApplyForActiveBuildTarget("ManualApplyNow", requireAdsManagerInScene: false);

        static void TogglePref(string key, string label)
        {
            var enabled = !EditorPrefs.GetBool(key, true);
            EditorPrefs.SetBool(key, enabled);
            Debug.Log($"[JIS SDK] {label}: {(enabled ? "ON" : "OFF")}");
        }

        static JisSDKAdsSettings LoadSettings()
        {
            var path = JisSDKAdsSettingsMenu.DefaultSettingsPath;
            var asset = AssetDatabase.LoadAssetAtPath<JisSDKAdsSettings>(path);
            if (asset != null) return asset;

            var guids = AssetDatabase.FindAssets("t:JisSDKAdsSettings");
            if (guids == null || guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<JisSDKAdsSettings>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        static AdsManagerSDKSetupContainer LoadContainer()
        {
            var guids = AssetDatabase.FindAssets("t:AdsManagerSDKSetupContainer");
            if (guids == null || guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<AdsManagerSDKSetupContainer>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        internal static bool IsBuildAutoApplyEnabled() => EditorPrefs.GetBool(PrefBuild, true);
    }

    class AdsManagerAutoPlatformSetupPreBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!AdsManagerAutoPlatformSetup.IsBuildAutoApplyEnabled()) return;
            AdsManagerAutoPlatformSetup.TryApplyForBuildTarget(report.summary.platform, "PreBuild");
        }
    }
}
#endif
