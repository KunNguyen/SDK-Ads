using JisSDKAds.Ads.UnitAdManagers;
using UnityEngine;
using UnityEngine.Events;

namespace JisSDKAds.Ads
{
     public partial class AdsManager
     {
          private AdsConfig RewardVideoAdsConfig => GetAdsConfig(AdsType.REWARDED);
          [field: SerializeField] public RewardAdManager RewardAdManager { get; set; }

          private string m_RewardedPlacement;
          private void SetupRewardVideo()
          {
               RewardAdManager.Setup(RewardVideoAdsConfig, CurrentSDKSetup, GetSelectedMediation(AdsType.REWARDED));
               onRemoveAdsEvent.AddListener(RewardAdManager.OnRemoveAd);
               RewardAdManager.IsRemoveAds = () => IsRemoveAds;
               RewardAdManager.IsCheatAds = () => isCheatAds;
               RewardAdManager.MarkShowingAds = MarkShowingAds;
               RewardAdManager.IsShowingAdChecking = IsShowingAds;
          }
          public void ShowRewardVideo(string rewardedPlacement, UnityAction successCallback,
               UnityAction<bool> closedCallback = null, UnityAction failedCallback = null)
          {
               if (TryShowRewardVideoViaJisAds(rewardedPlacement, successCallback, closedCallback, failedCallback))
               {
                    InterstitialAdManager?.ResetCooldown();
                    return;
               }

               RewardAdManager.CallToShowRewardAd(rewardedPlacement, (isCompleted) =>
               {
                    closedCallback?.Invoke(isCompleted);
                    InterstitialAdManager.ResetCooldown();
               }, successCallback, failedCallback);
          }
          public bool IsRewardedVideoLoaded()
          {
               if (TryQueryRewardedLoadedFromJisAds(out var loaded))
                    return loaded;
               return RewardAdManager.IsLoaded();
          }
          public bool CanShowRewardedVideo()
          {
               if (TryQueryRewardedLoadedFromJisAds(out var loaded))
                    return loaded;
               return RewardAdManager.IsAdReady();
          }
     }
}