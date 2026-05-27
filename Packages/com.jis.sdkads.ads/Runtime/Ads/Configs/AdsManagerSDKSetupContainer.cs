using JisSDKAds.Ads.Settings;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace JisSDKAds.Ads
{
    /// <summary>
    /// Legacy container — prefer <see cref="JisSDKAdsSettings"/> as single source of truth.
    /// When <see cref="unifiedSettings"/> is assigned, Setup() applies from settings.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AdsManagerSDKSetupContainer",
        menuName = "JIS SDK/Legacy/Ads Setup Container",
        order = 10)]
    public class AdsManagerSDKSetupContainer : ScriptableObject
    {
        [Tooltip("Recommended: assign JisSDKAdsSettings. When set, android/ios below are ignored on Setup.")]
        public JisSDKAdsSettings unifiedSettings;

        [Tooltip("Legacy — use JisSDKAdsSettings.android.sdkSetup instead.")]
        public SDKSetup android;

        [Tooltip("Legacy — use JisSDKAdsSettings.ios.sdkSetup instead.")]
        public SDKSetup ios;

        public AdsManager.AdsInitializationMode adsInitializationMode = AdsManager.AdsInitializationMode.AutoOnStart;

        public SDKSetup GetAndroidSetup() =>
            unifiedSettings?.android?.sdkSetup ?? android;

        public SDKSetup GetIosSetup() =>
            unifiedSettings?.ios?.sdkSetup ?? ios;

#if UNITY_EDITOR
        public void Setup()
        {
            if (unifiedSettings != null)
            {
                ApplyUnifiedSettings();
                return;
            }

            ApplyLegacyContainer();
        }

        void ApplyUnifiedSettings()
        {
            var adsManager = Object.FindFirstObjectByType<AdsManager>();
            if (adsManager != null)
            {
                unifiedSettings.ApplyToAdsManager(adsManager);
                EditorUtility.SetDirty(adsManager);
                EditorSceneManager.MarkSceneDirty(adsManager.gameObject.scene);
            }
            else
            {
                Debug.LogWarning("[AdsManager] No AdsManager in scene — assign JisSDKAdsSettings on JisAds manually.");
            }

            unifiedSettings.ApplyScriptingDefinesForAllPlatforms();
        }

        void ApplyLegacyContainer()
        {
            var adsManager = Object.FindFirstObjectByType<AdsManager>();
            if (adsManager == null)
            {
                Debug.LogError("[AdsManager] Please add AdsManager Prefab to scene.");
                return;
            }

            adsManager.ApplyFromContainer(this);
            EditorUtility.SetDirty(adsManager);
            EditorSceneManager.MarkSceneDirty(adsManager.gameObject.scene);
            if (unifiedSettings != null)
                unifiedSettings.ApplyScriptingDefinesForAllPlatforms();
            else
                GetSetupForActiveBuildTarget()?.SetupSymbol();
        }

        public void SyncLegacyFieldsFromSettings()
        {
            if (unifiedSettings == null) return;
            android = unifiedSettings.android?.sdkSetup;
            ios = unifiedSettings.ios?.sdkSetup;
            adsInitializationMode = unifiedSettings.adsInitializationMode;
            EditorUtility.SetDirty(this);
        }

        SDKSetup GetSetupForActiveBuildTarget()
        {
            return EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android
                ? GetAndroidSetup()
                : GetIosSetup();
        }
#endif
    }
}
