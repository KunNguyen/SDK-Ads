namespace JisSDKAds.Ads
{
     public partial class SDKSetup
     {
          public AdsMediationType appOpenAdsMediationType;

          public string appOpenAdUnitID_MAX
          {
               get => maxAdsSetup.AppOpenAdUnitID;
               set => maxAdsSetup.AppOpenAdUnitID = value;
          }

          public string appOpenAdUnitID_ADMOB
          {
               get => admobAdsSetup.AppOpenAdUnitID;
               set => admobAdsSetup.AppOpenAdUnitID = value;
          }

     }
}
