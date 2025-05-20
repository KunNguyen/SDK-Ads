using System.Collections.Generic;
using GoogleMobileAds.Api;
using Sirenix.OdinInspector;

namespace SDK
{
     public partial class SDKSetup
     {
          [BoxGroup("MREC"), PropertyOrder(6)] 
          public AdsMediationType mrecAdsMediationType;

          [BoxGroup("MREC"), PropertyOrder(6)]
          [ShowInInspector, ShowIf("@mrecAdsMediationType == AdsMediationType.ADMOB")]
          public AdPosition mrecAdsPosition;

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