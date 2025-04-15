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
          private const int MaxInterruptCount = 3;
          private bool IsWatchedSuccess { get; set; } = false;
          private UnityAction<bool> AdRewardClosedCallback = null;

          public override void Init(AdsMediationType adsMediationType)
          {
               if(AdsMediationType != adsMediationType) return;
               MediationController.InitRewardVideoAd(
                    OnAdClosed,
                    OnAdLoadSuccess,
                    OnAdLoadFail,
                    OnAdStartShow);
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

          private void OnAdStartShow()
          {
               
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
               return MediationController.IsRewardVideoLoaded();
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