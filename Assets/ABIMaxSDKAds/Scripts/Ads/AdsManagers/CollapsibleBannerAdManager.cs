using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using SDK.AdsManagers.Interface;
using UnityEngine;
using UnityEngine.Events;

namespace SDK.AdsManagers
{
     public class CollapsibleBannerAdManager : UnitAdManager, IBannerAdUnit
     {
          [field: SerializeField] public bool IsOpenOnStart { get; set; } = false;
          [field: SerializeField] public bool IsAutoRefresh { get; set; } = false;
          [field: SerializeField] public bool IsExpanded { get; set; } = false;
          [field: SerializeField] public float AutoRefreshTime { get; set; } = 15f;
          private CancellationTokenSource AutoRefreshCancellationTokenSource { get; set; }

          public override void Init(AdsMediationType adsMediationType)
          {
               MediationController.InitCollapsibleBannerAds(
                    OnAdLoadSuccess,
                    OnAdLoadFail,
                    OnAdCollapsed,
                    OnAdExpanded,
                    OnAdDestroyed,
                    OnAdHide);
               StartCoroutine(coCheckingShowCollapsibleBannerAdsOnStart());
          }
          IEnumerator coCheckingShowCollapsibleBannerAdsOnStart()
          {
               yield return new WaitForSeconds(5f);
               if (!IsOpenOnStart) yield break;
               RequestAd();
               Show();
               StartAutoReset();
          }
          public void StartAutoReset()
          {
               if (!IsAutoRefresh) return;
               StopAutoReset();
               AutoRefreshCancellationTokenSource = new CancellationTokenSource();
               _ = WaitForBannerAutoReset();
          }
          public void StopAutoReset()
          {
               AutoRefreshCancellationTokenSource?.Cancel();
               AutoRefreshCancellationTokenSource?.Dispose();
               AutoRefreshCancellationTokenSource = new CancellationTokenSource();
          }
          private async Task WaitForBannerAutoReset()
          {
               while (!AutoRefreshCancellationTokenSource.IsCancellationRequested && !IsRemoveAds() && !IsCheatAds() && IsShowingAd)
               {
                    await Task.Delay((int)(AutoRefreshTime * 1000), AutoRefreshCancellationTokenSource.Token);
                    if (IsShowingAd)
                    {
                         DestroyAd();
                         RequestAd();
                    }
               }
          }

          public override void RequestAd()
          {
               MediationController.RequestCollapsibleBannerAds(IsOpenOnStart);
          }

          public override void CallToShowAd(string placementName = "", UnityAction closedCallback = null, UnityAction showSuccessCallback = null,
               UnityAction showFailCallback = null, bool isTracking = true, bool isSkipCapping = false)
          {
               base.CallToShowAd(placementName, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
               if (IsRemoveAds() || IsCheatAds()) return;
               Show();
          }

          public override void Show()
          {
               MediationController.ShowCollapsibleBannerAds();
               IsShowingAd = true;
               StartAutoReset();
          }

          public void Show(UnityAction closedCallback)
          {
               MediationController.ShowCollapsibleBannerAds();
          }
          public override void Hide()
          {
               MediationController.HideCollapsibleBannerAds();
               StopAutoReset();
          }

          public override bool IsLoaded()
          {
               return MediationController.IsCollapsibleBannerLoaded();
          }

          public override bool IsAdReady()
          {
               return !IsRemoveAds() && !IsCheatAds() && IsLoaded();
          }

          public override void OnAdLoadSuccess()
          {
               base.OnAdLoadSuccess();
               StartAutoReset();
          }
          public void OnAdCollapsed()
          {
               Debug.Log("CollapsibleBanner OnAdCollapsed");
               IsExpanded = false;
          }
          public void OnAdExpanded()
          {
               Debug.Log("CollapsibleBanner OnAdExpanded");
               IsExpanded = true;
          }
          public void OnAdDestroyed()
          {
               IsShowingAd = false;
          }
          public void OnAdHide()
          {
          }
     }
}