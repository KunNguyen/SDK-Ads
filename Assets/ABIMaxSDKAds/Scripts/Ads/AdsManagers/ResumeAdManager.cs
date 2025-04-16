using ABI;
using ABIMaxSDKAds.Scripts;
using UnityEngine;

namespace SDK.AdsManagers.Interface
{
     public class ResumeAdManager : UnitAdManager
     {
          [field: SerializeField] private AdsType AdsType { get; set; }
          private UnitAdManager SelectedAdManager { get; set; }
          private float PauseTimeNeedToShowAds { get; set; } = 5f;
          private float AdsResumeCappingTime { get; set; } = 5f;

          public override void Init(AdsMediationType adsMediationType)
          {
          }
          public void UpdateRemoteConfig()
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

          public override void Show()
          {
               if (IsRemoveAds() || IsCheatAds()) return;
               if (SelectedAdManager == null) return;
               if (!SelectedAdManager.IsLoaded()) return;
               SelectedAdManager.Show();
          }

          public void OnPause(bool paused)
          {
               
          }

          public override bool IsLoaded()
          {
               return SelectedAdManager.IsLoaded();
          }

          public override bool IsAdReady()
          {
               return IsActive && !IsRemoveAds() && !IsCheatAds()&& IsLoaded();
          }
     }
}