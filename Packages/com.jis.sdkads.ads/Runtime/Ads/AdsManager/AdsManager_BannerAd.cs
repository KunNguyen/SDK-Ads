using JisSDKAds.Common;
using JisSDKAds.Ads.UnitAdManagers;
using UnityEngine;

namespace JisSDKAds.Ads
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
               if (TryRequestBannerViaJisAds())
                    return;

               if (!IsReady)
               {
                    DebugAds.LogWarning("[AdsManager] RequestBannerAds ignored — ads not ready (wait for JisAds/AdsManager init).");
                    return;
               }

               if (BannerAdManager == null)
                    return;

               BannerAdManager.RequestAd();
          }

          public void ShowBannerAds()
          {
               DebugAds.Log(("Call Show Banner Ads"));
               if (TryRequestBannerViaJisAds())
                    return;

               if (!IsReady || BannerAdManager == null)
                    return;
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
               if (TryQueryBannerLoadedFromJisAds(out var loaded))
                    return loaded;
               return BannerAdManager.IsAdReady();
          }
     }
}