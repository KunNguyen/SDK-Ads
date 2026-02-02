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
               if (CurrentSDKSetup == null) return;
               UpdateAdsMediationConfig(CurrentSDKSetup);
          }

          public void UpdateAdsMediationConfig(SDKSetup sdkSetup)
          {
               CurrentSDKSetup = sdkSetup;
               MainAdsMediationType = CurrentSDKSetup.adsMediationType;
               AdsConfigs.Clear();
               AdsConfigs.Add(new AdsConfig()
               {
                    adsType = AdsType.INTERSTITIAL,
                    adsMediationType = CurrentSDKSetup.GetAdsMediationType(AdsType.BANNER),
                    isActive = true
               });
               AdsConfigs.Add(new AdsConfig()
               {
                    adsType = AdsType.REWARDED,
                    adsMediationType = CurrentSDKSetup.GetAdsMediationType(AdsType.REWARDED),
                    isActive = true
               });
               AdsConfigs.Add(new AdsConfig()
               {
                    adsType = AdsType.BANNER,
                    adsMediationType = CurrentSDKSetup.GetAdsMediationType(AdsType.BANNER),
                    isActive = true
               });
               AdsConfigs.Add(new AdsConfig()
               {
                    adsType = AdsType.COLLAPSIBLE_BANNER,
                    adsMediationType = CurrentSDKSetup.GetAdsMediationType(AdsType.COLLAPSIBLE_BANNER),
                    isActive = true
               });
               AdsConfigs.Add(new AdsConfig()
               {
                    adsType = AdsType.MREC,
                    adsMediationType = CurrentSDKSetup.GetAdsMediationType(AdsType.MREC),
                    isActive = true
               });
               AdsConfigs.Add(new AdsConfig()
               {
                    adsType = AdsType.APP_OPEN,
                    adsMediationType = CurrentSDKSetup.GetAdsMediationType(AdsType.APP_OPEN),
                    isActive = true
               });
               RewardAdManager.IsLinkRewardWithRemoveAds = CurrentSDKSetup.IsLinkToRemoveAds;
               InterstitialAdManager.IsActiveCooldownFromStart = CurrentSDKSetup.IsActiveCooldownInterstitialFromStart;
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
               if (CurrentSDKSetup.interstitialAdsMediationType == adsMediationType)
               {
                    MainAdsMediationType = adsMediationType;
                    admobMediationController.m_AdmobAdSetup.InterstitialAdUnitIDList =
                         CurrentSDKSetup.admobAdsSetup.InterstitialAdUnitIDList;
               }
               else
               {
                    admobMediationController.m_AdmobAdSetup.InterstitialAdUnitIDList = new List<string>();
               }

               admobMediationController.m_AdmobAdSetup.RewardedAdUnitIDList =
                    CurrentSDKSetup.rewardedAdsMediationType == adsMediationType
                         ? CurrentSDKSetup.admobAdsSetup.RewardedAdUnitIDList
                         : new List<string>();

               {
                    admobMediationController.m_AdmobAdSetup.BannerAdUnitIDList =
                         CurrentSDKSetup.bannerAdsMediationType == adsMediationType
                              ? CurrentSDKSetup.admobAdsSetup.BannerAdUnitIDList
                              : new List<string>();
                    admobMediationController.IsBannerShowingOnStart = CurrentSDKSetup.isBannerShowingOnStart;
                    admobMediationController.m_BannerPosition = CurrentSDKSetup.admobBannerAdsPosition;
               }

               {
                    admobMediationController.m_AdmobAdSetup.CollapsibleBannerAdUnitIDList =
                         CurrentSDKSetup.collapsibleBannerAdsMediationType == adsMediationType
                              ? CurrentSDKSetup.admobAdsSetup.CollapsibleBannerAdUnitIDList
                              : new List<string>();
                    admobMediationController.IsCollapsibleBannerShowingOnStart =
                         CurrentSDKSetup.isShowingOnStartCollapsibleBanner;
                    CollapsibleBannerAdManager.IsAutoRefresh = CurrentSDKSetup.isAutoCloseCollapsibleBanner;


                    CollapsibleBannerAdManager.IsAutoRefresh = CurrentSDKSetup.isAutoRefreshCollapsibleBanner;
                    CollapsibleBannerAdManager.AutoRefreshTime = CurrentSDKSetup.autoRefreshTime;

                    admobMediationController.m_CollapsibleBannerPosition = CurrentSDKSetup.adsPositionCollapsibleBanner;
               }
               {
                    admobMediationController.m_AdmobAdSetup.MrecAdUnitIDList =
                         CurrentSDKSetup.mrecAdsMediationType == adsMediationType
                              ? CurrentSDKSetup.admobAdsSetup.MrecAdUnitIDList
                              : new List<string>();
                    admobMediationController.m_MRecPosition = CurrentSDKSetup.mrecAdsPosition;
               }
               admobMediationController.m_AdmobAdSetup.AppOpenAdUnitIDList =
                    CurrentSDKSetup.appOpenAdsMediationType == adsMediationType
                         ? CurrentSDKSetup.admobAdsSetup.AppOpenAdUnitIDList
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
            if (SDKSetup.adsMediationType == adsMediationType)
            {
                ironSourceMediationController.AppKey = SDKSetup.ironSourceAdSetup.appKey;
            }
            
            ironSourceMediationController.interstitialAdUnitID =
                 SDKSetup.interstitialAdsMediationType == adsMediationType ? SDKSetup.ironSourceAdSetup.interstitialID : "";
            ironSourceMediationController.rewardedAdUnitID =
                 SDKSetup.rewardedAdsMediationType == adsMediationType ? SDKSetup.ironSourceAdSetup.rewardedID : "";
            ironSourceMediationController.bannerAdUnitID =
                 SDKSetup.bannerAdsMediationType == adsMediationType ? SDKSetup.ironSourceAdSetup.bannerID : "";
#if UNITY_EDITOR
            EditorUtility.SetDirty(ironSourceMediationController);
            DebugAds.Log("Update IronSource Mediation Done");
#endif
#endif
          }

          #endregion
     }
}