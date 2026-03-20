using SDK.Struct;
using UnityEngine.Events;

namespace JisSDKAds.Runtime.Ads.MediationManager.Callbacks
{
     public class InterstitialCallbacks : IAdCallback
     {
          public UnityAction LoadedSuccess { get; set; }
          public UnityAction LoadedFail { get; set; }
          public UnityAction<bool> Closed { get; set; }
          public UnityAction Displayed { get; set; }
          public UnityAction DisplayedFail { get; set; }
          public UnityAction Clicked { get; set; }
     }
}