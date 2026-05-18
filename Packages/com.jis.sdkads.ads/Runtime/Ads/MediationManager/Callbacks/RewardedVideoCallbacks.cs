using UnityEngine.Events;

namespace JisSDKAds.Ads.Mediation.Callbacks
{
     public class RewardedVideoCallbacks
     {
          public UnityAction LoadedSuccess;
          public UnityAction LoadedFail;
          public UnityAction<bool> Closed;
          public UnityAction Completed;
          public UnityAction Displayed;
          public UnityAction DisplayedFailed;
          public UnityAction Clicked;
     }
}