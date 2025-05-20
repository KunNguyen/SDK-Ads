using System;
using System.Collections;
using ABIMaxSDKAds.Scripts;
using Firebase.RemoteConfig;
using UnityEngine;
using UnityEngine.Events;

namespace SDK.AdsManagers
{
     public class AppOpenAdManager : UnitAdManager
     {
          [field: SerializeField] public bool IsActiveByRemoteConfig { get; set; } = true;
          [field: SerializeField] public bool IsFirstOpen { get; set; } = true;
          [field: SerializeField] public bool IsActiveShowAdsFirstTime { get; set; } = true;
          public override void Init(AdsMediationType adsMediationType)
          {
               if (AdsMediationType != adsMediationType) return;
               if (IsRemoveAds() || IsCheatAds()) return;
               MediationController.InitAppOpenAds(
                    OnAdLoadSuccess, 
                    OnAdLoadFail, 
                    OnAdClose, 
                    OnAdShowSuccess,
                    OnAdShowFailed);
               StartCoroutine(coCheckingShowAppOpenAdsOnStart());
               IsInited = true;
          }
          IEnumerator coCheckingShowAppOpenAdsOnStart()
          {
               if (!IsActive || IsCheatAds() || IsRemoveAds()) yield break;
               
               float startCheckingTime = Time.realtimeSinceStartup;
               while (Time.realtimeSinceStartup < 10f)
               {
                    if (IsReady)
                    {
                         if(IsFirstOpen && !IsActiveShowAdsFirstTime) break;
                         if (IsLoaded())
                         {
                              ShowAdsFirstTime();
                              break;  
                         }
                    }
                    yield return new WaitForSeconds(0.2f);
               }

               DebugAds.Log("AOA Done Checking --- Start Time = " + startCheckingTime + " End Time = " +
                         Time.realtimeSinceStartup);
          }
          protected override void UpdateRemoteConfigValue()
          {
               {
                    ConfigValue configValue =
                         FirebaseManager.Instance.GetConfigValue(Keys.key_remote_aoa_active);
                    IsActiveByRemoteConfig = configValue.BooleanValue;
                    DebugAds.Log("App Open Ads Active = " + IsActiveByRemoteConfig);
               }

               {
                    ConfigValue configValue =
                         FirebaseManager.Instance.GetConfigValue(Keys.key_remote_aoa_show_first_time_active);
                    IsActiveShowAdsFirstTime = configValue.BooleanValue;
                    DebugAds.Log("AOA active show first time = " + IsActiveShowAdsFirstTime);
               }
               IsReady = true;
          }
          public void ShowAdsFirstTime()
          {
               if (IsCheatAds() || IsRemoveAds()) return;
               MediationController.ShowAppOpenAds();
          }

          public override void CallToShowAd(string placementName = "", UnityAction closedCallback = null, UnityAction showSuccessCallback = null,
               UnityAction showFailCallback = null, bool isTracking = true, bool isSkipCapping = false)
          {
               base.CallToShowAd(placementName, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
               if(IsCheatAds() || IsRemoveAds()) return;
               Show();
          }

          public override void RequestAd()
          {
               if(IsRemoveAds() || IsCheatAds()) return;
               MediationController.RequestAppOpenAds();
          }

          public override void Show()
          {
               if (IsLoaded())
               {
                    MarkShowingAds(true);
                    Debug.Log("Start Force Show App Open Ads");
                    MediationController.ShowAppOpenAds();
               }
          }

          public override void OnAdClose()
          {
               base.OnAdClose();
               RequestAd();
          }

          public override void OnAdShowSuccess()
          {
               base.OnAdShowSuccess();
          }

          public override bool IsLoaded()
          {
               return MediationController != null && MediationController.IsAppOpenAdsLoaded();
          }

          public override bool IsAdReady()
          {
               return IsActive && !IsRemoveAds() && !IsCheatAds() && IsLoaded();
          }
     }
}