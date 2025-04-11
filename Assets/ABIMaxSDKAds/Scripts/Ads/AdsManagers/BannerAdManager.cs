using SDK.AdsManagers.Interface;
using UnityEngine;
using UnityEngine.Events;

namespace SDK.AdsManagers
{
     public class BannerAdManager : UnitAdManager, IBannerAdUnit
     {
          public override void Init(AdsMediationType adsMediationType)
          {
               if(AdsMediationType != adsMediationType) return;
               if (IsRemoveAds()) return;
               MediationController.InitBannerAds(
                    OnAdLoadSuccess,
                    OnAdLoadFail,
                    OnAdCollapsed,
                    OnAdExpanded,
                    OnAdShowSuccess,
                    OnAdShowFail);
          }

          public override void RequestAd()
          {
               if (MediationController.IsBannerLoaded()) return;
               MediationController.RequestBannerAds();
          }

          public override void CallToShowAd(string placementName = "", UnityAction closedCallback = null, UnityAction showSuccessCallback = null,
               UnityAction showFailCallback = null, bool isTracking = true, bool isSkipCapping = false)
          {
               base.CallToShowAd(placementName, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
               Debug.Log("Banner CallToShowAd");
               if (IsCheatAds() && IsRemoveAds())
               {
                    return;
               }
               ShowAd();
          }
          public override void ShowAd()
          {
               Debug.Log("Banner ShowAd");
               MediationController.ShowBannerAds();
          }

          public override void HideAd()
          {
               Debug.Log("Banner HideAd");
               MediationController.HideBannerAds();
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
               MediationController.DestroyBannerAds();
          }

          public override bool IsAdLoaded()
          {
               return MediationController.IsBannerLoaded();
          }
     }
}