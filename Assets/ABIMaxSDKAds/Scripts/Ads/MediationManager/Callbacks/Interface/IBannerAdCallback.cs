using UnityEngine.Events;

namespace SDK.Struct
{
     public interface IBannerAdCallback : IAdCallback
     {
          UnityAction Expanded { get; set; }
          UnityAction Collapsed { get; set; }
          UnityAction Hided { get; set; }
     }
}