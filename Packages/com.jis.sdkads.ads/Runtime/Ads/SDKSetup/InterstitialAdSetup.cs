using System.Collections.Generic;

namespace JisSDKAds.Ads
{
     public partial class SDKSetup
     {
          public AdsMediationType interstitialAdsMediationType;
          public bool IsActiveCooldownInterstitialFromStart { get; set; } = true;

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
