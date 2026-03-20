using ABIMaxSDKAds.Scripts;
using SDK.AdsManagers;
using UnityEngine;
using UnityEngine.Events;

namespace SDK
{
     public partial class AdsManager
     {
          private AdsConfig CollapsibleBannerAdsConfig => GetAdsConfig(AdsType.COLLAPSIBLE_BANNER);
                  [field: SerializeField] public CollapsibleBannerAdManager CollapsibleBannerAdManager { get; set; }
          
                  private UnityAction m_CollapsibleBannerCloseCallback;
          
                  private void SetupCollapsibleBannerAds()
                  {
                      DebugAds.Log("Setup Collapsible Banner");
                      CollapsibleBannerAdManager.Setup(
                          CollapsibleBannerAdsConfig,
                          CurrentSDKSetup,
                          GetSelectedMediation(AdsType.COLLAPSIBLE_BANNER));
                      onRemoveAdsEvent.AddListener(CollapsibleBannerAdManager.OnRemoveAd);
                      CollapsibleBannerAdManager.IsRemoveAds = () => IsRemoveAds;
                      CollapsibleBannerAdManager.IsCheatAds = () => isCheatAds;
                  }
                  public bool IsCollapsibleBannerExpended()
                  {
                      return CollapsibleBannerAdManager.IsExpanded;
                  }
          
                  public bool IsCollapsibleBannerShowing()
                  {
                      return CollapsibleBannerAdManager.IsShowingAd;
                  }
                  public void ShowCollapsibleBannerAds(UnityAction closeCallback = null)
                  {
                      DebugAds.Log(("Call Show Collapsible Banner Ads"));
                      CollapsibleBannerAdManager.CallToShowAd("", closeCallback);
                  }
          
                  public void HideCollapsibleBannerAds()
                  {
                      CollapsibleBannerAdManager.Hide();
                  }
          
                  public void DestroyCollapsibleBanner()
                  {
                      CollapsibleBannerAdManager.DestroyAd();
                  }
          
                  public bool IsCollapsibleBannerLoaded()
                  {
                      return CollapsibleBannerAdManager.IsLoaded();
                  }
     }
}