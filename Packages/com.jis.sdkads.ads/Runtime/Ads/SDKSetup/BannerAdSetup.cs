using System.Collections.Generic;
using UnityEngine;
#if UNITY_AD_ADMOB
using GoogleMobileAds.Api;
#endif

namespace JisSDKAds.Ads
{
     /// <summary>
     /// MAX banner anchor — mirrors AppLovin <c>MaxSdkBase.BannerPosition</c> ordinals so the ads
     /// package does not need a reference to <c>MaxSdk.Scripts</c> (conversion happens in providers.max).
     /// </summary>
     public enum MaxBannerAdsPosition
     {
          TopLeft = 0,
          TopCenter = 1,
          TopRight = 2,
          Centered = 3,
          CenterLeft = 4,
          CenterRight = 5,
          BottomLeft = 6,
          BottomCenter = 7,
          BottomRight = 8
     }

     public partial class SDKSetup
     {
          public AdsMediationType bannerAdsMediationType;
          public MaxBannerAdsPosition maxBannerAdsPosition = MaxBannerAdsPosition.BottomCenter;
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
