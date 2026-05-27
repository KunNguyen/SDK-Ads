using JisSDKAds.Ads;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Common;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JisSDKAds.Providers.Max
{
    public static class MaxMediationConfigBridge
    {
        public static void ApplyFromSdkSetup(AdsManager manager, SDKSetup setup)
        {
#if UNITY_AD_MAX
            if (manager == null || setup == null) return;

            const AdsMediationType adsMediationType = AdsMediationType.MAX;
            var maxMediationController =
                manager.GetAdsMediationController(adsMediationType) as MaxMediationController
                ?? manager.GetComponentInChildren<MaxMediationController>(true);
            if (maxMediationController == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[JIS SDK] MaxMediationController not found under AdsManager.");
#endif
                return;
            }

            maxMediationController.AdsMediationType = adsMediationType;

            if (setup.adsMediationType == adsMediationType)
                maxMediationController.m_MaxAdConfig.SDKKey = setup.maxAdsSetup.SDKKey;

            maxMediationController.m_MaxAdConfig.InterstitialAdUnitID =
                setup.interstitialAdsMediationType == adsMediationType
                    ? setup.maxAdsSetup.InterstitialAdUnitID
                    : "";

            maxMediationController.m_MaxAdConfig.RewardedAdUnitID =
                setup.rewardedAdsMediationType == adsMediationType ? setup.maxAdsSetup.RewardedAdUnitID : "";

            maxMediationController.m_MaxAdConfig.BannerAdUnitID = setup.bannerAdsMediationType == adsMediationType
                ? setup.maxAdsSetup.BannerAdUnitID
                : "";

            maxMediationController.m_BannerPosition = setup.maxBannerAdsPosition;

            maxMediationController.m_MaxAdConfig.AppOpenAdUnitID =
                setup.appOpenAdsMediationType == adsMediationType ? setup.maxAdsSetup.AppOpenAdUnitID : "";

#if UNITY_EDITOR
            EditorUtility.SetDirty(maxMediationController);
            DebugAds.Log("Update Max Mediation Done");
#endif
#endif
        }
    }
}
