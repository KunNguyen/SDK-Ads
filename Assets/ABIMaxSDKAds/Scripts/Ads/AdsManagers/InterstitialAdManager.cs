using System;
using System.Threading;
using System.Threading.Tasks;
using ABI;
using ABIMaxSDKAds.Scripts;
using Firebase.RemoteConfig;
using UnityEngine;
using UnityEngine.Events;

namespace SDK.AdsManagers
{
     public class InterstitialAdManager : UnitAdManager
     {
          [field: SerializeField] public float MaxCappingAdsCooldown { get; set; }
          [field: SerializeField] public float CappingAdsCooldown { get; set; }
          [field: SerializeField] public int LevelToShowInterstitial { get; set; } = 0;
          private CancellationTokenSource StopCooldownCTS { get; set; } = new CancellationTokenSource();
          private DateTime StartRequestTime{ get; set; }
          private  bool IsLoading { get; set; } = false;

          public override void Init(AdsMediationType adsMediationType)
          {
               if (AdsMediationType != adsMediationType) return;
               if (!IsActive || IsRemoveAds() || IsCheatAds()) return;
               DebugAds.Log("Setup Interstitial");
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
               _ = AutoReloadInterstitialAd();
               DebugAds.Log("Setup Interstitial Done");
          }
          private async Task AutoReloadInterstitialAd() 
          {
               while (true)
               {
                    DebugAds.Log("Auto Loading Interstitial Ad " + IsLoaded() + " " + IsLoading + " " + IsRemoveAds() + " " + IsCheatAds() + " " + IsShowingAd);
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
                         ABIFirebaseManager.Instance.GetConfigValue(Keys.key_remote_interstitial_capping_time);
                    MaxCappingAdsCooldown = (float)configValue.DoubleValue;
                    DebugAds.Log("=============== Max Interstitial Capping Time " + MaxCappingAdsCooldown);
               }
               {
                    LevelToShowInterstitial =
                         (int)ABIFirebaseManager.Instance.GetConfigDouble(Keys.key_remote_interstitial_level);
                    DebugAds.Log("=============== Level Pass Show Interstitial " + LevelToShowInterstitial);
               }
          }

          private void StartCooldown()
          {
               CappingAdsCooldown = MaxCappingAdsCooldown;
               _ = WaitForAdCooldown();
          }
          private void StopAdCooldown()
          {
               StopCooldownCTS.Cancel();
               StopCooldownCTS.Dispose();
               StopCooldownCTS = new CancellationTokenSource();
          }
          public void RestartCooldown()
          {
               StopAdCooldown();
               StartCooldown();
          }
          private async Task WaitForAdCooldown()
          {
               try
               {
                    await Task.Delay(TimeSpan.FromSeconds(CappingAdsCooldown), StopCooldownCTS.Token);
               }
               catch (TaskCanceledException)
               {
                    // Handle cancellation if needed
               }
               finally
               {
                    CappingAdsCooldown = 0;
                    StopCooldownCTS.Dispose();
                    StopCooldownCTS = new CancellationTokenSource();
               }
          }

          public override void RequestAd()
          {
               if(MediationController.IsInterstitialLoaded())return;
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
               if (IsCheatAds())
               {
                    OnAdShowSuccess();
                    return;
               }

               if (!isSkipCapping)
               {
                    if (CappingAdsCooldown > 0)
                    {
                         showSuccessCallback?.Invoke();
                         closedCallback?.Invoke();
                         return;
                    }
               }
               if (!IsRemoveAds())
               {
                    if (isTracking)
                    {
                         ABIAnalyticsManager.Instance.TrackAdsInterstitial_ClickOnButton();
                    }
                    if (IsLoaded())
                    {
                         AdShowFailCallback = showFailCallback;
                         Show();
                    }
                    else
                    {
                         ABIAnalyticsManager.Instance.TrackAdsInterstitial_ShowFailByLoad();
                    }
               }
               else
               {
                    closedCallback?.Invoke();
                    showSuccessCallback?.Invoke();
               }
          }

          public override void Show()
          {
               MarkShowingAds(true);
               MediationController.ShowInterstitialAd();
          }
          public override bool IsLoaded()
          {
               return MediationController.IsInterstitialLoaded();
          }
          public override void OnAdShowSuccess()
          {
               base.OnAdShowSuccess();
               StartCooldown();
               ABIAnalyticsManager.Instance.TrackAdsInterstitial_ShowSuccess();
          }
          public override void OnAdShowFailed()
          {
               base.OnAdShowFailed();
               StartCooldown();
               ABIAnalyticsManager.Instance.TrackAdsInterstitial_ShowFail();
          }
          public override void OnAdClose()
          {
               base.OnAdClose();
               StartCooldown();
          }
          public override void OnAdLoadSuccess()
          {
               base.OnAdLoadSuccess();
               IsLoading = false;
               float timeFromStartRequest = (float)(DateTime.Now - StartRequestTime).TotalSeconds;
               ABIAnalyticsManager.Instance.TrackAdsInterstitial_LoadedSuccess(timeFromStartRequest);
          }

          public override void OnAdLoadFail()
          {
               base.OnAdLoadFail();
               IsLoading = false;
          }

          public override bool IsAdReady()
          {
               return CappingAdsCooldown == 0 && IsLoaded();
          }
     }
}