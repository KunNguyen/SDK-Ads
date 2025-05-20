using System.Collections.Generic;
using ABIMaxSDKAds.Scripts;
using UnityEditor;

namespace SDK
{
     public partial class AdsManager
     {
          #region EditorUpdate

          public void UpdateAdsMediationConfig()
          {
               if (SDKSetup == null) return;
               UpdateAdsMediationConfig(SDKSetup);
          }

          public void UpdateAdsMediationConfig(SDKSetup sdkSetup)
          {
               SDKSetup = sdkSetup;
               MainAdsMediationType = SDKSetup.adsMediationType;
               AdsConfigs.Clear();
               AdsConfigs.Add(new AdsConfig()
               {
                    adsType = AdsType.INTERSTITIAL,
                    adsMediationType = SDKSetup.GetAdsMediationType(AdsType.BANNER),
                    isActive = true
               });
               AdsConfigs.Add(new AdsConfig()
               {
                    adsType = AdsType.REWARDED,
                    adsMediationType = SDKSetup.GetAdsMediationType(AdsType.REWARDED),
                    isActive = true
               });
               AdsConfigs.Add(new AdsConfig()
               {
                    adsType = AdsType.BANNER,
                    adsMediationType = SDKSetup.GetAdsMediationType(AdsType.BANNER),
                    isActive = true
               });
               AdsConfigs.Add(new AdsConfig()
               {
                    adsType = AdsType.COLLAPSIBLE_BANNER,
                    adsMediationType = SDKSetup.GetAdsMediationType(AdsType.COLLAPSIBLE_BANNER),
                    isActive = true
               });
               AdsConfigs.Add(new AdsConfig()
               {
                    adsType = AdsType.MREC,
                    adsMediationType = SDKSetup.GetAdsMediationType(AdsType.MREC),
                    isActive = true
               });
               AdsConfigs.Add(new AdsConfig()
               {
                    adsType = AdsType.APP_OPEN,
                    adsMediationType = SDKSetup.GetAdsMediationType(AdsType.APP_OPEN),
                    isActive = true
               });
               RewardAdManager.IsLinkRewardWithRemoveAds = SDKSetup.IsLinkToRemoveAds;
               InterstitialAdManager.IsActiveCooldownFromStart = SDKSetup.IsActiveCooldownInterstitialFromStart;
               UpdateMaxMediation();
               UpdateAdmobMediation();
               UpdateIronSourceMediation();
          }

          private void UpdateMaxMediation()
          {
#if UNITY_AD_MAX
               const AdsMediationType adsMediationType = AdsMediationType.MAX;
               MaxMediationController maxMediationController =
                    GetAdsMediationController(adsMediationType) as MaxMediationController;
               if (maxMediationController == null) return;
               if (SDKSetup.adsMediationType == adsMediationType)
               {
                    maxMediationController.m_MaxAdConfig.SDKKey = SDKSetup.maxAdsSetup.SDKKey;
               }

               maxMediationController.m_MaxAdConfig.InterstitialAdUnitID =
                    SDKSetup.interstitialAdsMediationType == adsMediationType
                         ? SDKSetup.maxAdsSetup.InterstitialAdUnitID
                         : "";

               maxMediationController.m_MaxAdConfig.RewardedAdUnitID =
                    SDKSetup.rewardedAdsMediationType == adsMediationType ? SDKSetup.maxAdsSetup.RewardedAdUnitID : "";

               maxMediationController.m_MaxAdConfig.BannerAdUnitID = SDKSetup.bannerAdsMediationType == adsMediationType
                    ? SDKSetup.maxAdsSetup.BannerAdUnitID
                    : "";
#if UNITY_AD_MAX
               maxMediationController.m_BannerPosition = SDKSetup.maxBannerAdsPosition;
#endif

               maxMediationController.m_MaxAdConfig.CollapsibleBannerAdUnitID =
                    SDKSetup.collapsibleBannerAdsMediationType == adsMediationType
                         ? SDKSetup.maxAdsSetup.CollapsibleBannerAdUnitID
                         : "";

               maxMediationController.m_MaxAdConfig.MrecAdUnitID = SDKSetup.mrecAdsMediationType == adsMediationType
                    ? SDKSetup.maxAdsSetup.MrecAdUnitID
                    : "";

               maxMediationController.m_MaxAdConfig.AppOpenAdUnitID =
                    SDKSetup.appOpenAdsMediationType == adsMediationType ? SDKSetup.maxAdsSetup.AppOpenAdUnitID : "";

#if UNITY_EDITOR
               EditorUtility.SetDirty(maxMediationController);
               DebugAds.Log("Update Max Mediation Done");
#endif
#endif
          }

