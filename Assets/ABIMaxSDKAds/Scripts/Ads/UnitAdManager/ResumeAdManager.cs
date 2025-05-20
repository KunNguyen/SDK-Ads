using System;
using System.Threading.Tasks;
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
               SetAdManager(AdsType.APP_OPEN);
               IsInited = true;
          }
          protected override void UpdateRemoteConfigValue()
          {
               IsActive = FirebaseManager.Instance.GetConfigBool(Keys.key_remote_ads_resume_ads_active);
               DebugAds.Log("=============== Active Resume Ads " + IsActive);
               
               string adsType = FirebaseManager.Instance.GetConfigString(Keys.key_remote_ads_resume_ads_type);
               switch (adsType)
               {
                    case "INTERSTITIAL":
                    {
                         AdsType = AdsType.INTERSTITIAL;
                         break;
                    }
                    case "APP_OPEN":
                    {
                         AdsType = AdsType.APP_OPEN;
                         break;
                    }
               }

               SetAdManager(AdsType);
               PauseTimeNeedToShowAds = (float)FirebaseManager.Instance.GetConfigDouble(Keys.key_remote_ads_resume_pause_time);
               DebugAds.Log("=============== Pause Time Need To Show Ads " + PauseTimeNeedToShowAds);
               
               AdsResumeCappingTime = (float)FirebaseManager.Instance.GetConfigDouble(Keys.key_remote_ads_resume_capping_time);
               DebugAds.Log("=============== Ads Resume Capping Time " + AdsResumeCappingTime);
               IsReady = true;
          }

          private void SetAdManager(AdsType adsType)
          {
               switch (adsType)
               {
                    case AdsType.INTERSTITIAL:
                    {
                         SelectedAdManager = AdsManager.Instance.InterstitialAdManager;
                         break;
                    }
                    case AdsType.APP_OPEN:
                    {
                         SelectedAdManager = AdsManager.Instance.AppOpenAdManager;
                         break;
                    }
               }
          }

          public override void RequestAd()
          {
          }

          public override void CallToShowAd(string placementName = "", UnityAction closedCallback = null, UnityAction showSuccessCallback = null,
               UnityAction showFailCallback = null, bool isTracking = true, bool isSkipCapping = false)
          {
               base.CallToShowAd(placementName, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
               
               DebugAds.Log("AOA Status: " + IsActive);
               DebugAds.Log("IsRemoveAds: " + IsRemoveAds());
               DebugAds.Log("IsCheatAds: " + IsCheatAds());
               DebugAds.Log("IsLoaded: " + IsLoaded());
               
               if (!IsAdReady()) return;
               if(IsShowingAd) return;
               
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

          public override void OnPause(bool paused)
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
                         DebugAds.Log("Call Resume Ads");
                         if (IsReady)
                         {
                              CallToShowAd();     
                         }
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
               return IsActive && IsReady && !IsRemoveAds() && !IsCheatAds()&& IsLoaded();
          }
     }
}