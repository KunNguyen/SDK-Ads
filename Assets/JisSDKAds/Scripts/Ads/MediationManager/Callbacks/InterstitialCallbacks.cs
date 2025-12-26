using UnityEngine.Events;

namespace SDK.Struct
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