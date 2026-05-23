using System.Collections.Generic;

namespace JisSDKAds.Ads
{
     public partial class SDKSetup
     {
          public AdsMediationType rewardedAdsMediationType;

          public bool IsLinkToRemoveAds = true;

          public string rewardedAdUnitID_MAX
          {
               get => maxAdsSetup.RewardedAdUnitID;
               set => maxAdsSetup.RewardedAdUnitID = value;
          }

          public List<string> rewardedAdUnitID_ADMOB
          {
               get => admobAdsSetup.RewardedAdUnitIDList;
               set => admobAdsSetup.RewardedAdUnitIDList = value;
          }

     }
}
