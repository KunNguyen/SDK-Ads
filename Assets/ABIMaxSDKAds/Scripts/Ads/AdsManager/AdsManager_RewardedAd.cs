using SDK.AdsManagers;
using UnityEngine;
using UnityEngine.Events;

namespace SDK
{
     public partial class AdsManager
     {
          private AdsConfig RewardVideoAdsConfig => GetAdsConfig(AdsType.REWARDED);
          [field: SerializeField] public RewardAdManager RewardAdManager { get; set; }

          private string m_RewardedPlacement;
          private void SetupRewardVideo()
          {
               RewardAdManager.Setup(RewardVideoAdsConfig, SDKSetup, GetSelectedMediation(AdsType.REWARDED));
               OnRemoveAdsEvent.AddListener(RewardAdManager.OnRemoveAd);
               RewardAdManager.IsRemoveAds = () => IsRemoveAds;
               RewardAdManager.IsCheatAds = () => IsCheatAds;
               RewardAdManager.MarkShowingAds = MarkShowingAds;
               RewardAdManager.IsShowingAdChecking = IsShowingAds;
          }
          private void InitRewardedVideo(AdsMediationType adsMediationType)
          {
               RewardAdManager.Init(adsMediationType);
          }
          public void ShowRewardVideo(string rewardedPlacement, UnityAction successCallback,
               UnityAction<bool> closedCallback = null, UnityAction failedCallback = null)
          {
               RewardAdManager.CallToShowRewardAd(rewardedPlacement, (isCompleted) =>
               {
                    closedCallback?.Invoke(isCompleted);
                    InterstitialAdManager.ResetCooldown();
               }, successCallback, failedCallback);
          }
          public bool IsRewardedVideoLoaded()
          {
               return RewardAdManager.IsLoaded();
          }
          public bool CanShowRewardedVideo()
          {
               return RewardAdManager.IsAdReady();
          }
     }
}