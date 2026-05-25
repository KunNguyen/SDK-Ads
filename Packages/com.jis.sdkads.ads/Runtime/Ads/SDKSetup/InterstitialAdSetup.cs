using System.Collections.Generic;
using JisSDKAds.Ads.InterstitialTier;

namespace JisSDKAds.Ads
{
     public partial class SDKSetup
     {
          public AdsMediationType interstitialAdsMediationType;
          public bool IsActiveCooldownInterstitialFromStart { get; set; } = true;

          public bool enableTieredInterstitial =>
               interstitialAdsMediationType == AdsMediationType.ADMOB &&
               admobAdsSetup.InterstitialTierConfig.enableTieredInterstitial;

          public InterstitialTierConfig InterstitialTierConfig => admobAdsSetup.InterstitialTierConfig;

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
