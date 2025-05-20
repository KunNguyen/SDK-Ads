using UnityEngine.Events;

namespace SDK.Struct
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