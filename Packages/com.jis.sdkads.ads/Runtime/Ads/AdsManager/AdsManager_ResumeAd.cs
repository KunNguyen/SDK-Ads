using JisSDKAds.Ads.UnitAdManagers.Interface;
using UnityEngine;

namespace JisSDKAds.Ads
{
     public partial class AdsManager
     {
          [field: SerializeField] public ResumeAdManager ResumeAdManager { get; set; }

          public void InitResumeAdManager()
          {
               ResumeAdManager.IsActive = true;
               ResumeAdManager.Init();
               onRemoveAdsEvent.AddListener(AppOpenAdManager.OnRemoveAd);
            
               ResumeAdManager.IsCheatAds = () => isCheatAds;
               ResumeAdManager.IsRemoveAds = () => IsRemoveAds;
               ResumeAdManager.IsShowingAdChecking = IsShowingAds;
            
               ResumeAdManager.ShowLoadingPanel = ShowLoadingPanel();
               ResumeAdManager.CloseLoadingPanel = CloseLoadingPanel;
          }
     }
}