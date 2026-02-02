using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace SDK
{
     public partial class SDKSetup
     {
          [BoxGroup("APP OPEN"), PropertyOrder(5)] 
          public AdsMediationType appOpenAdsMediationType;

          [BoxGroup("APP OPEN"), PropertyOrder(5)]
          [ShowInInspector, ShowIf("@appOpenAdsMediationType == AdsMediationType.MAX")]
          public string appOpenAdUnitID_MAX
          {
               get => maxAdsSetup.AppOpenAdUnitID;
               set => maxAdsSetup.AppOpenAdUnitID = value;
          }

          [BoxGroup("APP OPEN"), PropertyOrder(5)]
          [ShowInInspector, ShowIf("@appOpenAdsMediationType == AdsMediationType.ADMOB")]
          public List<string> appOpenAdUnitID_ADMOB
          {
               get => admobAdsSetup.AppOpenAdUnitIDList;
               set => admobAdsSetup.AppOpenAdUnitIDList = value;
          }
     }
}