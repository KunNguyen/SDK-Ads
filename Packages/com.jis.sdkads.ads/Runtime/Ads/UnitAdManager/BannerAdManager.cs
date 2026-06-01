using System.Threading;
using System.Threading.Tasks;
using JisSDKAds.Ads.UnitAdManagers.Interface;
using JisSDKAds.Common;
using UnityEngine;
using UnityEngine.Events;

namespace JisSDKAds.Ads.UnitAdManagers
{
     public class BannerAdManager : UnitAdManager, IBannerAdUnit
     {
          public override bool IsShowingAd { get; protected set; }
          [field: SerializeField] public bool IsAutoRefreshBanner { get; set; } = false;
          [field: SerializeField] public float BannerAutoResetTime { get; set; } = 15f;
          private CancellationTokenSource AutoResetCancellationTokenSource { get; set; }
          private bool _wantsBannerVisible;

          public override void Init()
          {
               if (IsRemoveAdsActive() || IsCheatAdsActive()) return;
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
               Status = AdStatus.Inited;
          }

          protected override void UpdateRemoteConfigValue()
          {
               base.UpdateRemoteConfigValue();
               BannerRefreshSettings.Resolve(SDKSetup, out var enabled, out var interval);
               IsAutoRefreshBanner = enabled;
               BannerAutoResetTime = interval;
               DebugAds.Log($"[BannerAdManager] autoRefresh={IsAutoRefreshBanner} interval={BannerAutoResetTime:0.#}s");
               if (IsAutoRefreshBanner)
                    StartAutoReset();
               else
                    StopAutoReset();
          }

          public override void RequestAd()
          {
               if (!TryGetMediationController(out var mediation))
                    return;
               if (mediation.IsBannerLoaded())
                    return;
               mediation.RequestBannerAds();
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
               var token = AutoResetCancellationTokenSource.Token;
               while (!token.IsCancellationRequested && !IsRemoveAdsActive() && !IsCheatAdsActive() && _wantsBannerVisible)
               {
                    await Task.Delay((int)(BannerAutoResetTime * 1000), token);
                    if (token.IsCancellationRequested || !_wantsBannerVisible)
                         break;

                    if (!TryGetMediationController(out var mediation))
                         continue;

                    mediation.DestroyBannerAds();
                    mediation.RequestBannerAds();
               }
          }

          public override void CallToShowAd(string placementName = "", UnityAction closedCallback = null, UnityAction showSuccessCallback = null,
               UnityAction showFailCallback = null, bool isTracking = true, bool isSkipCapping = false)
          {
               base.CallToShowAd(placementName, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
               if (IsCheatAdsActive() || IsRemoveAdsActive()) return;
               Show();
          }

          public override void Show()
          {
               if (!TryGetMediationController(out var mediation))
                    return;
               _wantsBannerVisible = true;
               IsShowingAd = true;
               mediation.ShowBannerAds();
               if (IsAutoRefreshBanner)
                    StartAutoReset();
          }

          public override void Hide()
          {
               if (!TryGetMediationController(out var mediation))
                    return;
               _wantsBannerVisible = false;
               IsShowingAd = false;
               StopAutoReset();
               mediation.HideBannerAds();
          }

          public override void OnAdLoadSuccess()
          {
               AdLoadSuccessCallback?.Invoke();
               if (_wantsBannerVisible)
               {
                    IsShowingAd = true;
                    if (TryGetMediationController(out var mediation))
                         mediation.ShowBannerAds();
               }
          }

          public override void OnAdLoadFail()
          {
               AdLoadFailCallback?.Invoke();
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
               DebugAds.Log("Banner OnAdCollapsed");
          }

          public void OnAdExpanded()
          {
               DebugAds.Log("Banner OnAdExpanded");
          }

          public override void DestroyAd()
          {
               base.DestroyAd();
               if (!TryGetMediationController(out var mediation))
                    return;
               mediation.DestroyBannerAds();
               if (!_wantsBannerVisible)
                    IsShowingAd = false;
          }

          public override bool IsLoaded()
          {
               return MediationController != null && MediationController.IsBannerLoaded();
          }

          public override bool IsAdReady()
          {
               return !IsCheatAdsActive() && !IsRemoveAdsActive() && IsLoaded();
          }
     }
}
