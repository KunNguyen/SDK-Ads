using System;
using ABIMaxSDKAds.Scripts;
using ABIMaxSDKAds.Scripts.Ads.AdsManagers;
using ABIMaxSDKAds.Scripts.Ads.AdsManagers.Service;
using Firebase.RemoteConfig;
using SDK.AdsManagers.Interface;
using UnityEngine;
using UnityEngine.Events;

namespace SDK.AdsManagers
{
     public class InterstitialAdManager : UnitAdManager, IAutoLoad, ICooldown
     {
          [field: SerializeField] public float MaxCappingAdsCooldown { get; set; }
          [field: SerializeField] public int LevelToShowInterstitial { get; set; } = 0;
          [field: SerializeField] public bool IsActiveCooldownFromStart { get; set; } = true;
          private DateTime StartRequestTime{ get; set; }
          [field: SerializeField] public AutoLoadSystem AutoLoad { get; set; }
          [field: SerializeField] public CooldownSystem CooldownSystem { get; set; }


          public override void Init(AdsMediationType adsMediationType)
          {
               if (AdsMediationType != adsMediationType) return;
               if (!IsActive || IsRemoveAds() || IsCheatAds()) return;
               DebugAds.Log("Init Interstitial Start");
               foreach (AdsMediationController t in AdsConfig.adsMediations)
               {
                    t.InitInterstitialAd(
                         OnAdClose,
                         OnAdLoadSuccess,
                         OnAdLoadFail,
                         OnAdShowSuccess,
                         OnAdShowFailed
                    );
               }
               AutoLoad = new AutoLoadSystem();
               AutoLoad.Init(this, RequestAd);
               AutoLoad.IsActiveLogging = true;
               
               CooldownSystem = new CooldownSystem();
               CooldownSystem.Init(this, MaxCappingAdsCooldown,false);
               IsInited = true;
          }

          public override void OnRemoveAd()
          {
               base.OnRemoveAd();
               IsActive = false;
               AutoLoad.StopAutoLoad();
               StopAdCooldown();
          }
          protected override void UpdateRemoteConfigValue()
          {
               {
                    ConfigValue configValue =
                         FirebaseManager.Instance.GetConfigValue(Keys.key_remote_interstitial_capping_time);
                    MaxCappingAdsCooldown = (float)configValue.DoubleValue;
                    CooldownSystem.SetMaxCooldown(MaxCappingAdsCooldown);
                    DebugAds.Log("=============== Max Interstitial Capping Time " + MaxCappingAdsCooldown);
               }
               {
                    LevelToShowInterstitial =
                         (int)FirebaseManager.Instance.GetConfigDouble(Keys.key_remote_interstitial_level);
                    DebugAds.Log("=============== Level Pass Show Interstitial " + LevelToShowInterstitial);
               }
               IsReady = true;
               StartAutoLoad();
               StartCooldown();
          }

          private void StartAutoLoad()
          {
               if (AutoLoad == null)
               {
                    AutoLoad = new AutoLoadSystem();
                    AutoLoad.Init(this, RequestAd);
               }
               AutoLoad.StartAutoLoad();
          }

          private void StartCooldown()
          {
               CooldownSystem.StartCooldown();
          }
          private void StopAdCooldown()
          {
               CooldownSystem.StopCooldown();
          }
          public void ResetCooldown()
          {
               DebugAds.Log("Restarting Interstitial Ad Cooldown");
               CooldownSystem.ResetCooldown();
          }

          public override void RequestAd()
          {
               if(MediationController.IsInterstitialLoaded())return;
               if (!IsReady) return;
               StartRequestTime = DateTime.Now;
               MediationController.RequestInterstitialAd();
          }

