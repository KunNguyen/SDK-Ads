using UnityEngine.Events;

namespace SDK.Struct
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