using System.Collections;
using System.Threading.Tasks;
using JisSDKAds.Common;
using UnityEngine;
using UnityEngine.Events;

namespace JisSDKAds.Ads.UnitAdManagers
{
     public abstract class UnitAdManager : MonoBehaviour
     {
          public enum AdStatus
          {
               NotInited,
               Inited,
          }
          public delegate bool AdChecking();
          [field: SerializeField] public AdsMediationController MediationController { get; set; }
          [field: SerializeField] public SDKSetup SDKSetup { get; set; }
          [field: SerializeField] public AdsConfig AdsConfig { get; set; }
          [field: SerializeField] public AdsMediationType AdsMediationType { get; set; }
          [field: SerializeField] public AdStatus Status { get; set; } = AdStatus.NotInited;
          [field: SerializeField] public bool IsActive { get; set; }
          [field: SerializeField] public bool IsReady { get; set; } = false;
          [field: SerializeField] public bool IsPaused { get; set; } = false;
          [field: SerializeField] public string Placement { get; set; }

          private bool _isShowingAdState;

          public virtual bool IsShowingAd
          {
               get => IsShowingAdChecking != null ? IsShowingAdChecking() : _isShowingAdState;
               protected set
               {
                    _isShowingAdState = value;
                    MarkShowingAds?.Invoke(value);
               }
          }

          protected UnityAction AdCloseCallback;
          protected UnityAction AdLoadSuccessCallback;
          protected UnityAction AdLoadFailCallback;
          protected UnityAction AdShowSuccessCallback;
          protected UnityAction AdShowFailCallback;
          
          
          public UnityAction<bool> MarkShowingAds { get; set; }
          public Task ShowLoadingPanel { get; set; } 
          public UnityAction CloseLoadingPanel { get; set; }
          public AdChecking IsShowingAdChecking { get; set; }
          public AdChecking IsCheatAds { get; set; }
          public AdChecking IsRemoveAds { get; set; }

          public virtual void Setup(AdsConfig adsConfig, SDKSetup sdkSetup, AdsMediationController mediationController)
          {
               SDKSetup = sdkSetup;
               AdsConfig = adsConfig;
               IsActive = adsConfig.isActive;
               AdsConfig.isActive = SDKSetup.IsActiveAdsType(AdsConfig.adsType);
               AdsMediationType = adsConfig.adsMediationType;
               MediationController = mediationController ?? adsConfig.GetAdsMediation();
          }

          protected bool TryGetMediationController(out AdsMediationController controller)
          {
               controller = MediationController;
               if (controller != null)
                    return true;

               DebugAds.LogWarning(
                    $"[{GetType().Name}] MediationController is null — wait for AdsManager.IsReady or check mediation in scene.");
               return false;
          }

          public abstract void Init();

          public virtual void RequestAd()
          {
          }

          public virtual void CallToShowAd(
               string placementName = "",
               UnityAction closedCallback = null,
               UnityAction showSuccessCallback = null,
               UnityAction showFailCallback = null,
               bool isTracking = true,
               bool isSkipCapping = false)
          {
               Placement = placementName;
               AdShowSuccessCallback = showSuccessCallback;
               AdShowFailCallback = showFailCallback;
               AdCloseCallback = closedCallback;
          }

          public abstract void Show();

          public virtual void Hide()
          {
          }

          public virtual void DestroyAd()
          {
          }
          
          public virtual void UpdateRemoteConfig()
          {
               StartCoroutine(CoWaitingForRemoteConfigUpdate());
          }

          /// <summary>Apply Remote Config values immediately (call after <see cref="Init"/> when RC was pre-fetched).</summary>
          public void ApplyRemoteConfigNow()
          {
               if (Status != AdStatus.Inited) return;
               UpdateRemoteConfigValue();
          }

          private IEnumerator CoWaitingForRemoteConfigUpdate()
          {
               while (Status != AdStatus.Inited)
               {
                    yield return Yields.EndOfFrame;
               }
               UpdateRemoteConfigValue();
          }

          protected virtual void UpdateRemoteConfigValue()
          {
               
          }

          public virtual void OnAdShowSuccess()
          {
               IsShowingAd = true;
               AdShowSuccessCallback?.Invoke();
          }

          public virtual void OnAdShowFailed()
          {
               IsShowingAd = false;
               AdShowFailCallback?.Invoke();
          }

          public virtual void OnAdClose()
          {
               IsShowingAd = false;
               AdCloseCallback?.Invoke();
          }

          public virtual void OnAdLoadSuccess()
          {
               IsShowingAd = false;
               AdLoadSuccessCallback?.Invoke();
          }

          public virtual void OnAdLoadFail()
          {
               IsShowingAd = false;
               AdLoadFailCallback?.Invoke();
          }

          public virtual void OnAdClicked()
          {
          }

          public virtual void OnPause(bool paused)
          {
               IsPaused = paused;
          }
          public virtual void OnRemoveAd(){}
          public abstract bool IsLoaded();
          public abstract bool IsAdReady();
          
     }
}