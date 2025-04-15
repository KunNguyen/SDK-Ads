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
               if (IsRemoveAds() || IsCheatAds()) return;
               MediationController.InitRMecAds(OnAdLoadSuccess, OnAdLoadFail, OnAdClicked, OnAdExpanded,OnAdCollapsed);
          }

          public override void RequestAd()
          {
               if (IsRemoveAds()) return;
               MediationController.RequestMRecAds();
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
               Show();
               
          }
          
          
          public override void Show()
          {
               MediationController.ShowMRecAds();
          }

          public override bool IsLoaded()
          {
               return MediationController != null && MediationController.IsMRecLoaded();
          }

          public override bool IsAdReady()
          {
               return !IsRemoveAds() && !IsCheatAds() &&  IsLoaded();
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