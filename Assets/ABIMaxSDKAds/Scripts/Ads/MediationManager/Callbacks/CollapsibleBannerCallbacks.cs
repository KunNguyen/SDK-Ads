using UnityEngine.Events;

namespace SDK.Struct
{
     public class CollapsibleBannerCallbacks : IBannerAdCallback
     {
          public UnityAction LoadedSuccess { get; set; }
          public UnityAction LoadedFail { get; set; }
          public UnityAction<bool> Closed { get; set; }
          public UnityAction Displayed { get; set; }
          public UnityAction DisplayedFail { get; set; }
          public UnityAction Clicked { get; set; }
          public UnityAction Expanded { get; set; }
          public UnityAction Collapsed { get; set; }
          public UnityAction Hided { get; set; }
          public UnityAction Destroyed { get; set; }
     }
}