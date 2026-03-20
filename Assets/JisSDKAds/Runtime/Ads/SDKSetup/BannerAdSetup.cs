using System.Collections.Generic;
using GoogleMobileAds.Api;
using Sirenix.OdinInspector;

namespace SDK
{
     public partial class SDKSetup
     {
          [BoxGroup("BANNER"), PropertyOrder(4)] 
          public AdsMediationType bannerAdsMediationType;
#if UNITY_AD_MAX
          [BoxGroup("BANNER"), PropertyOrder(4)]
          [ShowInInspector, ShowIf("@bannerAdsMediationType == AdsMediationType.MAX")] 
          public MaxSdkBase.BannerPosition maxBannerAdsPosition;
#endif
#if UNITY_AD_ADMOB
          [BoxGroup("BANNER"), PropertyOrder(4)]
          [ShowInInspector, ShowIf("@bannerAdsMediationType == AdsMediationType.ADMOB")]
          public AdPosition admobBannerAdsPosition;
#endif

          [BoxGroup("BANNER"), PropertyOrder(4)]
          [ShowInInspector, ShowIf("@bannerAdsMediationType != AdsMediationType.NONE")]
          public bool isBannerShowingOnStart = false;

          [BoxGroup("BANNER"), PropertyOrder(4)]
          [ShowInInspector, ShowIf("@bannerAdsMediationType != AdsMediationType.NONE")]
          public bool isAutoRefreshBannerByCode = false;

          [BoxGroup("BANNER"), PropertyOrder(4)]
          [ShowInInspector, ShowIf("@bannerAdsMediationType == AdsMediationType.MAX")]
          public string bannerAdUnitID_MAX
          {
               get => maxAdsSetup.BannerAdUnitID;
               set => maxAdsSetup.BannerAdUnitID = value;
          }

          [BoxGroup("BANNER"), PropertyOrder(4)]
          [ShowInInspector, ShowIf("@bannerAdsMediationType == AdsMediationType.ADMOB")]
          public List<string> bannerAdUnitID_ADMOB
          {
               get => admobAdsSetup.BannerAdUnitIDList;
               set => admobAdsSetup.BannerAdUnitIDList = value;
          }

          [BoxGroup("BANNER"), PropertyOrder(4)]
          [ShowInInspector, ShowIf("@bannerAdsMediationType == AdsMediationType.IRONSOURCE")]
          public string bannerAdUnitID_IRONSOURCE
          {
               get => ironSourceAdSetup.bannerID;
               set => ironSourceAdSetup.bannerID = value;
          }
     }
}