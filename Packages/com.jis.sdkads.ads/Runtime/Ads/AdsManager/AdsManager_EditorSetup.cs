using System.Collections.Generic;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Ads.UnitAdManagers;
using JisSDKAds.Ads.UnitAdManagers.Interface;
using JisSDKAds.Ads.Integration;
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
               MainAdsMediationType = ResolvePrimaryMediationType(sdkSetup);
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
               BannerAdManager ??= GetComponentInChildren<BannerAdManager>(true);
               InterstitialAdManager ??= GetComponentInChildren<InterstitialAdManager>(true);
               RewardAdManager ??= GetComponentInChildren<RewardAdManager>(true);
               MRecAdManager ??= GetComponentInChildren<MRecAdManager>(true);
               AppOpenAdManager ??= GetComponentInChildren<AppOpenAdManager>(true);
               CollapsibleBannerAdManager ??= GetComponentInChildren<CollapsibleBannerAdManager>(true);
               ResumeAdManager ??= GetComponentInChildren<ResumeAdManager>(true);

               if (AdsMediationControllers == null || AdsMediationControllers.Count == 0)
               {
                    AdsMediationControllers = new List<AdsMediationController>(
                         GetComponentsInChildren<AdsMediationController>(true));
               }
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
               MaxMediationReflection.ApplySdkSetup(this, CurrentSDKSetup);
#endif
          }

          private void UpdateAdmobMediation()
          {
#if UNITY_AD_ADMOB
               AdMobMediationReflection.ApplySdkSetup(this, CurrentSDKSetup);
#endif
          }

          AdsMediationType ResolvePrimaryMediationType(SDKSetup sdkSetup)
          {
               if (SdkSettings != null)
                    return SdkSettings.GetActiveMediation();
               return sdkSetup != null ? sdkSetup.adsMediationType : AdsMediationType.NONE;
          }

          #endregion
     }
}