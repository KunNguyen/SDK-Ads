using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace  SDK
{
     public partial class SDKSetup
     {
          [BoxGroup("INTERSTITIAL"), PropertyOrder(2)] public AdsMediationType interstitialAdsMediationType;
          [BoxGroup("INTERSTITIAL"), PropertyOrder(2)] public bool IsActiveCooldownInterstitialFromStart { get; set; } = true;

          [BoxGroup("INTERSTITIAL"),PropertyOrder(2)]
          [ShowInInspector, ShowIf("@interstitialAdsMediationType == AdsMediationType.MAX")]
          public string interstitialAdUnitID_MAX
          {
               get => maxAdsSetup.InterstitialAdUnitID;
               set => maxAdsSetup.InterstitialAdUnitID = value;
          }

          [BoxGroup("INTERSTITIAL"), PropertyOrder(2)]
          [ShowInInspector, ShowIf("@interstitialAdsMediationType == AdsMediationType.ADMOB")]
          public List<string> interstitialAdUnitID_ADMOB
          {
               get => admobAdsSetup.InterstitialAdUnitIDList;
               set => admobAdsSetup.InterstitialAdUnitIDList = value;
          }

          [BoxGroup("INTERSTITIAL"), PropertyOrder(2)]
          [ShowInInspector, ShowIf("@interstitialAdsMediationType == AdsMediationType.IRONSOURCE")]
          public string interstitialAdUnitID_IRONSOURCE
          {
               get => ironSourceAdSetup.interstitialID;
               set => ironSourceAdSetup.interstitialID = value;
          }
     }
     
}