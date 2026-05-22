using UnityEngine;

namespace JisSDKAds.Ads
{
     public partial class SDKSetup
     {
          [field: SerializeField]
          public bool IsActiveAppsflyer { get; set; }= true;

          [field: SerializeField]
          public bool IsActiveFirebaseAuth { get; set; } = false;

          [field: SerializeField]
          public bool IsActiveIAP { get; set; } = false;
          
          [field: SerializeField]
          public bool IsActiveAdImpressionTracking { get; set; } = true;

          [field: SerializeField]
          public bool IsActiveCustomAdImpressionTracking { get; set; } = true;

          [field: SerializeField]
          public string CustomAdImpressionEventName { get; set; } = "ad_impression_abi";
          
          public AdsMediationType adsMediationType;

          public string sdkKey_MAX
          {
               get => maxAdsSetup.SDKKey;
               set => maxAdsSetup.SDKKey = value;
          }

     }
}
