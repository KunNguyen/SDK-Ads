using UnityEngine.Events;

namespace JisSDKAds.Ads.Mediation.Callbacks
{
     public interface IAdCallback
     {
          UnityAction LoadedSuccess { get; set; }
          UnityAction LoadedFail { get; set; }
          UnityAction<bool> Closed { get; set; }
          UnityAction Displayed { get; set; }
          UnityAction DisplayedFail { get; set; }
          UnityAction Clicked { get; set; }
     }
}