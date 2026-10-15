#if UNITY_EDITOR
using JisSDKAds.Ads.Settings;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace JisSDKAds.Editor
{
    /// <summary>Applies JisSDKAdsSettings (scripting defines + JisAds wiring) on platform switch, play and build.</summary>
    [InitializeOnLoad]
    public static class JisAdsAutoPlatformSetup
    {
        const string PrefSwitch = "SDK_ADS_AUTO_SETUP_ON_PLATFORM_SWITCH";
        const string PrefPlay = "SDK_ADS_AUTO_SETUP_ON_PLAY";
        const string PrefBuild = "SDK_ADS_AUTO_SETUP_ON_BUILD";

        const string MenuSwitch = JisSDKMenuPaths.AdsAutoApplyPlatformSwitch;
        const string MenuPlay = JisSDKMenuPaths.AdsAutoApplyOnPlay;
        const string MenuBuild = JisSDKMenuPaths.AdsAutoApplyOnBuild;
        const string MenuApplyNow = JisSDKMenuPaths.AdsAutoApplyNow;

        static JisAdsAutoPlatformSetup()
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

        public static bool TryApplyForActiveBuildTarget(string reason, bool requireJisAdsInScene)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return false;

            var settings = JisSDKAdsSettingsApplier.TryLoadDefaultSettings();
            if (settings == null)
            {
                Debug.LogWarning($"[JIS SDK] {reason}: No JisSDKAdsSettings asset found.");
                return false;
            }

            if (requireJisAdsInScene && Object.FindFirstObjectByType<JisSDKAds.Ads.JisAds>() == null)
            {
                Debug.Log($"[JIS SDK] {reason}: No JisAds in scene, skipped.");
                return false;
            }

            return JisSDKAdsSettingsApplier.Apply(settings, reason);
        }

        public static bool TryApplyForBuildTarget(BuildTarget target, string reason)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return false;

            var settings = JisSDKAdsSettingsApplier.TryLoadDefaultSettings();
            if (settings == null)
                return false;

            var previous = EditorUserBuildSettings.activeBuildTarget;
            try
            {
                if (previous != target)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(target), target);
                return JisSDKAdsSettingsApplier.Apply(settings, reason);
            }
            finally
            {
                if (previous != target)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(previous), previous);
            }
        }

        static void OnActiveBuildTargetChanged()
        {
            if (!EditorPrefs.GetBool(PrefSwitch, true)) return;
            EditorApplication.delayCall += () =>
                TryApplyForActiveBuildTarget("OnPlatformSwitch", requireJisAdsInScene: false);
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!EditorPrefs.GetBool(PrefPlay, true)) return;
            if (state == PlayModeStateChange.ExitingEditMode)
                TryApplyForActiveBuildTarget("OnPlay", requireJisAdsInScene: true);
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
            TryApplyForActiveBuildTarget("ManualApplyNow", requireJisAdsInScene: false);

        static void TogglePref(string key, string label)
        {
            var enabled = !EditorPrefs.GetBool(key, true);
            EditorPrefs.SetBool(key, enabled);
            Debug.Log($"[JIS SDK] {label}: {(enabled ? "ON" : "OFF")}");
        }

        internal static bool IsBuildAutoApplyEnabled() => EditorPrefs.GetBool(PrefBuild, true);
    }

    class JisAdsAutoPlatformSetupPreBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!JisAdsAutoPlatformSetup.IsBuildAutoApplyEnabled()) return;
            JisAdsAutoPlatformSetup.TryApplyForBuildTarget(report.summary.platform, "PreBuild");
        }
    }
}
#endif