          private void UpdateAdmobMediation()
          {
#if UNITY_AD_ADMOB
               const AdsMediationType adsMediationType = AdsMediationType.ADMOB;
               AdmobMediationController admobMediationController =
                    GetAdsMediationController(adsMediationType) as AdmobMediationController;
               if (admobMediationController == null) return;
               if (SDKSetup.interstitialAdsMediationType == adsMediationType)
               {
                    MainAdsMediationType = adsMediationType;
                    admobMediationController.m_AdmobAdSetup.InterstitialAdUnitIDList =
                         SDKSetup.admobAdsSetup.InterstitialAdUnitIDList;
               }
               else
               {
                    admobMediationController.m_AdmobAdSetup.InterstitialAdUnitIDList = new List<string>();
               }

               admobMediationController.m_AdmobAdSetup.RewardedAdUnitIDList =
                    SDKSetup.rewardedAdsMediationType == adsMediationType
                         ? SDKSetup.admobAdsSetup.RewardedAdUnitIDList
                         : new List<string>();

               {
                    admobMediationController.m_AdmobAdSetup.BannerAdUnitIDList =
                         SDKSetup.bannerAdsMediationType == adsMediationType
                              ? SDKSetup.admobAdsSetup.BannerAdUnitIDList
                              : new List<string>();
                    admobMediationController.IsBannerShowingOnStart = SDKSetup.isBannerShowingOnStart;
                    admobMediationController.m_BannerPosition = SDKSetup.admobBannerAdsPosition;
               }

               {
                    admobMediationController.m_AdmobAdSetup.CollapsibleBannerAdUnitIDList =
                         SDKSetup.collapsibleBannerAdsMediationType == adsMediationType
                              ? SDKSetup.admobAdsSetup.CollapsibleBannerAdUnitIDList
                              : new List<string>();
                    admobMediationController.IsCollapsibleBannerShowingOnStart =
                         SDKSetup.isShowingOnStartCollapsibleBanner;
                    CollapsibleBannerAdManager.IsAutoRefresh = SDKSetup.isAutoCloseCollapsibleBanner;


                    CollapsibleBannerAdManager.IsAutoRefresh = SDKSetup.isAutoRefreshCollapsibleBanner;
                    CollapsibleBannerAdManager.AutoRefreshTime = SDKSetup.autoRefreshTime;

                    admobMediationController.m_CollapsibleBannerPosition = SDKSetup.adsPositionCollapsibleBanner;
               }
               {
                    admobMediationController.m_AdmobAdSetup.MrecAdUnitIDList =
                         SDKSetup.mrecAdsMediationType == adsMediationType
                              ? SDKSetup.admobAdsSetup.MrecAdUnitIDList
                              : new List<string>();
                    admobMediationController.m_MRecPosition = SDKSetup.mrecAdsPosition;
               }
               admobMediationController.m_AdmobAdSetup.AppOpenAdUnitIDList =
                    SDKSetup.appOpenAdsMediationType == adsMediationType
                         ? SDKSetup.admobAdsSetup.AppOpenAdUnitIDList
                         : new List<string>();
#if UNITY_EDITOR
               EditorUtility.SetDirty(admobMediationController);
               DebugAds.Log("Update Admob Mediation Done");
#endif
#endif
          }

          private void UpdateIronSourceMediation()
          {
#if UNITY_AD_IRONSOURCE
            const AdsMediationType adsMediationType = AdsMediationType.IRONSOURCE;
            IronSourceMediationController ironSourceMediationController =
 GetAdsMediationController(adsMediationType) as IronSourceMediationController;
            if (ironSourceMediationController == null) return;
            if (m_SDKSetup.adsMediationType == adsMediationType)
            {
                ironSourceMediationController.AppKey = m_SDKSetup.ironSourceAdSetup.appKey;
            }
            
            ironSourceMediationController.interstitialAdUnitID =
 m_SDKSetup.interstitialAdsMediationType == adsMediationType ? m_SDKSetup.ironSourceAdSetup.interstitialID : "";
            ironSourceMediationController.rewardedAdUnitID =
 m_SDKSetup.rewardedAdsMediationType == adsMediationType ? m_SDKSetup.ironSourceAdSetup.rewardedID : "";
            ironSourceMediationController.bannerAdUnitID =
 m_SDKSetup.bannerAdsMediationType == adsMediationType ? m_SDKSetup.ironSourceAdSetup.bannerID : "";
            isAutoRefreshBannerByCode = m_SDKSetup.isAutoRefreshBannerByCode;
#if UNITY_EDITOR
            EditorUtility.SetDirty(ironSourceMediationController);
            DebugAds.Log("Update IronSource Mediation Done");
#endif
#endif
          }

          #endregion
     }
}