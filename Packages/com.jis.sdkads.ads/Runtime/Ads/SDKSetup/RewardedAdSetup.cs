namespace JisSDKAds.Ads
{
     public partial class SDKSetup
     {
          public AdsMediationType rewardedAdsMediationType;

          public bool isRewardedShowingOnStart = false;

          public string rewardedAdUnitID_MAX
          {
               get => maxAdsSetup.RewardedAdUnitID;
               set => maxAdsSetup.RewardedAdUnitID = value;
          }

          public string rewardedAdUnitID_ADMOB
          {
               get => admobAdsSetup.RewardedAdUnitID;
               set => admobAdsSetup.RewardedAdUnitID = value;
          }

     }
}
