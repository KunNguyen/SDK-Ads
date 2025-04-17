using System;
using System.Threading.Tasks;
using ABI;
using ABIMaxSDKAds.Scripts;
using UnityEngine;
using UnityEngine.Events;

namespace SDK.AdsManagers.Interface
{
     public class ResumeAdManager : UnitAdManager
     {
          [field: SerializeField] private AdsType AdsType { get; set; }
          private UnitAdManager SelectedAdManager { get; set; }
          [field: SerializeField] private float PauseTimeNeedToShowAds { get; set; } = 5f;
          [field: SerializeField] private float AdsResumeCappingTime { get; set; } = 5f;
          [field: SerializeField] private float DelayTimeToShowAds { get; set; } = 0.3f;

          public DateTime PauseTime { get; private set; }
          public DateTime LastTimeShowAds { get; set; } = DateTime.Now;

          public override void Init(AdsMediationType adsMediationType)
          {
          }
          public override void UpdateRemoteConfig()
          {
               IsActive = ABIFirebaseManager.Instance.GetConfigBool(Keys.key_remote_ads_resume_ads_active);
               DebugAds.Log("=============== Active Resume Ads " + IsActive);
               
               string adsType = ABIFirebaseManager.Instance.GetConfigString(Keys.key_remote_ads_resume_ads_type);
               switch (adsType)
               {
                    case "INTERSTITIAL":
                    {
                         AdsType = AdsType.INTERSTITIAL;
                         SelectedAdManager = AdsManager.Instance.InterstitialAdManager;
                         break;
                    }
                    case "APP_OPEN":
                    {
                         AdsType = AdsType.APP_OPEN;
                         SelectedAdManager = AdsManager.Instance.AppOpenAdManager;
                         break;
                    }
               }
               PauseTimeNeedToShowAds = (float)ABIFirebaseManager.Instance.GetConfigDouble(Keys.key_remote_ads_resume_pause_time);
               DebugAds.Log("=============== Pause Time Need To Show Ads " + PauseTimeNeedToShowAds);
               
               AdsResumeCappingTime = (float)ABIFirebaseManager.Instance.GetConfigDouble(Keys.key_remote_ads_resume_capping_time);
               DebugAds.Log("=============== Ads Resume Capping Time " + AdsResumeCappingTime);
          }

          public override void RequestAd()
          {
          }

          public override void CallToShowAd(string placementName = "", UnityAction closedCallback = null, UnityAction showSuccessCallback = null,
               UnityAction showFailCallback = null, bool isTracking = true, bool isSkipCapping = false)
          {
               base.CallToShowAd(placementName, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
               if (!IsActive || IsRemoveAds() || IsCheatAds()) return;
               if(IsShowingAd) return;
               if (!IsLoaded()) return;
               float timeFromPause = (float)(DateTime.Now - PauseTime).TotalSeconds;
               DebugAds.Log("Time From Pause " + timeFromPause);
               if (timeFromPause < PauseTimeNeedToShowAds) return;
               float timeFromLastShow = (float)(DateTime.Now - LastTimeShowAds).TotalSeconds;
               DebugAds.Log("Time From Last Show " + timeFromLastShow);
               if (timeFromLastShow < AdsResumeCappingTime) return;
               Show();
          }

          public override void Show()
          {
               ShowAsync().ContinueWith(task =>
               {
                    if (task.IsFaulted)
                    {
                         DebugAds.Log("Error showing ad: " + task.Exception);
                    }
                    else
                    {
                         DebugAds.Log("Ad shown successfully");
                    }
               });
          }
          private async Task ShowAsync()
          {
               if (ShowLoadingPanel != null)
               {
                    DebugAds.Log("Showing loading panel");
                    await ShowLoadingPanel;
               }
               
               await Task.Delay(TimeSpan.FromSeconds(DelayTimeToShowAds));
               SelectedAdManager.CallToShowAd("",
                    OnAdClose,
                    OnAdShowSuccess,
                    OnAdShowFailed,
                    true,
                    true);
          }

          public override void OnAdClose()
          {
               base.OnAdClose();
               LastTimeShowAds = DateTime.Now;
               CloseLoadingPanel();
          }

          public void OnPause(bool paused)
          {
               switch (paused)
               {
                    case true:
                    {
                         PauseTime = DateTime.Now;
                         break;
                    }
                    case false:
                    {
                         CallToShowAd();
                         break;
                    }
               }
          }

          public override bool IsLoaded()
          {
               return SelectedAdManager != null && SelectedAdManager.IsLoaded();
          }

          public override bool IsAdReady()
          {
               return IsActive && !IsRemoveAds() && !IsCheatAds()&& IsLoaded();
          }
     }
}