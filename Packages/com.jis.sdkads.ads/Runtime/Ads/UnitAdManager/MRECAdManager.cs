using JisSDKAds.Ads.UnitAdManagers.Interface;
using JisSDKAds.Common;
using JisSDKAds.Firebase;
using UnityEngine;
using UnityEngine.Events;

namespace JisSDKAds.Ads.UnitAdManagers
{
     public class MRecAdManager : UnitAdManager, IBannerAdUnit
     {
          public bool IsActiveByRemote { get; set; } = false;
          public override bool IsShowingAd { get; protected set; }    
          public override void Init()
          {
               if (IsRemoveAds() || IsCheatAds()) return;
               foreach (AdsMediationController t in AdsConfig.adsMediations)
               {
                    t.InitRMecAds(OnAdLoadSuccess, OnAdLoadFail, OnAdClicked, OnAdExpanded,OnAdCollapsed);
               }
               Status = AdStatus.Inited;
          }
          protected override void UpdateRemoteConfigValue()
          {
               {
                    IsActiveByRemote = FirebaseManager.Instance.GetConfigBool(Keys.key_remote_mrec_active);
                    Debug.Log("=============== Active MRec Ads " + IsActiveByRemote);
               }
               IsReady = true;
          }

          public override void RequestAd()
          {
               if (IsRemoveAds() || IsCheatAds()) return;
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
               MediationController?.ShowMRecAds();
          }

          public override bool IsLoaded()
          {
               return MediationController != null && MediationController.IsMRecLoaded();
          }

          public override bool IsAdReady()
          {
               return IsActive && IsReady && !IsRemoveAds() && !IsCheatAds() &&  IsLoaded();
          }

          public void OnAdCollapsed()
          {
               Debug.Log("MRec OnAdCollapsed");
               
          }
          public void OnAdExpanded()
          {
               Debug.Log("MRec OnAdExpanded");
               
          }
     }
}