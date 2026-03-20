#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SDK
{
    [InitializeOnLoad]
    public static class AdsManagerAutoPlatformSetup
    {
        private const string PrefSwitch = "SDK_ADS_AUTO_SETUP_ON_PLATFORM_SWITCH";
        private const string PrefPlay = "SDK_ADS_AUTO_SETUP_ON_PLAY";
        private const string PrefBuild = "SDK_ADS_AUTO_SETUP_ON_BUILD";

        private const string MenuSwitch = "SDK Setup/AdsManager/Auto Apply/Toggle On Platform Switch";
        private const string MenuPlay = "SDK Setup/AdsManager/Auto Apply/Toggle On Play";
        private const string MenuBuild = "SDK Setup/AdsManager/Auto Apply/Toggle On Build";

        static AdsManagerAutoPlatformSetup()
        {
            EnsureDefaultPrefs();

            EditorUserBuildSettings.activeBuildTargetChanged += OnActiveBuildTargetChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void EnsureDefaultPrefs()
        {
            if (!EditorPrefs.HasKey(PrefSwitch)) EditorPrefs.SetBool(PrefSwitch, true);
            if (!EditorPrefs.HasKey(PrefBuild)) EditorPrefs.SetBool(PrefBuild, true);

            // Tuỳ bạn: nếu muốn mặc định OFF khi Play thì đổi true -> false
            if (!EditorPrefs.HasKey(PrefPlay)) EditorPrefs.SetBool(PrefPlay, true);
        }

        // --------------------
        // Public entry points
        // --------------------

        public static bool TryApplyForActiveBuildTarget(string reason, bool requireAdsManagerInScene)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return false;

            var container = LoadContainer();
            if (container == null)
            {
                Debug.LogWarning($"[AdsManager] {reason}: Không tìm thấy AdsManagerSDKSetupContainer asset.");
                return false;
            }

            var setup = GetSetupForActiveBuildTarget(container);
            if (setup == null)
            {
                Debug.LogWarning($"[AdsManager] {reason}: Container chưa gán SDKSetup cho {EditorUserBuildSettings.activeBuildTarget}.");
                return false;
            }

            var adsManager = Object.FindFirstObjectByType<AdsManager>();
            if (requireAdsManagerInScene && adsManager == null)
            {
                Debug.Log($"[AdsManager] {reason}: Không có AdsManager trong scene, bỏ qua apply.");
                return false;
            }

            if (adsManager != null)
            {
                adsManager.ApplyFromContainer(container);
                EditorUtility.SetDirty(adsManager);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(adsManager.gameObject.scene);
            }
            setup.SetupSymbol();
            Debug.Log($"[AdsManager] {reason}: Applied SDKSetup '{setup.name}' for {EditorUserBuildSettings.activeBuildTarget}");
            return true;
        }

        public static bool TryApplyForBuildTarget(BuildTarget target, string reason)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return false;

            var container = LoadContainer();
            if (container == null)
            {
                Debug.LogWarning($"[AdsManager] {reason}: Không tìm thấy AdsManagerSDKSetupContainer asset.");
                return false;
            }

            var setup = GetSetupForBuildTarget(container, target);
            if (setup == null)
            {
                Debug.LogWarning($"[AdsManager] {reason}: Container chưa gán SDKSetup cho {target}.");
                return false;
            }

            var adsManager = Object.FindFirstObjectByType<AdsManager>();
            if (adsManager != null)
            {
                adsManager.ApplyFromContainer(container);
                EditorUtility.SetDirty(adsManager);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(adsManager.gameObject.scene);
            }
            setup.SetupSymbol();
            Debug.Log($"[AdsManager] {reason}: Applied SDKSetup '{setup.name}' for {target}");
            return true;
        }

        // --------------------
        // Hooks
        // --------------------

        private static void OnActiveBuildTargetChanged()
        {
            if (!EditorPrefs.GetBool(PrefSwitch, true))
                return;

            // Chờ 1 nhịp để tránh chạy lúc Unity đang reimport
            EditorApplication.delayCall += () =>
            {
                TryApplyForActiveBuildTarget("OnPlatformSwitch", requireAdsManagerInScene: false);
            };
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!EditorPrefs.GetBool(PrefPlay, true))
                return;

            // Trước khi vào Play
            if (state != PlayModeStateChange.ExitingEditMode)
                return;

            TryApplyForActiveBuildTarget("OnPlay", requireAdsManagerInScene: true);
        }

        // --------------------
        // Menu toggles
        // --------------------

        [MenuItem(MenuSwitch)]
        private static void ToggleOnSwitch()
        {
            TogglePref(PrefSwitch, "Auto apply on platform switch");
        }

        [MenuItem(MenuPlay)]
        private static void ToggleOnPlay()
        {
            TogglePref(PrefPlay, "Auto apply on play");
        }

        [MenuItem(MenuBuild)]
        private static void ToggleOnBuild()
        {
            TogglePref(PrefBuild, "Auto apply on build");
        }

        // ---- Checked status (tick) ----
        [MenuItem(MenuSwitch, true)]
        private static bool ToggleOnSwitch_Validate()
        {
            Menu.SetChecked(MenuSwitch, EditorPrefs.GetBool(PrefSwitch, true));
            return true;
        }

        [MenuItem(MenuPlay, true)]
        private static bool ToggleOnPlay_Validate()
        {
            Menu.SetChecked(MenuPlay, EditorPrefs.GetBool(PrefPlay, true));
            return true;
        }

        [MenuItem(MenuBuild, true)]
        private static bool ToggleOnBuild_Validate()
        {
            Menu.SetChecked(MenuBuild, EditorPrefs.GetBool(PrefBuild, true));
            return true;
        }

        [MenuItem("SDK Setup/AdsManager/Auto Apply/Apply Now (Active BuildTarget)")]
        private static void ApplyNow()
        {
            TryApplyForActiveBuildTarget("ManualApplyNow", requireAdsManagerInScene: false);
        }

        private static void TogglePref(string key, string label)
        {
            bool enabled = !EditorPrefs.GetBool(key, true);
            EditorPrefs.SetBool(key, enabled);
            Debug.Log($"[AdsManager] {label}: {(enabled ? "ON" : "OFF")}");
        }

        // --------------------
        // Internals
        // --------------------

        private static AdsManagerSDKSetupContainer LoadContainer()
        {
            // Nếu bạn muốn cố định path (ví dụ Assets/JisSDKConfigs/...), có thể đổi thành LoadAssetAtPath.
            var guids = AssetDatabase.FindAssets("t:AdsManagerSDKSetupContainer");
            if (guids == null || guids.Length == 0) return null;

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AdsManagerSDKSetupContainer>(path);
        }

        private static SDKSetup GetSetupForActiveBuildTarget(AdsManagerSDKSetupContainer container)
        {
            return GetSetupForBuildTarget(container, EditorUserBuildSettings.activeBuildTarget);
        }

        private static SDKSetup GetSetupForBuildTarget(AdsManagerSDKSetupContainer container, BuildTarget target)
        {
            return target switch
            {
                BuildTarget.Android => container.android,
                BuildTarget.iOS => container.ios,
                _ => null
            };
        }

        internal static bool IsBuildAutoApplyEnabled()
        {
            return EditorPrefs.GetBool(PrefBuild, true);
        }
    }

    internal class AdsManagerAutoPlatformSetupPreBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!AdsManagerAutoPlatformSetup.IsBuildAutoApplyEnabled())
                return;

            AdsManagerAutoPlatformSetup.TryApplyForBuildTarget(
                report.summary.platform,
                reason: "PreBuild");
        }
    }
}
#endif
