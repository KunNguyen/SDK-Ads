using System.Collections.Generic;
#if UNITY_AD_ADMOB
using GoogleMobileAds.Api;
#endif

namespace JisSDKAds.Ads
{
     public partial class SDKSetup
     {
          public AdsMediationType bannerAdsMediationType;
#if UNITY_AD_MAX
          public MaxSdkBase.BannerPosition maxBannerAdsPosition;
#endif
#if UNITY_AD_ADMOB
          public AdPosition admobBannerAdsPosition;
#endif

          public bool isBannerShowingOnStart = false;

          public bool isAutoRefreshBannerByCode = false;

          public string bannerAdUnitID_MAX
          {
               get => maxAdsSetup.BannerAdUnitID;
               set => maxAdsSetup.BannerAdUnitID = value;
          }

          public List<string> bannerAdUnitID_ADMOB
          {
               get => admobAdsSetup.BannerAdUnitIDList;
               set => admobAdsSetup.BannerAdUnitIDList = value;
          }

     }
}
