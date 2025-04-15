using System;
using System.Collections;
using UnityEngine;

namespace SDK.AdsManagers
{
     public class AppOpenAdManager : UnitAdManager
     {
          [field: SerializeField] public bool IsFirstOpen { get; set; } = true;
          [field: SerializeField] public bool IsActiveShowAdsFirstTime { get; set; } = true;
          [field: SerializeField] public bool IsActiveResumeAds { get; set; } = true;
          [field: SerializeField] public DateTime CloseAdsTime { get; set; }
          [field: SerializeField] public float AdResumeCappingTime { get; set; } = 0f;
          public override void Init(AdsMediationType adsMediationType)
          {
               if (AdsMediationType != adsMediationType) return;
               if (IsRemoveAds() || IsCheatAds()) return;
               MediationController.InitAppOpenAds(
                    OnAdLoadSuccess, 
                    OnAdLoadFail, 
                    OnAdClose, 
                    OnAdShowSuccess,
                    OnAdShowFailed);
               StartCoroutine(coCheckingShowAppOpenAdsOnStart());
          }
          IEnumerator coCheckingShowAppOpenAdsOnStart()
          {
               if (IsCheatAds() || IsRemoveAds()) yield break;
               if(IsFirstOpen && !IsActiveShowAdsFirstTime) yield break;
               float startCheckingTime = Time.realtimeSinceStartup;
               while (Time.realtimeSinceStartup < 10f)
               {
                    if (IsReady)
                    {
                         if (!IsActive) break;
                         if (IsLoaded())
                         {
                              ShowAdsFirstTime();
                              break;  
                         }
                    }
                    yield return new WaitForSeconds(0.2f);
               }

               Debug.Log("AOA Done Checking --- Start Time = " + startCheckingTime + " End Time = " +
                         Time.realtimeSinceStartup);
          }
          public void ShowAdsFirstTime()
          {
               if (IsCheatAds() || IsRemoveAds()) return;
               MediationController.ShowAppOpenAds();
          }

          public override void RequestAd()
          {
               if(IsRemoveAds() || IsCheatAds()) return;
               MediationController.RequestAppOpenAds();
          }

          public override void Show()
          {
               if (IsLoaded())
               {
                    MarkShowingAds(true);
                    Debug.Log("Start Force Show App Open Ads");
                    MediationController.ShowAppOpenAds();
               }
          }

          public override void OnAdClose()
          {
               base.OnAdClose();
               CloseAdsTime = DateTime.Now;
               RequestAd();
          }

          public override void OnAdShowSuccess()
          {
               base.OnAdShowSuccess();
          }

          public override bool IsLoaded()
          {
               return MediationController != null && MediationController.IsAppOpenAdsLoaded();
          }
     }
}