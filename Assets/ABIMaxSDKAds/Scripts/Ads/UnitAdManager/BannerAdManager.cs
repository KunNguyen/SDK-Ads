using System.Threading;
using System.Threading.Tasks;
using ABIMaxSDKAds.Scripts;
using SDK.AdsManagers.Interface;
using UnityEngine;
using UnityEngine.Events;

namespace SDK.AdsManagers
{
     public class BannerAdManager : UnitAdManager, IBannerAdUnit
     {
          public override bool IsShowingAd { get; protected set; }    
          [field: SerializeField] public bool IsAutoRefreshBanner { get; set; } = false;
          [field: SerializeField] public float BannerAutoResetTime { get; set; } = 15f;
          private CancellationTokenSource AutoResetCancellationTokenSource { get; set; }

          public override void Init(AdsMediationType adsMediationType)
          {
               if(AdsMediationType != adsMediationType) return;
               if (IsRemoveAds() || IsCheatAds()) return;
               foreach (AdsMediationController t in AdsConfig.adsMediations)
               {
                    t.InitBannerAds(
                         OnAdLoadSuccess,
                         OnAdLoadFail,
                         OnAdCollapsed,
                         OnAdExpanded,
                         OnAdShowSuccess,
                         OnAdShowFailed,
                         OnAdClicked);
               }
               IsInited = true;
          }

          protected override void UpdateRemoteConfigValue()
          {
               base.UpdateRemoteConfigValue();
               IsAutoRefreshBanner = FirebaseManager.Instance
                    .GetConfigValue(Keys.key_remote_banner_auto_refresh).BooleanValue;
               DebugAds.Log($"=============== Active {IsAutoRefreshBanner}");
               BannerAutoResetTime = (float)FirebaseManager.Instance
                    .GetConfigValue(Keys.key_remote_banner_auto_refresh_time).DoubleValue;
               if (IsAutoRefreshBanner)
               {
                    StartAutoReset();
               }
               else
               {
                    StopAutoReset();
               }
          }

          public override void RequestAd()
          {
               if (MediationController.IsBannerLoaded()) return;
               MediationController.RequestBannerAds();
          }
          private void StartAutoReset()
          {
               StopAutoReset();
               _ = WaitForBannerAutoReset();
          }
          private void StopAutoReset()
          {
               AutoResetCancellationTokenSource?.Cancel();
               AutoResetCancellationTokenSource?.Dispose();
               AutoResetCancellationTokenSource = new CancellationTokenSource();
          }
          
          private async Task WaitForBannerAutoReset()
          {
               while(!AutoResetCancellationTokenSource.IsCancellationRequested && !IsRemoveAds() && !IsCheatAds() && IsShowingAd)
               {
                    await Task.Delay((int)(BannerAutoResetTime * 1000), AutoResetCancellationTokenSource.Token);
                    if (IsShowingAd)
                    {
                         DestroyAd();
                         RequestAd();
                    }
               }
          }

          public override void CallToShowAd(string placementName = "", UnityAction closedCallback = null, UnityAction showSuccessCallback = null,
               UnityAction showFailCallback = null, bool isTracking = true, bool isSkipCapping = false)
          {
               base.CallToShowAd(placementName, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
               Debug.Log("Banner CallToShowAd");
               if (IsCheatAds() || IsRemoveAds())return;
               Show();
          }
          public override void Show()
          {
               Debug.Log("Banner ShowAd");
               IsShowingAd = true;
               MediationController.ShowBannerAds();
          }
          public override void Hide()
          {
               Debug.Log("Banner HideAd");
               IsShowingAd = false;
               MediationController.HideBannerAds();
          }
          
          public override void OnAdShowSuccess()
          {
               IsShowingAd = true;
          }
          public override void OnAdShowFailed()
          {
          }

          public void OnAdCollapsed()
          {
               Debug.Log("Banner OnAdCollapsed");
          }
          public void OnAdExpanded()
          {
               Debug.Log("Banner OnAdExpanded");
          }
          public override void DestroyAd()
          {
               base.DestroyAd();
               Debug.Log("Banner DestroyAd");
               IsShowingAd = false;
               MediationController.DestroyBannerAds();
          }

          public override bool IsLoaded()
          {
               return MediationController != null && MediationController.IsBannerLoaded();
          }

          public override bool IsAdReady()
          {
               return !IsCheatAds() && !IsRemoveAds() && IsLoaded();
          }
     }
}