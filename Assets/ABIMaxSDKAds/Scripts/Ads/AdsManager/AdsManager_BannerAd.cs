using ABIMaxSDKAds.Scripts;
using SDK.AdsManagers;
using UnityEngine;

namespace SDK
{
     public partial class AdsManager
     {
          private AdsConfig BannerAdsConfig => GetAdsConfig(AdsType.BANNER);
          [field: SerializeField] public BannerAdManager BannerAdManager { get; set; }

          private void SetupBannerAds()
          {
               DebugAds.Log("Setup Banner");
               BannerAdManager.Setup(
                    BannerAdsConfig,
                    SDKSetup,
                    GetSelectedMediation(AdsType.BANNER));
               OnRemoveAdsEvent.AddListener(BannerAdManager.OnRemoveAd);
               BannerAdManager.IsRemoveAds = () => IsRemoveAds;
               BannerAdManager.IsCheatAds = () => IsCheatAds;
               HideBannerAds();
          }
          private void InitBannerAds(AdsMediationType adsMediationType)
          {
               DebugAds.Log("Init Banner");
               BannerAdManager.Init(adsMediationType);
          }

          public void RequestBannerAds()
          {
               BannerAdManager.RequestAd();
          }
          public void ShowBannerAds()
          {
               DebugAds.Log(("Call Show Banner Ads"));
               BannerAdManager.Show();
          }
          public void HideBannerAds()
          {
               DebugAds.Log(("Call Hide Banner Ads"));
               BannerAdManager.Hide();
          }

          public void DestroyBanner()
          {
               BannerAdManager.DestroyAd();
          }
          public bool CanShowBannerAd()
          {
               return BannerAdManager.IsAdReady();
          }
     }
}