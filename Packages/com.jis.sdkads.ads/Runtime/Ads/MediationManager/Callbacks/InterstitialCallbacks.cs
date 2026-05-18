using JisSDKAds.Ads.Mediation.Callbacks;
using UnityEngine.Events;

namespace JisSDKAds.Ads.Mediation.Callbacks
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