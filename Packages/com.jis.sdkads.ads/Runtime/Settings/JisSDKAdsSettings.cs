using JisSDKAds.Common;
using UnityEngine;
using JisSDKAds.Ads;
using System.Collections.Generic;
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

        [Tooltip("Legacy global mode. Use the per-format mediation mode fields below.")]
        [HideInInspector]
        public bool singleMediationOnly = true;

        [Tooltip("Interstitial can use only primary mediation or cross-mediation fallback.")]
        public AdFormatMediationMode interstitialMediationMode = AdFormatMediationMode.Single;

        [Tooltip("Rewarded can use only primary mediation or cross-mediation fallback.")]
        public AdFormatMediationMode rewardedMediationMode = AdFormatMediationMode.Single;

        [SerializeField, HideInInspector] int mediationModeSettingsVersion;

        [Tooltip("When enabled, ads SDK logs init steps, load success/fail with ad unit IDs, and mediation errors to the Unity Console.")]
        public bool enableAdsDebugLogging = false;

        [Tooltip("After SDK init + Remote Config, preload banner, interstitial, rewarded, and app open.")]
        public bool preloadAdsOnGameStart = true;

        [Tooltip("When the player owns Remove Ads, skip all startup ad loads (no banner/interstitial/rewarded/app-open preload).")]
        public bool skipStartupAdLoadWhenRemoveAds = true;

        [Tooltip("Auto fullscreen show priority for interstitial/rewarded. Default: AdMob first, then MAX.")]
        public AdsMediationType autoShowFirstMediation = AdsMediationType.ADMOB;

        [Tooltip("Fallback mediation for auto fullscreen show and explicit show when selected mediation is not loaded.")]
        public AdsMediationType autoShowSecondMediation = AdsMediationType.MAX;

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

        public bool IsMultipleMediationEnabled(AdsType adsType)
        {
            MigrateMediationModeSettingsIfNeeded();
            return adsType switch
            {
                AdsType.INTERSTITIAL => interstitialMediationMode == AdFormatMediationMode.Multiple,
                AdsType.REWARDED => rewardedMediationMode == AdFormatMediationMode.Multiple,
                _ => false
            };
        }

        public bool UsesAnyFullscreenMultipleMediation() =>
            IsMultipleMediationEnabled(AdsType.INTERSTITIAL) || IsMultipleMediationEnabled(AdsType.REWARDED);

        public bool ShouldDelegateConsentToMax()
        {
            if (!UsesAnyFullscreenMultipleMediation())
                return false;

            return GetFullscreenAutoShowPriority().Contains(AdsMediationType.MAX);
        }

        public List<AdsMediationType> GetFullscreenAutoShowPriority()
        {
            var result = new List<AdsMediationType>(2);
            if (!UsesAnyFullscreenMultipleMediation())
            {
                AddMediationIfValid(result, GetActiveMediation());
                return result;
            }

            AddConfiguredFullscreenAutoShowPriority(result);

            var active = GetActiveMediation();
            AddMediationIfValid(result, active);
            AddMediationIfValid(result, active == AdsMediationType.ADMOB ? AdsMediationType.MAX : AdsMediationType.ADMOB);

            return result;
        }

        public List<AdsMediationType> GetFullscreenAutoShowPriority(AdsType adsType)
        {
            var result = new List<AdsMediationType>(2);
            if (!IsMultipleMediationEnabled(adsType))
            {
                AddMediationIfValid(result, GetActiveMediation());
                return result;
            }

            AddConfiguredFullscreenAutoShowPriority(result);

            var active = GetActiveMediation();
            AddMediationIfValid(result, active);
            AddMediationIfValid(result, active == AdsMediationType.ADMOB ? AdsMediationType.MAX : AdsMediationType.ADMOB);

            return result;
        }

        public List<AdsMediationType> GetConfiguredFullscreenAutoShowPriority()
        {
            var result = new List<AdsMediationType>(2);
            if (!UsesAnyFullscreenMultipleMediation())
                return result;

            AddConfiguredFullscreenAutoShowPriority(result);
            return result;
        }

        void AddConfiguredFullscreenAutoShowPriority(List<AdsMediationType> result)
        {
            AddMediationIfValid(result, autoShowFirstMediation);
            AddMediationIfValid(result, autoShowSecondMediation);
        }

        static void AddMediationIfValid(List<AdsMediationType> result, AdsMediationType mediation)
        {
            if (mediation == AdsMediationType.NONE || result.Contains(mediation))
                return;
            result.Add(mediation);
        }

        /// <summary>Sync <see cref="DebugAds"/> from this asset (call on play / Apply to Scene).</summary>
        public void ApplyRuntimeDebugSettings()
        {
            DebugAds.Configure(enableAdsDebugLogging);
        }

        void OnValidate()
        {
            MigrateMediationModeSettingsIfNeeded();
            singleMediationOnly = !UsesAnyFullscreenMultipleMediation();
        }

        void MigrateMediationModeSettingsIfNeeded()
        {
            if (mediationModeSettingsVersion > 0)
                return;

            var legacyMode = singleMediationOnly
                ? AdFormatMediationMode.Single
                : AdFormatMediationMode.Multiple;
            interstitialMediationMode = legacyMode;
            rewardedMediationMode = legacyMode;
            mediationModeSettingsVersion = 1;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Android <see cref="SDKSetup"/> → BuildTargetGroup.Android; iOS → BuildTargetGroup.iOS.
        /// </summary>
        public void ApplyScriptingDefinesForAllPlatforms()
        {
            SyncAllProfileMediationToSdkSetups();
            var fullscreenMediations = GetConfiguredFullscreenAutoShowPriority();

            if (android?.sdkSetup != null)
                android.sdkSetup.ApplyScriptingDefines(BuildTargetGroup.Android, fullscreenMediations);

            if (ios?.sdkSetup != null)
                ios.sdkSetup.ApplyScriptingDefines(BuildTargetGroup.iOS, fullscreenMediations);
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

            profile.sdkSetup.EnsureMediationSetups();
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

    public enum AdFormatMediationMode
    {
        Single = 0,
        Multiple = 1
    }
}