          public override void CallToShowAd(
               string placementName = "",
               UnityAction closedCallback = null,
               UnityAction showSuccessCallback = null,
               UnityAction showFailCallback = null,
               bool isTracking = true, bool isSkipCapping = false)
          {
               base.CallToShowAd(placementName, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
               DebugAds.Log("IsActive: " + IsActive + " IsReady: " + IsReady);
               if(!IsActive || !IsReady) return; 
               if (HandleCheatAdsIfNeeded(showSuccessCallback)) return;
               if (HandleCooldownIfNeeded(isSkipCapping, showSuccessCallback, closedCallback)) return;

               if (!AdsHelper.IsRemoveAds(this))
               {
                    HandleAdTracking(isTracking);
                    HandleAdDisplay(showFailCallback, closedCallback, showSuccessCallback);
               }
               else
               {
                    HandleRemoveAds(closedCallback, showSuccessCallback);
               }
          }

          #region Handle

          private bool HandleCheatAdsIfNeeded(UnityAction showSuccessCallback)
          {
               DebugAds.Log("IsCheatAds: " + AdsHelper.IsCheatAds(this));
               if (AdsHelper.IsCheatAds(this))
               {
                    HandleCheatAds(showSuccessCallback);
                    return true;
               }
               return false;
          }

          private bool HandleCooldownIfNeeded(bool isSkipCapping, UnityAction showSuccessCallback, UnityAction closedCallback)
          {
               DebugAds.Log("HandleCooldownIfNeeded: " + isSkipCapping + " Cooldown " + CooldownSystem.IsCooldownFinished + " Time" + CooldownSystem.CooldownTime);
               if (!isSkipCapping && !CooldownSystem.IsCooldownFinished)
               {
                    HandleCooldownNotFinished(showSuccessCallback, closedCallback);
                    return true;
               }
               return false;
          }

          private void HandleAdTracking(bool isTracking)
          {
               if (isTracking)
               {
                    AdsTracker.Instance.TrackAdsInterstitial_ClickOnButton();
               }
          }

          private void HandleAdDisplay(UnityAction showFailCallback, UnityAction closedCallback, UnityAction showSuccessCallback)
          {
               DebugAds.Log("HandleAdDisplay: " + IsLoaded());
               if (AdsHelper.IsAdLoaded(this))
               {
                    AdShowFailCallback = showFailCallback;
                    Show();
               }
               else
               {
                    AdsTracker.Instance.TrackAdsInterstitial_ShowFailByLoad();
                    showFailCallback?.Invoke();
               }
          }
          
          private void HandleCheatAds(UnityAction showSuccessCallback)
          {
               OnAdShowSuccess();
               showSuccessCallback?.Invoke();
          }

          private void HandleCooldownNotFinished(UnityAction showSuccessCallback, UnityAction closedCallback)
          {
               DebugAds.Log("Ad skipped due to cooldown. " +
                            "Cooldown time: " + CooldownSystem.CooldownTime +
                            " seconds");
               showSuccessCallback?.Invoke();
               closedCallback?.Invoke();
          }

          private void HandleRemoveAds(UnityAction closedCallback, UnityAction showSuccessCallback)
          {
               DebugAds.Log("Ads removed. Executing callbacks.");
               closedCallback?.Invoke();
               showSuccessCallback?.Invoke();
          }

          #endregion

          public override void Show()
          {
               MarkShowingAds(true);
               MediationController.ShowInterstitialAd();
          }
          public override void OnAdShowSuccess()
          {
               DebugAds.Log("OnAdShowSuccess Interstitial");
               base.OnAdShowSuccess();
               AdsTracker.Instance.TrackAdsInterstitial_ShowSuccess();
          }
          public override void OnAdShowFailed()
          {
               DebugAds.Log("OnAdShowFailed Interstitial");
               base.OnAdShowFailed();
               ResetCooldown();
               AdsTracker.Instance.TrackAdsInterstitial_ShowFail();
          }
          public override void OnAdClose()
          {
               DebugAds.Log("OnAdClose Interstitial");
               base.OnAdClose();
               AutoLoad.OnAdClosed();
               ResetCooldown();
          }
          public override void OnAdLoadSuccess()
          {
               DebugAds.Log("OnAdLoadSuccess Interstitial");
               base.OnAdLoadSuccess();
               AutoLoad.OnLoadSuccess();
               float timeFromStartRequest = (float)(DateTime.Now - StartRequestTime).TotalSeconds;
               AdsTracker.Instance.TrackAdsInterstitial_LoadedSuccess(timeFromStartRequest);
          }

          public override void OnAdLoadFail()
          {
               DebugAds.Log("OnAdLoadFail Interstitial");
               base.OnAdLoadFail();
               AutoLoad.OnLoadFailed();
          }
          public override bool IsLoaded()
          {
               return MediationController.IsInterstitialLoaded();
          }
          public override bool IsAdReady()
          {
               return IsActive && IsReady && CooldownSystem.IsCooldownFinished && IsLoaded();
          }
     }
}