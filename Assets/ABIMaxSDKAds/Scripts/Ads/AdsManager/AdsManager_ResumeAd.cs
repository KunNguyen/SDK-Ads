using SDK.AdsManagers.Interface;
using UnityEngine;

namespace SDK
{
     public partial class AdsManager
     {
          [field: SerializeField] public ResumeAdManager ResumeAdManager { get; set; }

          public void InitResumeAdManager()
          {
               ResumeAdManager.IsActive = true;
               ResumeAdManager.Init(AdsMediationType.ADMOB);
               OnRemoveAdsEvent.AddListener(AppOpenAdManager.OnRemoveAd);
            
               ResumeAdManager.IsCheatAds = () => IsCheatAds;
               ResumeAdManager.IsRemoveAds = () => IsRemoveAds;
               ResumeAdManager.IsShowingAdChecking = IsShowingAds;
            
               ResumeAdManager.ShowLoadingPanel = ShowLoadingPanel();
               ResumeAdManager.CloseLoadingPanel = CloseLoadingPanel;
          }
     }
}