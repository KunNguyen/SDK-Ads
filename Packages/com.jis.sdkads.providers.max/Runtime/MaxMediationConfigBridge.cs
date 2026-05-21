using JisSDKAds.Ads;
using JisSDKAds.Ads.Settings;
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
                manager.GetAdsMediationController(adsMediationType) as MaxMediationController;
            if (maxMediationController == null) return;

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

            maxMediationController.m_MaxAdConfig.CollapsibleBannerAdUnitID =
                setup.collapsibleBannerAdsMediationType == adsMediationType
                    ? setup.maxAdsSetup.CollapsibleBannerAdUnitID
                    : "";

            maxMediationController.m_MaxAdConfig.MrecAdUnitID = setup.mrecAdsMediationType == adsMediationType
                ? setup.maxAdsSetup.MrecAdUnitID
                : "";

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
