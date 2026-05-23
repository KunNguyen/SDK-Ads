using System.Collections.Generic;
#if UNITY_AD_ADMOB
using GoogleMobileAds.Api;
#endif

namespace JisSDKAds.Ads
{
     public partial class SDKSetup
     {
          public AdsMediationType mrecAdsMediationType;
#if UNITY_AD_ADMOB
          public AdPosition mrecAdsPosition;
#else
          public int mrecAdsPositionFallback = 8; // BottomCenter when AdMob
#endif

          public bool isMrecShowingOnStart = false;

          public string mrecAdUnitID_MAX
          {
               get => maxAdsSetup.MrecAdUnitID;
               set => maxAdsSetup.MrecAdUnitID = value;
          }

          public List<string> mrecAdUnitID_ADMOB
          {
               get => admobAdsSetup.MrecAdUnitIDList;
               set => admobAdsSetup.MrecAdUnitIDList = value;
          }

     }
}
