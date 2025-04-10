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

          public virtual void InitAd(AdsConfig adsConfig, SDKSetup sdkSetup, AdsMediationController mediationController)
          {
               SDKSetup = sdkSetup;
               AdsConfig = adsConfig;
               MediationController = mediationController;
          }
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

          public abstract void ShowAd();

          public virtual void HideAd()
          {
          }

          public virtual void DestroyAd()
          {
          }

          public virtual void OnAdShowSuccess()
          {
               IsShowingAd = true;
               MarkShowingAds?.Invoke(IsShowingAd);
               AdShowSuccessCallback?.Invoke();
          }

          public virtual void OnAdShowFail()
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

          public virtual void OnAdClick()
          {
          }

          public abstract bool IsAdLoaded();
          
     }
}