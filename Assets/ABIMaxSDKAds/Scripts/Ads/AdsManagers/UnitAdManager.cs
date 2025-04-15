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
          [field: SerializeField] public bool IsActive { get; set; }
          [field: SerializeField] public bool IsReady { get; set; } = false;
          [field: SerializeField] public string Placement { get; set; }
          [field: SerializeField] public bool IsShowingAd { get; set; } = false;

          protected UnityAction AdCloseCallback;
          protected UnityAction AdLoadSuccessCallback;
          protected UnityAction AdLoadFailCallback;
          protected UnityAction AdShowSuccessCallback;
          protected UnityAction AdShowFailCallback;
          
          public UnityAction<bool> MarkShowingAds { get; set; }
          public AdChecking IsCheatAds { get; set; }
          public AdChecking IsRemoveAds { get; set; }

          public virtual void Setup(AdsConfig adsConfig, SDKSetup sdkSetup, AdsMediationController mediationController)
          {
               SDKSetup = sdkSetup;
               AdsConfig = adsConfig;
               AdsConfig.isActive = SDKSetup.IsActiveAdsType(AdsConfig.adsType);
               AdsMediationType = adsConfig.adsMediationType;
               MediationController = mediationController;
          }

          public abstract void Init(AdsMediationType adsMediationType);
          public abstract void RequestAd();

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
               IsShowingAd = false;
               MarkShowingAds?.Invoke(IsShowingAd);
          }

          public abstract bool IsLoaded();
          public abstract bool IsAdReady();

     }
}