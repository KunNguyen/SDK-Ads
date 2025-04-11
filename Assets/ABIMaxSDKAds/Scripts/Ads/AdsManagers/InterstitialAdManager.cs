using UnityEngine;
using UnityEngine.Events;

namespace SDK.AdsManagers
{
     public class InterstitialAdManager : UnitAdManager
     {
          [field: SerializeField] public float CappingAdsCooldown;
          public override void Init(AdsMediationType adsMediationType)
          {
               if (AdsMediationType != adsMediationType) return;
               if (IsRemoveAds()) return;
               Debug.Log("Setup Interstitial");
               AdsConfig.isActive = SDKSetup.IsActiveAdsType(AdsType.INTERSTITIAL);
               if (!SDKSetup.IsActiveAdsType(AdsType.INTERSTITIAL)) return;
               foreach (AdsMediationController t in AdsConfig.adsMediations)
               {
                    t.InitInterstitialAd(
                         AdCloseCallback,
                         AdLoadSuccessCallback,
                         AdLoadFailCallback,
                         AdShowSuccessCallback,
                         AdShowFailCallback
                    );
               }

               Debug.Log("Setup Interstitial Done");
          }

          public override void RequestAd()
          {
               if(MediationController.IsInterstitialLoaded())return;
               MediationController.RequestInterstitialAd();
          }

          public override void CallToShowAd(string placementName = "", UnityAction closedCallback = null, UnityAction showSuccessCallback = null,
               UnityAction showFailCallback = null, bool isTracking = true, bool isSkipCapping = false)
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
                         OnAdShowSuccess();
                         OnAdClose();
                         return;
                    }
               }
               if (isTracking)
               {
                    ABIAnalyticsManager.Instance.TrackAdsInterstitial_ClickOnButton();
               }

               if (!IsRemoveAds())
               {
                    if (IsAdLoaded())
                    {
                         AdShowFailCallback = showFailCallback;
                         ShowAd();
                    }
               }
               else
               {
                    AdCloseCallback?.Invoke();
                    showSuccessCallback?.Invoke();
               }
          }

          public override void ShowAd()
          {
               MarkShowingAds(true);
               MediationController.ShowInterstitialAd();
          }
          public override bool IsAdLoaded()
          {
               return MediationController.IsInterstitialLoaded();
          }
     }
}