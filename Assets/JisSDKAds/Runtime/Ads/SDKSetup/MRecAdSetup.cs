using System.Collections.Generic;
#if UNITY_AD_ADMOB
using GoogleMobileAds.Api;
#endif
using Sirenix.OdinInspector;

namespace SDK
{
     public partial class SDKSetup
     {
          [BoxGroup("MREC"), PropertyOrder(6)] 
          public AdsMediationType mrecAdsMediationType;

#if UNITY_AD_ADMOB
          [BoxGroup("MREC"), PropertyOrder(6)]
          [ShowInInspector, ShowIf("@mrecAdsMediationType == AdsMediationType.ADMOB")]
          public AdPosition mrecAdsPosition;
#else
          [BoxGroup("MREC"), PropertyOrder(6)]
          public int mrecAdsPositionFallback = 8; // BottomCenter when AdMob
#endif

          [BoxGroup("MREC"), PropertyOrder(6)]
          [ShowInInspector, ShowIf("@mrecAdsMediationType == AdsMediationType.MAX")]
          public string mrecAdUnitID_MAX
          {
               get => maxAdsSetup.MrecAdUnitID;
               set => maxAdsSetup.MrecAdUnitID = value;
          }

          [BoxGroup("MREC"), PropertyOrder(6)]
          [ShowInInspector, ShowIf("@mrecAdsMediationType == AdsMediationType.ADMOB")]
          public List<string> mrecAdUnitID_ADMOB
          {
               get => admobAdsSetup.MrecAdUnitIDList;
               set => admobAdsSetup.MrecAdUnitIDList = value;
          }
     }
}