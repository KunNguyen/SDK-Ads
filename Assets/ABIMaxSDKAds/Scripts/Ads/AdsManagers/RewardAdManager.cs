using UnityEngine;
using UnityEngine.Events;

namespace SDK.AdsManagers
{
     public class RewardAdManager : UnitAdManager
     {
          
          [field: SerializeField] public bool IsLinkRewardWithRemoveAds { get; set; } = true;
          [field: SerializeField] public bool IsActiveInterruptReward { get; set; } = false;

          [field: SerializeField] public UnitAdManager InterruptAdManager { get; set; }
          private int rewardInterruptCount = 0;
          private const int maxRewardInterruptCount = 3;
          

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
                         ShowInterruptAd(OnAdShowSuccess, OnAdShowFail);
                    }
                    else
                    {
                         if (IsAdLoaded())
                         {
                              
                              ShowAd();
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
               AdCloseCallback?.Invoke();
               ABIAnalyticsManager.Instance.TrackAdsReward_ShowCompleted(Placement);
          }

          private void OnAdClosed(bool isWatched)
          {
               
          }

          public override void ShowAd()
          {
               MarkShowingAds(true);
               MediationController.ShowRewardVideoAd(AdShowSuccessCallback, AdShowFailCallback);
          }
          private void ShowInterruptAd(UnityAction onSuccessCallback = null, UnityAction onFailCallback = null)
          {
               InterruptAdManager.CallToShowAd(Placement, onSuccessCallback, onFailCallback);
          }
          private bool IsInterruptAdLoaded()
          {
               return InterruptAdManager.IsAdLoaded();
          }
          public override bool IsAdLoaded()
          {
               return MediationController.IsRewardVideoLoaded();
          }
          private bool IsReadyToShowRewardInterrupt()
          {
               if (IsActiveInterruptReward) return false;
               return rewardInterruptCount>= maxRewardInterruptCount;
          }
     }
}