using Sirenix.OdinInspector;
using UnityEngine;
using JisSDKAds.Ads;

namespace JisSDKAds.Ads.Settings
{
    [CreateAssetMenu(fileName = "JisSDKAdsSettings", menuName = "JIS SDK/Ads Settings", order = 0)]
    public class JisSDKAdsSettings : ScriptableObject
    {
        [Title("Platform mediation")]
        [InfoBox("One mediation per platform (e.g. AdMob on Android, MAX on iOS).")]
        public PlatformAdsProfile android = new PlatformAdsProfile();

        public PlatformAdsProfile ios = new PlatformAdsProfile();

        [Title("Core AdManager")]
        [Tooltip("When enabled, AdManager will not fall back to another network on the same platform.")]
        public bool singleMediationOnly = true;

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

        public void ApplyToAdsManager(AdsManager adsManager)
        {
            if (adsManager == null) return;

            var profile = GetActiveProfile();
            if (profile?.sdkSetup == null)
            {
                Debug.LogError("[JisSDKAds] Active platform profile has no SDKSetup assigned.");
                return;
            }

#if UNITY_EDITOR
            if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
                adsManager.AndroidSdkSetup = profile.sdkSetup;
            else
                adsManager.IOSSdkSetup = profile.sdkSetup;
#else
            if (Application.platform == RuntimePlatform.Android)
                adsManager.AndroidSdkSetup = profile.sdkSetup;
            else
                adsManager.IOSSdkSetup = profile.sdkSetup;
#endif
            adsManager.MainAdsMediationType = profile.mediation;
        }
    }
}
