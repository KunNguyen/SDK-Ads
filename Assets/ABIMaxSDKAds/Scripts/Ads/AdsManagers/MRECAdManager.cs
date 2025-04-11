using SDK.AdsManagers.Interface;
using UnityEngine;
using UnityEngine.Events;

namespace SDK.AdsManagers
{
     public class MRECAdManager : UnitAdManager, IBannerAdUnit
     {
          public override void Init(AdsMediationType adsMediationType)
          {
               if(AdsMediationType!= adsMediationType) return;
               if (IsRemoveAds()) return;
               MediationController.InitRMecAds(OnAdLoadSuccess, OnAdLoadFail, OnAdClick, OnAdExpanded,OnAdCollapsed);
          }

          public override void RequestAd()
          {
          }

          public override void CallToShowAd(string placementName = "", UnityAction closedCallback = null, UnityAction showSuccessCallback = null,
               UnityAction showFailCallback = null, bool isTracking = true, bool isSkipCapping = false)
          {
               base.CallToShowAd(placementName, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
               if (IsCheatAds() || IsRemoveAds())
               {
                    OnAdShowSuccess();
                    return;
               }
               ShowAd();
               
          }
          
          public override void ShowAd()
          {
               MediationController.ShowMRecAds();
          }

          public override bool IsAdLoaded()
          {
               return MediationController != null && MediationController.IsMRecLoaded();
          }

          public void OnAdCollapsed()
          {
               Debug.Log("MREC OnAdCollapsed");
               
          }
          public void OnAdExpanded()
          {
               Debug.Log("MREC OnAdExpanded");
               
          }
     }
}