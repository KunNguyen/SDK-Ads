using ABIMaxSDKAds.Scripts;
using ABIMaxSDKAds.Scripts.Ads.AdsManagers.Service;
using SDK.AdsManagers.Interface;
using UnityEngine;
using UnityEngine.Events;

namespace SDK.AdsManagers
{
     public class RewardAdManager : UnitAdManager, IAutoLoad
     {
          [field: SerializeField] public bool IsLinkRewardWithRemoveAds { get; set; } = true;
          [field: SerializeField] public bool IsActiveInterruptReward { get; set; }
          [field: SerializeField] public UnitAdManager InterruptAdManager { get; set; }
          [field: SerializeField] public AutoLoadSystem AutoLoad { get; set; }

          private int InterruptCount { get; set; }
          private int MaxInterruptCount { get; set; } = 3;
          private bool IsWatchedSuccess { get; set; }
          private UnityAction<bool> AdRewardClosedCallback { get; set; }

          public override void Init(AdsMediationType adsMediationType)
          {
               DebugAds.Log("Init Reward Ad Start");
               if (AdsMediationType != adsMediationType || !IsActive || IsRemoveAds() || IsCheatAds()) return;

               foreach (var t in AdsConfig.adsMediations)
               {
                    t.InitRewardVideoAd( OnRewardVideoEarnSuccess, OnAdClosed, OnAdLoadSuccess, OnAdLoadFail, OnAdStartShow);
               }
               AutoLoad = new AutoLoadSystem();
               AutoLoad.Init(this, RequestAd);
               IsInited = true;
          }

          public override void OnRemoveAd()
          {
               base.OnRemoveAd();
               if (!IsLinkRewardWithRemoveAds) return;

               IsActive = false;
               AutoLoad.OnRemoveAds();
               InterruptCount = 0;
               AutoLoad.StopAutoLoad();
          }

          protected override void UpdateRemoteConfigValue()
          {
               base.UpdateRemoteConfigValue();

               IsActiveInterruptReward = FirebaseManager.Instance
                    .GetConfigValue(Keys.key_remote_inter_reward_interspersed).BooleanValue;
               DebugAds.Log($"=============== Active Reward Interrupt {IsActiveInterruptReward}");

               MaxInterruptCount = (int)FirebaseManager.Instance
                    .GetConfigValue(Keys.key_remote_inter_reward_interspersed_time).DoubleValue;
               DebugAds.Log($"=============== MAX Reward Interrupt Count {MaxInterruptCount}");
               IsReady = true;
               StartAutoLoad();
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

          public override void RequestAd()
          {
               DebugAds.Log("Start Requesting Reward Ad");
               if (!MediationController.IsRewardVideoLoaded())
               {
                    DebugAds.Log($"Requesting Reward Video");
                    MediationController.RequestRewardVideoAd();
               }
          }

          public void CallToShowRewardAd(string placementName = "", UnityAction<bool> closedCallback = null,
               UnityAction showSuccessCallback = null, UnityAction showFailCallback = null, 
               bool isTracking = true, bool isSkipCapping = false)
          {
               IsWatchedSuccess = false;
               AdRewardClosedCallback = closedCallback;
               CallToShowAd(placementName, null, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
          }

          public override void CallToShowAd(string placementName = "", UnityAction closedCallback = null, 
               UnityAction showSuccessCallback = null, UnityAction showFailCallback = null, 
               bool isTracking = true, bool isSkipCapping = false)
          {
               base.CallToShowAd(placementName, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
               if(!IsReady)
               {
                    DebugAds.Log("Reward Ad is not ready");
                    closedCallback?.Invoke();
                    return;
               }
               if (IsCheatAds())
               {
                    OnAdShowSuccess();
                    return;
               }

               AdsTracker.Instance.TrackAdsReward_ClickOnButton();

               if (IsRemoveAds() && IsLinkRewardWithRemoveAds)
               {
                    OnRewardVideoEarnSuccess();
               }
               else if (IsReadyToShowRewardInterrupt() && IsInterruptAdLoaded())
               {
                    ShowInterruptAd(() =>
                    {
                         OnAdShowSuccess();
                         ResetRewardInterruptCount();
                    }, OnAdShowFailed);
               }
               else if (IsLoaded())
               {
                    Show();
               }
          }

          public override void OnAdLoadSuccess()
          {
               base.OnAdLoadSuccess();
               DebugAds.Log("On RewardAdManager Load Success");
               AutoLoad.OnLoadSuccess();
          }

          public override void OnAdLoadFail()
          {
               base.OnAdLoadFail();
               DebugAds.Log("On RewardAdManager Load Failed");
               AutoLoad.OnLoadFailed();
          }

          private void OnAdStartShow()
          {
               IsShowingAd = true;
          }

          private void OnRewardVideoEarnSuccess()
          {
               DebugAds.Log("On RewardAdManager Earn Success");
               AdShowSuccessCallback?.Invoke();
               InterruptCount++;
               AdsTracker.Instance.TrackAdsReward_ShowCompleted(Placement);
          }

          private void OnAdClosed(bool isWatched)
          {
               DebugAds.Log($"On RewardAdManager Closed {isWatched}");
               IsShowingAd = false;
               MarkShowingAds(false);
               AutoLoad.OnAdClosed();
               AdRewardClosedCallback?.Invoke(isWatched);
          }

          public override void Show()
          {
               MarkShowingAds(true);
               MediationController.ShowRewardVideoAd();
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
               return MediationController != null && MediationController.IsRewardVideoLoaded();
          }

          public override bool IsAdReady()
          {
               if (IsActive && IsReady)
               {
                    return (IsLinkRewardWithRemoveAds && IsRemoveAds()) || IsLoaded();     
               }
               return false;
          }

          private bool IsReadyToShowRewardInterrupt()
          {
               return !IsActiveInterruptReward && InterruptCount >= MaxInterruptCount;
          }
     }
}
