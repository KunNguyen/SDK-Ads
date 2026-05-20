using System.Collections.Generic;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Ads.UnitAdManagers;
using JisSDKAds.Ads.UnitAdManagers.Interface;
using JisSDKAds.Common;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JisSDKAds.Ads
{
     public partial class AdsManager
     {
          #region EditorUpdate

#if UNITY_EDITOR
          /// <summary>
          /// Gán Android/IOS setup từ Container vào AdsManager, sau đó apply config theo build target hiện tại.
          /// </summary>
          public void ApplyFromContainer(AdsManagerSDKSetupContainer container)
          {
               if (container == null) return;

               if (container.unifiedSettings != null)
               {
                    container.unifiedSettings.ApplyToAdsManager(this);
                    return;
               }

               AndroidSdkSetup = container.android;
               IOSSdkSetup = container.ios;
               InitializationMode = container.adsInitializationMode;
               var setup = EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android
                    ? container.android
                    : container.ios;
               if (setup != null)
                    UpdateAdsMediationConfig(setup);
          }

          public void ApplyFromSettings(JisSDKAdsSettings settings)
          {
               if (settings == null) return;
               settings.ApplyToAdsManager(this);
          }
#endif

          public void UpdateAdsMediationConfig()
          {
               if (CurrentSDKSetup == null) return;
               UpdateAdsMediationConfig(CurrentSDKSetup);
          }

          public void UpdateAdsMediationConfig(SDKSetup sdkSetup)
          {
               if (sdkSetup == null) return;

               CurrentSDKSetup = sdkSetup;
               MainAdsMediationType = CurrentSDKSetup.adsMediationType;
               AdsConfigs ??= new List<AdsConfig>();
               AdsConfigs.Clear();
               AddAdsConfig(AdsType.INTERSTITIAL);
               AddAdsConfig(AdsType.REWARDED);
               AddAdsConfig(AdsType.BANNER);
               AddAdsConfig(AdsType.COLLAPSIBLE_BANNER);
               AddAdsConfig(AdsType.MREC);
               AddAdsConfig(AdsType.APP_OPEN);
#if UNITY_EDITOR
               EnsureEditorSubManagersWired();
#endif
               if (RewardAdManager != null)
                    RewardAdManager.IsLinkRewardWithRemoveAds = CurrentSDKSetup.IsLinkToRemoveAds;
               if (InterstitialAdManager != null)
                    InterstitialAdManager.IsActiveCooldownFromStart = CurrentSDKSetup.IsActiveCooldownInterstitialFromStart;
               UpdateMaxMediation();
               UpdateAdmobMediation();
          }

#if UNITY_EDITOR
          void EnsureEditorSubManagersWired()
          {
               var go = gameObject;
               BannerAdManager ??= go.GetComponent<BannerAdManager>();
               InterstitialAdManager ??= go.GetComponent<InterstitialAdManager>();
               RewardAdManager ??= go.GetComponent<RewardAdManager>();
               MRecAdManager ??= go.GetComponent<MRecAdManager>();
               AppOpenAdManager ??= go.GetComponent<AppOpenAdManager>();
               CollapsibleBannerAdManager ??= go.GetComponent<CollapsibleBannerAdManager>();
               ResumeAdManager ??= go.GetComponent<ResumeAdManager>();
          }
#endif

          void AddAdsConfig(AdsType adsType)
          {
               AdsConfigs.Add(new AdsConfig
               {
                    adsType = adsType,
                    adsMediationType = CurrentSDKSetup.GetAdsMediationType(adsType),
                    isActive = CurrentSDKSetup.IsActiveAdsType(adsType)
               });
          }

          private void UpdateMaxMediation()
          {
#if UNITY_AD_MAX
               const AdsMediationType adsMediationType = AdsMediationType.MAX;
               MaxMediationController maxMediationController =
                    GetAdsMediationController(adsMediationType) as MaxMediationController;
               if (maxMediationController == null) return;
               if (CurrentSDKSetup.adsMediationType == adsMediationType)
               {
                    maxMediationController.m_MaxAdConfig.SDKKey = CurrentSDKSetup.maxAdsSetup.SDKKey;
               }

               maxMediationController.m_MaxAdConfig.InterstitialAdUnitID =
                    CurrentSDKSetup.interstitialAdsMediationType == adsMediationType
                         ? CurrentSDKSetup.maxAdsSetup.InterstitialAdUnitID
                         : "";

               maxMediationController.m_MaxAdConfig.RewardedAdUnitID =
                    CurrentSDKSetup.rewardedAdsMediationType == adsMediationType ? CurrentSDKSetup.maxAdsSetup.RewardedAdUnitID : "";

               maxMediationController.m_MaxAdConfig.BannerAdUnitID = CurrentSDKSetup.bannerAdsMediationType == adsMediationType
                    ? CurrentSDKSetup.maxAdsSetup.BannerAdUnitID
                    : "";
#if UNITY_AD_MAX
               maxMediationController.m_BannerPosition = CurrentSDKSetup.maxBannerAdsPosition;
#endif

               maxMediationController.m_MaxAdConfig.CollapsibleBannerAdUnitID =
                    CurrentSDKSetup.collapsibleBannerAdsMediationType == adsMediationType
                         ? CurrentSDKSetup.maxAdsSetup.CollapsibleBannerAdUnitID
                         : "";

               maxMediationController.m_MaxAdConfig.MrecAdUnitID = CurrentSDKSetup.mrecAdsMediationType == adsMediationType
                    ? CurrentSDKSetup.maxAdsSetup.MrecAdUnitID
                    : "";

               maxMediationController.m_MaxAdConfig.AppOpenAdUnitID =
                    CurrentSDKSetup.appOpenAdsMediationType == adsMediationType ? CurrentSDKSetup.maxAdsSetup.AppOpenAdUnitID : "";

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

          #endregion
     }
}