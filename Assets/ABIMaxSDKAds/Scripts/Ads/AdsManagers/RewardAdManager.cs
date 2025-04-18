using System;
using System.Threading.Tasks;
using ABI;
using ABIMaxSDKAds.Scripts;
using Firebase.RemoteConfig;
using UnityEngine;
using UnityEngine.Events;

namespace SDK.AdsManagers
{
     public class RewardAdManager : UnitAdManager
     {
          
          [field: SerializeField] public bool IsLinkRewardWithRemoveAds { get; set; } = true;
          [field: SerializeField] public bool IsActiveInterruptReward { get; set; } = false;

          [field: SerializeField] public UnitAdManager InterruptAdManager { get; set; }
          private int InterruptCount { get; set; } = 0;
          private int MaxInterruptCount = 3;
          private bool IsWatchedSuccess { get; set; } = false;
          private UnityAction<bool> AdRewardClosedCallback = null;
          private float ReloadTime { get; set; } = 2f;
          private int RetryAttempt { get; set; } = 0;
          private bool IsLoading = false;

          public override void Init(AdsMediationType adsMediationType)
          {
               DebugAds.Log("Start Init Reward Ad");
               if(AdsMediationType != adsMediationType) return;
               if (!IsActive || IsRemoveAds() || IsCheatAds()) return;
               foreach (AdsMediationController t in AdsConfig.adsMediations)
               {
                    t.InitRewardVideoAd(
                         OnAdClosed,
                         OnAdLoadSuccess,
                         OnAdLoadFail,
                         OnAdStartShow);
               }
               // Start Auto Reload Reward Ad
               _ = AutoReloadRewardAd();
               DebugAds.Log("Init Reward Ad Done");
          }
          
          private async Task AutoReloadRewardAd() 
          {
               while (true)
               {
                    DebugAds.Log("Auto Reloading Reward Ad " + IsLoaded() + " " + IsLoading + " " + IsRemoveAds() + " " + IsCheatAds() + " " + IsShowingAd);
                    if (!IsLoaded() && !IsLoading && !IsRemoveAds() && !IsCheatAds() && !IsShowingAd)
                    {
                         IsLoading = true;
                         RequestAd();
                    }
                    await Task.Delay(TimeSpan.FromSeconds(10f));
               }
          }

          public override void UpdateRemoteConfig()
          {
               base.UpdateRemoteConfig();
               {
                    ConfigValue configValue =
                         ABIFirebaseManager.Instance.GetConfigValue(Keys.key_remote_inter_reward_interspersed);
                    IsActiveInterruptReward = configValue.BooleanValue;
                    Debug.Log("=============== Active " + IsActiveInterruptReward);
               }
               {
                    ConfigValue configValue =
                         ABIFirebaseManager.Instance.GetConfigValue(Keys.key_remote_inter_reward_interspersed_time);
                    MaxInterruptCount = (int)configValue.DoubleValue;
                    Debug.Log("=============== MAX Reward InterruptCount" + MaxInterruptCount);
               }
          }

          public override void RequestAd()
          {
               if (MediationController.IsRewardVideoLoaded()) return;
               MediationController.RequestRewardVideoAd();
          }

          public void CallToShowRewardAd(string placementName = "", UnityAction<bool> closedCallback = null,
               UnityAction showSuccessCallback = null,
               UnityAction showFailCallback = null, bool isTracking = true, bool isSkipCapping = false)
          {     
               IsWatchedSuccess = false;
               AdRewardClosedCallback = closedCallback;
               CallToShowAd(placementName, null, showSuccessCallback, showFailCallback, isTracking,
                    isSkipCapping);
          }
          public override void CallToShowAd(string placementName = "", UnityAction closedCallback = null, UnityAction showSuccessCallback = null,
               UnityAction showFailCallback = null, bool isTracking = true, bool isSkipCapping = false)
          {
               base.CallToShowAd(placementName, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
               if (IsCheatAds())
               {
                    OnAdShowSuccess();
                    return;
               }
               
               ABIAnalyticsManager.Instance.TrackAdsReward_ClickOnButton();
               if (IsRemoveAds() && IsLinkRewardWithRemoveAds)
               {
                    OnRewardVideoEarnSuccess();
               }
               else
               {
                    if (IsReadyToShowRewardInterrupt() && IsInterruptAdLoaded())
                    {
                         ShowInterruptAd(() =>
                         {
                              OnAdShowSuccess();
                              ResetRewardInterruptCount();
                         }, OnAdShowFailed);
                    }
                    else
                    {
                         if (IsLoaded())
                         {
                              Show();
                         }
                    }
               }
          }
          public override void OnAdLoadSuccess()
          {
               base.OnAdLoadSuccess();
               DebugAds.Log("Load Reward Ad Success");
               IsLoading = false;
          }
          
          public override void OnAdLoadFail()
          {
               base.OnAdLoadFail();
               DebugAds.Log("Load Reward Ad Failed");
               IsLoading = false;
          }

          private void OnAdStartShow()
          {
               IsShowingAd = true;
          }
          private void OnRewardVideoEarnSuccess()
          {
               MarkShowingAds(false);
               AdShowSuccessCallback?.Invoke();
               InterruptCount++;
               ABIAnalyticsManager.Instance.TrackAdsReward_ShowCompleted(Placement);
          }

          private void OnAdClosed(bool isWatched)
          {
               IsShowingAd = false;
               AdRewardClosedCallback?.Invoke(isWatched);
          }

          public override void Show()
          {
               MarkShowingAds(true);
               MediationController.ShowRewardVideoAd(AdShowSuccessCallback, AdShowFailCallback);
          }
          private void ShowInterruptAd(UnityAction onSuccessCallback = null, UnityAction onFailCallback = null)
          {
               InterruptAdManager.CallToShowAd(Placement, onSuccessCallback, onFailCallback);
          }
          private void ResetRewardInterruptCount()
          {
               InterruptCount = 0;
          }
          private bool IsInterruptAdLoaded()
          {
               return InterruptAdManager.IsLoaded();
          }
          public override bool IsLoaded()
          {
               return MediationController!= null && MediationController.IsRewardVideoLoaded();
          }
          public override bool IsAdReady()
          {
               return (IsLinkRewardWithRemoveAds && IsRemoveAds()) || IsLoaded();
          }
          private bool IsReadyToShowRewardInterrupt()
          {
               if (IsActiveInterruptReward) return false;
               return InterruptCount>= MaxInterruptCount;
          }
     }
}