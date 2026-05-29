using System.Collections.Generic;
using UnityEngine;
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

          [Tooltip("When Remote Config is not ready, controls banner auto-refresh. RC key banner_auto_refresh overrides when fetched.")]
          public bool isAutoRefreshBannerByCode = false;

          [Tooltip("Seconds between banner reloads when auto-refresh is on (local default; RC banner_auto_refresh_time overrides).")]
          public float bannerAutoRefreshIntervalSeconds = 15f;

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
