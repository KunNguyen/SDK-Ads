using ABIMaxSDKAds.Scripts;
using SDK.AdsManagers;
using UnityEngine;
using UnityEngine.Events;

namespace SDK
{
     public partial class AdsManager
     {
          [field: SerializeField] private int LevelPassToShowInterstitial { get; set; } = 2;
          [field: SerializeField] public InterstitialAdManager InterstitialAdManager { get; set; }
          private AdsConfig InterstitialAdsConfig => GetAdsConfig(AdsType.INTERSTITIAL);

          private void SetupInterstitialAds()
          {
               InterstitialAdManager.Setup(
                    InterstitialAdsConfig,
                    SDKSetup,
                    GetSelectedMediation(AdsType.INTERSTITIAL));
               OnRemoveAdsEvent.AddListener(InterstitialAdManager.OnRemoveAd);
               InterstitialAdManager.IsRemoveAds = () => IsRemoveAds;
               InterstitialAdManager.IsCheatAds = () => IsCheatAds;
               InterstitialAdManager.MarkShowingAds = MarkShowingAds;
               InterstitialAdManager.IsShowingAdChecking = IsShowingAds;
          }

          private void InitInterstitial(AdsMediationType adsMediationType)
          {
               InterstitialAdManager.Init(adsMediationType);
               DebugAds.Log("Setup Interstitial Done");
          }

          public void ShowInterstitial(
               UnityAction closedCallback = null,
               UnityAction showSuccessCallback = null,
               UnityAction showFailCallback = null,
               bool isTracking = true, bool isSkipCapping = false)
          {
               InterstitialAdManager.CallToShowAd("", closedCallback, showSuccessCallback, showFailCallback, isTracking,
                    isSkipCapping);
          }

          private void ResetAdsInterstitialCappingTime()
          {
               InterstitialAdManager.ResetCooldown();
          }

          public bool IsInterstitialAdLoaded()
          {
               return InterstitialAdManager.IsLoaded();
          }

          public bool CanShowInterstitialAd()
          {
               return InterstitialAdManager.IsAdReady();
          }

          public bool IsPassLevelToShowInterstitial(int level)
          {
               DebugAds.Log("currentLevel: " + level + " Level need passed " + LevelPassToShowInterstitial);
               return level >= LevelPassToShowInterstitial;
          }
     }
}