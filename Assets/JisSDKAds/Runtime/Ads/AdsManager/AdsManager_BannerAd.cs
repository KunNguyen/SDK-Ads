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
                    CurrentSDKSetup,
                    GetSelectedMediation(AdsType.BANNER));
               onRemoveAdsEvent.AddListener(BannerAdManager.OnRemoveAd);
               BannerAdManager.IsRemoveAds = () => IsRemoveAds;
               BannerAdManager.IsCheatAds = () => isCheatAds;
               HideBannerAds();
          }

          public void RequestBannerAds()
          {
               BannerAdManager.RequestAd();
          }
          public void ShowBannerAds()
          {
               DebugAds.Log(("Call Show Banner Ads"));
               if (!IsReady) return;
               BannerAdManager.Show();
          }
          public void HideBannerAds()
          {
               DebugAds.Log(("Call Hide Banner Ads"));
               if (!IsReady) return;
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