using System.Collections;
using ABIMaxSDKAds.Scripts;
using SDK.AdsManagers;
using UnityEngine;

namespace SDK
{
     public partial class AdsManager
     {
          private AdsConfig AppOpenAdsConfig => GetAdsConfig(AdsType.APP_OPEN);
          [field: SerializeField] public AppOpenAdManager AppOpenAdManager { get; set; }
          private void SetupAppOpenAds()
          {
               AppOpenAdManager.Setup(
                    AppOpenAdsConfig,
                    SDKSetup,
                    GetSelectedMediation(AdsType.APP_OPEN));
               OnRemoveAdsEvent.AddListener(AppOpenAdManager.OnRemoveAd);
               AppOpenAdManager.IsRemoveAds = () => IsRemoveAds;
               AppOpenAdManager.IsCheatAds = () => IsCheatAds;
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
               StartCoroutine(coDelayShowAppOpenAds());
          }

          IEnumerator coDelayShowAppOpenAds()
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