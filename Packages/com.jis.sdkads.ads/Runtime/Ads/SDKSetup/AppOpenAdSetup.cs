using System.Collections.Generic;

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

          public List<string> appOpenAdUnitID_ADMOB
          {
               get => admobAdsSetup.AppOpenAdUnitIDList;
               set => admobAdsSetup.AppOpenAdUnitIDList = value;
          }

     }
}
