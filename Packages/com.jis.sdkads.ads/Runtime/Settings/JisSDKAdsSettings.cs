using JisSDKAds.Common;
using UnityEngine;
using JisSDKAds.Ads;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JisSDKAds.Ads.Settings
{
    [CreateAssetMenu(fileName = "JisSDKAdsSettings", menuName = "JIS SDK/Ads Settings", order = 0)]
    public class JisSDKAdsSettings : ScriptableObject
    {
        [HideInInspector] public PlatformAdsProfile android = new PlatformAdsProfile();

        [HideInInspector] public PlatformAdsProfile ios = new PlatformAdsProfile();

        [Tooltip("Manual (recommended): call JisAds.InitializeAsync() from loading — fetches Remote Config then inits ads. AutoOnStart: AdsManager bootstraps on Start (prototypes only; do not use with JisAds auto-init).")]
        public AdsManager.AdsInitializationMode adsInitializationMode = AdsManager.AdsInitializationMode.Manual;

        [Tooltip("When enabled, AdManager will not fall back to another network on the same platform.")]
        public bool singleMediationOnly = true;

        [Tooltip("When enabled, ads SDK logs init steps, load success/fail with ad unit IDs, and mediation errors to the Unity Console.")]
        public bool enableAdsDebugLogging = false;

        public PlatformAdsProfile GetProfile(BuildTargetPlatform platform) =>
            platform == BuildTargetPlatform.iOS ? ios : android;

        public PlatformAdsProfile GetActiveProfile()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android
                    ? android
                    : ios;
            }
#endif
            return Application.platform == RuntimePlatform.Android ? android : ios;
        }

        public SDKSetup GetActiveSdkSetup() => GetActiveProfile()?.sdkSetup;

        public AdsMediationType GetActiveMediation() =>
            GetActiveProfile() != null ? GetActiveProfile().mediation : AdsMediationType.NONE;

        /// <summary>Sync <see cref="DebugAds"/> from this asset (call on play / Apply to Scene).</summary>
        public void ApplyRuntimeDebugSettings()
        {
            DebugAds.Configure(enableAdsDebugLogging);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Android <see cref="SDKSetup"/> → BuildTargetGroup.Android; iOS → BuildTargetGroup.iOS.
        /// </summary>
        public void ApplyScriptingDefinesForAllPlatforms()
        {
            SyncAllProfileMediationToSdkSetups();

            if (android?.sdkSetup != null)
                android.sdkSetup.ApplyScriptingDefines(BuildTargetGroup.Android);

            if (ios?.sdkSetup != null)
                ios.sdkSetup.ApplyScriptingDefines(BuildTargetGroup.iOS);
        }
#endif

        /// <summary>Assign both platform SDKSetups and sync active build target config to AdsManager.</summary>
        public void ApplyToAdsManager(AdsManager adsManager)
        {
            if (adsManager == null) return;

            ApplyRuntimeDebugSettings();
            SyncAllProfileMediationToSdkSetups();

            adsManager.SdkSettings = this;
            adsManager.AndroidSdkSetup = android?.sdkSetup;
            adsManager.IOSSdkSetup = ios?.sdkSetup;
            adsManager.InitializationMode = adsInitializationMode;

            var profile = GetActiveProfile();
            if (profile?.sdkSetup == null)
            {
                Debug.LogError("[JisSDKAds] Active platform profile has no SDKSetup assigned.");
                return;
            }

            adsManager.UpdateAdsMediationConfig(profile.sdkSetup);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                adsManager.RebindUnitManagersToMediation();
#endif
        }

        /// <summary>
        /// Keeps legacy <see cref="SDKSetup.adsMediationType"/> aligned with profile primary mediation
        /// (scripting defines, bridges, and editor refresh).
        /// </summary>
        public void SyncAllProfileMediationToSdkSetups()
        {
            SyncProfileMediationToSdkSetup(android);
            SyncProfileMediationToSdkSetup(ios);
        }

        public static void SyncProfileMediationToSdkSetup(PlatformAdsProfile profile)
        {
            if (profile?.sdkSetup == null)
                return;

            if (profile.sdkSetup.adsMediationType == profile.mediation)
                return;

            profile.sdkSetup.adsMediationType = profile.mediation;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(profile.sdkSetup);
#endif
        }
    }

    public enum BuildTargetPlatform
    {
        Android = 0,
        iOS = 1
    }
}
