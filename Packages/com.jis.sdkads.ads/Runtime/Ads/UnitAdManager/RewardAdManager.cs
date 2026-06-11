using JisSDKAds.Ads.UnitAdManagers.Interface;
using JisSDKAds.Ads.UnitAdManagers.Service;
using JisSDKAds.Common;
using JisSDKAds.Firebase;
using UnityEngine;
using UnityEngine.Events;
using JisAdsEntry = JisSDKAds.Ads.JisAds;

namespace JisSDKAds.Ads.UnitAdManagers
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

          public override void Init()
          {
               DebugAds.Log("Init Reward Ad Start");
               if (!IsActive || IsRemoveAdsActive() || IsCheatAdsActive()) return;

               foreach (var t in AdsConfig.adsMediations)
               {
                    t.InitRewardVideoAd( OnRewardVideoEarnSuccess, OnAdClosed, OnAdLoadSuccess, OnAdLoadFail, OnAdStartShow);
               }
               AutoLoad = new AutoLoadSystem();
               AutoLoad.Init(this, RequestAd);
               Status = AdStatus.Inited;
          }

          public override void OnRemoveAd()
          {
               base.OnRemoveAd();
               if (!IsLinkRewardWithRemoveAds) return;

               IsActive = false;
               AutoLoad?.OnRemoveAds();
               InterruptCount = 0;
               AutoLoad?.StopAutoLoad();
          }

          protected override void UpdateRemoteConfigValue()
          {
               base.UpdateRemoteConfigValue();

               if (FirebaseManager.Instance == null)
               {
                    DebugAds.LogWarning("[RewardAdManager] FirebaseManager.Instance is null. Using default remote config values.");
               IsReady = true;
               if (!AdsManager.UsesJisAdsCoreForStandardLoads())
                    StartAutoLoad();
               else
                    DebugAds.Log("[RewardAdManager] AutoLoad skipped — JisAds Core owns preload.");
               return;
          }

               IsActiveInterruptReward = FirebaseManager.Instance
                    .GetConfigValue(Keys.key_remote_inter_reward_interspersed).BooleanValue;
               DebugAds.Log($"=============== Active Reward Interrupt {IsActiveInterruptReward}");

               MaxInterruptCount = (int)FirebaseManager.Instance
                    .GetConfigValue(Keys.key_remote_inter_reward_interspersed_time).DoubleValue;
               DebugAds.Log($"=============== MAX Reward Interrupt Count {MaxInterruptCount}");
               IsReady = true;
               if (!AdsManager.UsesJisAdsCoreForStandardLoads())
                    StartAutoLoad();
               else
                    DebugAds.Log("[RewardAdManager] AutoLoad skipped — JisAds Core owns preload.");
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
               if (AdsManager.UsesJisAdsCoreForStandardLoads())
               {
                    JisAdsEntry.Instance?.RequestRewardedLoadIfNeeded();
                    return;
               }

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
               if (TryShowViaJisAdsCore(placementName, closedCallback, showSuccessCallback, showFailCallback))
                    return;
               CallToShowAd(placementName, null, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
          }

          static bool UsesJisAdsCore() =>
               JisAdsEntry.Instance != null && JisAdsEntry.Instance.UseCoreForStandardFormats;

          bool TryShowViaJisAdsCore(
               string placementName,
               UnityAction<bool> closedCallback,
               UnityAction showSuccessCallback,
               UnityAction showFailCallback)
          {
               if (!UsesJisAdsCore())
                    return false;

               var jis = JisAdsEntry.Instance;
               if (!jis.IsRewardedVideoLoaded())
               {
                    DebugAds.Log("Reward Ad is not ready");
                    showFailCallback?.Invoke();
                    return true;
               }

               jis.ShowRewardVideo(placementName, showSuccessCallback, closedCallback, showFailCallback);
               return true;
          }

          public override void CallToShowAd(string placementName = "", UnityAction closedCallback = null, 
               UnityAction showSuccessCallback = null, UnityAction showFailCallback = null, 
               bool isTracking = true, bool isSkipCapping = false)
          {
               base.CallToShowAd(placementName, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
               if (!IsReady && !UsesJisAdsCore())
               {
                    DebugAds.Log("Reward Ad is not ready");
                    closedCallback?.Invoke();
                    return;
               }
               if (!IsReady && UsesJisAdsCore())
               {
                    DebugAds.Log("Reward Ad is not ready");
                    showFailCallback?.Invoke();
                    return;
               }
               if (IsCheatAdsActive())
               {
                    OnAdShowSuccess();
                    return;
               }

               if (AdsTracker.Instance != null)
                    AdsTracker.Instance.TrackAdsReward_ClickOnButton();
               else
                    DebugAds.LogWarning("[RewardAdManager] AdsTracker.Instance is null. Skipping reward click tracking.");

               if (IsRemoveAdsActive() && IsLinkRewardWithRemoveAds)
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
               else
               {
                    if (AdsTracker.Instance != null)
                         AdsTracker.Instance.TrackAdsReward_ShowFailByLoad();
                    else
                         DebugAds.LogWarning("[RewardAdManager] AdsTracker.Instance is null. Skipping reward show-fail-by-load tracking.");
                    showFailCallback?.Invoke();
               }
          }

          public override void OnAdLoadSuccess()
          {
               base.OnAdLoadSuccess();
               DebugAds.Log("On RewardAdManager Load Success");
               AutoLoad?.OnLoadSuccess();
          }

          public override void OnAdLoadFail()
          {
               base.OnAdLoadFail();
               DebugAds.Log("On RewardAdManager Load Failed");
               AutoLoad?.OnLoadFailed();
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
               if (AdsTracker.Instance != null)
                    AdsTracker.Instance.TrackAdsReward_ShowCompleted(Placement);
          }

          private void OnAdClosed(bool isWatched)
          {
               DebugAds.Log($"On RewardAdManager Closed {isWatched}");
               IsShowingAd = false;
               MarkShowingAds(false);
               AutoLoad?.OnAdClosed();
               AdRewardClosedCallback?.Invoke(isWatched);
          }

          public override void Show()
          {
               MarkShowingAds(true);
               MediationController.ShowRewardVideoAd();
          }

          private void ShowInterruptAd(UnityAction onSuccessCallback = null, UnityAction onFailCallback = null)
          {
               if (InterruptAdManager == null)
               {
                    DebugAds.LogWarning("[RewardAdManager] InterruptAdManager is null. Skipping interrupt flow.");
                    onFailCallback?.Invoke();
                    return;
               }
               InterruptAdManager.CallToShowAd(Placement, onSuccessCallback, onFailCallback);
          }

          private void ResetRewardInterruptCount()
          {
               InterruptCount = 0;
          }

          private bool IsInterruptAdLoaded()
          {
               return InterruptAdManager != null && InterruptAdManager.IsLoaded();
          }

          public override bool IsLoaded()
          {
               if (UsesJisAdsCore())
                    return JisAdsEntry.Instance.IsRewardedVideoLoaded();
               return MediationController != null && MediationController.IsRewardVideoLoaded();
          }

          public override bool IsAdReady()
          {
               if (!IsActive)
                    return false;

               if (UsesJisAdsCore())
               {
                    return (IsLinkRewardWithRemoveAds && IsRemoveAdsActive())
                           || JisAdsEntry.Instance.IsRewardedVideoLoaded();
               }

               if (IsReady)
                    return (IsLinkRewardWithRemoveAds && IsRemoveAdsActive()) || IsLoaded();
               return false;
          }

          private bool IsReadyToShowRewardInterrupt()
          {
               return IsActiveInterruptReward
                      && InterruptAdManager != null
                      && InterruptCount >= MaxInterruptCount;
          }
     }
}
