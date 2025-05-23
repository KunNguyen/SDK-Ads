using System.Collections;
using System.Threading.Tasks;
using ABIMaxSDKAds.Scripts.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace SDK.AdsManagers
{
     public abstract class UnitAdManager : MonoBehaviour
     {
          public delegate bool AdChecking();
          [field: SerializeField] public AdsMediationController MediationController { get; set; }
          [field: SerializeField] public SDKSetup SDKSetup { get; set; }
          [field: SerializeField] public AdsConfig AdsConfig { get; set; }
          [field: SerializeField] public AdsMediationType AdsMediationType { get; set; }
          [field: SerializeField] public bool IsInited { get; set; } = false;
          [field: SerializeField] public bool IsActive { get; set; }
          [field: SerializeField] public bool IsReady { get; set; } = false;
          [field: SerializeField] public bool IsPaused { get; set; } = false;
          [field: SerializeField] public string Placement { get; set; }

          public virtual bool IsShowingAd
          {
               get => IsShowingAdChecking();
               protected set => MarkShowingAds?.Invoke(value);
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
               MediationController = mediationController;
          }

          public abstract void Init(AdsMediationType adsMediationType);

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
          private IEnumerator CoWaitingForRemoteConfigUpdate()
          {
               while (!IsInited)
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
               MarkShowingAds?.Invoke(IsShowingAd);
               AdShowSuccessCallback?.Invoke();
          }

          public virtual void OnAdShowFailed()
          {
               IsShowingAd = false;
               MarkShowingAds?.Invoke(IsShowingAd);
               AdShowFailCallback?.Invoke();
          }

          public virtual void OnAdClose()
          {
               IsShowingAd = false;
               MarkShowingAds?.Invoke(IsShowingAd);
               AdCloseCallback?.Invoke();
          }

          public virtual void OnAdLoadSuccess()
          {
               IsShowingAd = false;
               MarkShowingAds?.Invoke(IsShowingAd);
               AdLoadSuccessCallback?.Invoke();
          }

          public virtual void OnAdLoadFail()
          {
               IsShowingAd = false;
               MarkShowingAds?.Invoke(IsShowingAd);
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