using System.Collections;
using JisSDKAds.Common;
using JisSDKAds.Ads.UnitAdManagers;
using UnityEngine;

namespace JisSDKAds.Ads
{
     public partial class AdsManager
     {
          private AdsConfig AppOpenAdsConfig => GetAdsConfig(AdsType.APP_OPEN);
          [field: SerializeField] public AppOpenAdManager AppOpenAdManager { get; set; }
          private void SetupAppOpenAds()
          {
               AppOpenAdManager.Setup(
                    AppOpenAdsConfig,
                    CurrentSDKSetup,
                    GetSelectedMediation(AdsType.APP_OPEN));
               onRemoveAdsEvent.AddListener(AppOpenAdManager.OnRemoveAd);
               AppOpenAdManager.IsRemoveAds = () => IsRemoveAds;
               AppOpenAdManager.IsCheatAds = () => isCheatAds;
               AppOpenAdManager.MarkShowingAds = MarkShowingAds;
               AppOpenAdManager.IsShowingAdChecking = () => AdsStateMachine.GetCurrentState() == AdsStateMachine.AdsState.ShowingAds;
            
               DebugAds.Log("Setup App Open Ads Done");
          }
          private void ShowAppOpenAds()
          {
               AppOpenAdManager.CallToShowAd();
          }
        
          private void DelayShowAppOpenAds()
          {
               StartCoroutine(CoDelayShowAppOpenAds());
          }

          private IEnumerator CoDelayShowAppOpenAds()
          {
               yield return new WaitForSeconds(0.3f);
               ShowAppOpenAds();
          }
          private void RequestAppOpenAds()
          {
               AppOpenAdManager.RequestAd();
          }
          private bool IsAppOpenAdsLoaded()
          {
               return AppOpenAdManager.IsLoaded();
          }
     }
}