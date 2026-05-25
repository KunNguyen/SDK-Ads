using System.Collections.Generic;
using JisSDKAds.Ads.SequentialTier;

namespace JisSDKAds.Ads
{
     public partial class SDKSetup
     {
          public AdsMediationType interstitialAdsMediationType;
          public bool IsActiveCooldownInterstitialFromStart { get; set; } = true;

          public bool enableTieredInterstitial =>
               interstitialAdsMediationType == AdsMediationType.ADMOB &&
               admobAdsSetup.InterstitialTierConfig.enableSequentialLadder;

          public bool enableTieredRewarded =>
               rewardedAdsMediationType == AdsMediationType.ADMOB &&
               admobAdsSetup.RewardedTierConfig.enableSequentialLadder;

          public SequentialTierConfig InterstitialTierConfig => admobAdsSetup.InterstitialTierConfig;
          public SequentialTierConfig RewardedTierConfig => admobAdsSetup.RewardedTierConfig;

          public string interstitialAdUnitID_MAX
          {
               get => maxAdsSetup.InterstitialAdUnitID;
               set => maxAdsSetup.InterstitialAdUnitID = value;
          }

          public List<string> interstitialAdUnitID_ADMOB
          {
               get => admobAdsSetup.InterstitialAdUnitIDList;
               set => admobAdsSetup.InterstitialAdUnitIDList = value;
          }

     }
}
