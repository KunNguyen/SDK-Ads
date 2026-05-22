#if UNITY_AD_ADMOB
using GoogleMobileAds.Api;
#endif

namespace JisSDKAds.Ads
{
     public partial class SDKSetup
     {
          public AdsMediationType mrecAdsMediationType;
#if UNITY_AD_ADMOB
          public AdPosition admobMrecAdsPosition;
#endif

          public bool isMrecShowingOnStart = false;

          public string mrecAdUnitID_MAX
          {
               get => maxAdsSetup.MrecAdUnitID;
               set => maxAdsSetup.MrecAdUnitID = value;
          }

          public string mrecAdUnitID_ADMOB
          {
               get => admobAdsSetup.MrecAdUnitID;
               set => admobAdsSetup.MrecAdUnitID = value;
          }

     }
}
