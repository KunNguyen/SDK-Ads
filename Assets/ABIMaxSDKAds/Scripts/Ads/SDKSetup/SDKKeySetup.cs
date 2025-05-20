using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace SDK
{
     public partial class SDKSetup
     {
          [field: SerializeField, BoxGroup("SDK Key"), PropertyOrder(1)]
          public bool IsActiveAppsflyer { get; set; }= true;
          
          [field: SerializeField, BoxGroup("SDK Key"), PropertyOrder(1)]
          public bool IsActiveAdImpressionTracking { get; set; } = true;

          [field: SerializeField, BoxGroup("SDK Key"), PropertyOrder(1)]
          public bool IsActiveCustomAdImpressionTracking { get; set; } = true;

          [field: SerializeField, BoxGroup("SDK Key"), PropertyOrder(1)]
          [ShowIf("@IsActiveCustomAdImpression == true")]
          public string CustomAdImpressionEventName { get; set; } = "ad_impression_abi";
          
          [BoxGroup("SDK Key"), PropertyOrder(1)] 
          public AdsMediationType adsMediationType;

          [BoxGroup("SDK Key"), PropertyOrder(1)]
          [ShowInInspector, ShowIf("@adsMediationType == AdsMediationType.MAX")]
          public string sdkKey_MAX
          {
               get => maxAdsSetup.SDKKey;
               set => maxAdsSetup.SDKKey = value;
          }

          [BoxGroup("SDK Key"), PropertyOrder(1)]
          [ShowInInspector, ShowIf("@adsMediationType == AdsMediationType.IRONSOURCE")]
          public string sdk_IronSource
          {
               get => ironSourceAdSetup.appKey;
               set => ironSourceAdSetup.appKey = value;
          }
     }
}