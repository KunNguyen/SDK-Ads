using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using SDK.AdsManagers.Interface;
using UnityEngine;

namespace SDK.AdsManagers
{
     public class CollapsibleBannerAdManager : UnitAdManager, IBannerAdUnit
     {
          [field: SerializeField] public bool IsOpenOnStart { get; set; } = false;
          [field: SerializeField] public bool IsAutoRefresh { get; set; } = false;
          [field: SerializeField] public bool IsExpanded { get; set; } = false;
          [field: SerializeField] private float AutoResetTime { get; set; } = 15f;
          private CancellationTokenSource AutoResetCancellationTokenSource { get; set; }

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
          }
          public void StartAutoReset()
          {
               if (!IsAutoRefresh) return;
               StopAutoReset();
               AutoResetCancellationTokenSource = new CancellationTokenSource();
               _ = WaitForBannerAutoReset();
          }
          public void StopAutoReset()
          {
               AutoResetCancellationTokenSource?.Cancel();
               AutoResetCancellationTokenSource?.Dispose();
               AutoResetCancellationTokenSource = new CancellationTokenSource();
          }
          private async Task WaitForBannerAutoReset()
          {
               while (!AutoResetCancellationTokenSource.IsCancellationRequested && !IsRemoveAds() && !IsCheatAds() && IsShowingAd)
               {
                    await Task.Delay((int)(AutoResetTime * 1000), AutoResetCancellationTokenSource.Token);
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

          public override void Show()
          {
               MediationController.ShowCollapsibleBannerAds();
               StartAutoReset();
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
          }
          public void OnAdHide()
          {
          }
     }
}