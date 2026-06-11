using System.Collections.Generic;
using System;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Ads.UnitAdManagers;
using JisSDKAds.Ads.Integration;
using JisSDKAds.Common;
using UnityEngine;
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
#if UNITY_EDITOR
               EnsureEditorSubManagersWired();
#endif
               if (RewardAdManager != null)
                    RewardAdManager.IsLinkRewardWithRemoveAds = CurrentSDKSetup.IsLinkToRemoveAds;
               if (InterstitialAdManager != null)
                    InterstitialAdManager.IsActiveCooldownFromStart = CurrentSDKSetup.IsActiveCooldownInterstitialFromStart;
               SyncMediationControllersToSetup();
               UpdateMaxMediation();
               UpdateAdmobMediation();
          }

          void SyncMediationControllersToSetup()
          {
#if UNITY_EDITOR
               EnsureEditorSubManagersWired();
#endif
               if (CurrentSDKSetup == null || AdsMediationControllers == null)
                    return;

               foreach (var controller in AdsMediationControllers)
               {
                    if (controller == null) continue;

                    var mediationType = controller.GetAdsMediationType();
                    controller.AdsMediationType = mediationType;

                    var usedByAnyFormat = false;
                    foreach (AdsType adsType in System.Enum.GetValues(typeof(AdsType)))
                    {
                         if (CurrentSDKSetup.GetAdsMediationType(adsType) == mediationType)
                         {
                              usedByAnyFormat = true;
                              break;
                         }
                    }

                    controller.IsActive = usedByAnyFormat;
#if UNITY_EDITOR
                    EditorUtility.SetDirty(controller);
#endif
               }
          }

#if UNITY_EDITOR
          void EnsureEditorSubManagersWired()
          {
               BannerAdManager ??= GetComponentInChildren<BannerAdManager>(true);
               InterstitialAdManager ??= GetComponentInChildren<InterstitialAdManager>(true);
               RewardAdManager ??= GetComponentInChildren<RewardAdManager>(true);
               EnsureEditorMediationControllersForCurrentSetup();
               AdsMediationControllers = new List<AdsMediationController>(
                    GetComponentsInChildren<AdsMediationController>(true));
          }

          void EnsureEditorMediationControllersForCurrentSetup()
          {
               if (CurrentSDKSetup == null)
                    return;

               EnsureEditorMediationController(MainAdsMediationType);
               foreach (AdsType adsType in Enum.GetValues(typeof(AdsType)))
                    EnsureEditorMediationController(CurrentSDKSetup.GetAdsMediationType(adsType));
          }

          void EnsureEditorMediationController(AdsMediationType mediationType)
          {
               if (mediationType == AdsMediationType.NONE)
                    return;
               if (GetAdsMediationController(mediationType) != null)
                    return;

               foreach (var existing in GetComponentsInChildren<AdsMediationController>(true))
               {
                    if (existing != null && existing.GetAdsMediationType() == mediationType)
                         return;
               }

               var typeName = mediationType switch
               {
                    AdsMediationType.MAX => "JisSDKAds.Ads.MaxMediationController, JisSDKAds.Providers.Max",
                    AdsMediationType.ADMOB => "JisSDKAds.Ads.AdmobMediationController, JisSDKAds.Providers.AdMob",
                    _ => null
               };
               if (string.IsNullOrEmpty(typeName))
                    return;

               var type = Type.GetType(typeName);
               if (type == null || !typeof(AdsMediationController).IsAssignableFrom(type))
                    return;

               var mediationRoot = GetOrCreateEditorChild(transform, "Mediation");
               var childName = mediationType == AdsMediationType.MAX ? "MaxMediation" : "AdmobMediation";
               var host = GetOrCreateEditorChild(mediationRoot.transform, childName);
               var controller = host.GetComponent(type) as AdsMediationController;
               if (controller == null)
                    controller = Undo.AddComponent(host, type) as AdsMediationController;
               if (controller == null)
                    return;

               controller.AdsMediationType = mediationType;
               AdsMediationControllers ??= new List<AdsMediationController>();
               if (!AdsMediationControllers.Contains(controller))
                    AdsMediationControllers.Add(controller);
               EditorUtility.SetDirty(controller);
          }

          static GameObject GetOrCreateEditorChild(Transform parent, string childName)
          {
               foreach (Transform child in parent)
               {
                    if (child.name == childName)
                         return child.gameObject;
               }

               var go = new GameObject(childName);
               Undo.RegisterCreatedObjectUndo(go, "Create " + childName);
               go.transform.SetParent(parent, false);
               return go;
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
               MaxMediationReflection.ApplySdkSetup(this, CurrentSDKSetup);
          }

          private void UpdateAdmobMediation()
          {
               AdMobMediationReflection.ApplySdkSetup(this, CurrentSDKSetup);
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
