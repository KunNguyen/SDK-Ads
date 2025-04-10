using UnityEngine.Events;

namespace SDK.AdsManagers
{
     public class BannerAdManager : UnitAdManager
     {
          public override void InitAd(AdsConfig adsConfig, SDKSetup sdkSetup, AdsMediationController mediationController)
          {
               base.InitAd(adsConfig, sdkSetup, mediationController);
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
               if (IsCheatAds() && IsRemoveAds())
               {
                    return;
               }
               ShowAd();
          }
          public override void ShowAd()
          {
               MediationController.ShowBannerAds();
          }

          public override void HideAd()
          {
               MediationController.HideBannerAds();
          }
          public void OnAdCollapsed()
          {
          }
          public void OnAdExpanded()
          {
          }
          
          
          public override void DestroyAd()
          {
               base.DestroyAd();
               MediationController.DestroyBannerAds();
          }

          public override bool IsAdLoaded()
          {
               return MediationController.IsBannerLoaded();
          }
     }
}