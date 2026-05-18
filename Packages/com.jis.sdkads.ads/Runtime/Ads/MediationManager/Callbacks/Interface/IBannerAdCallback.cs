using UnityEngine.Events;

namespace JisSDKAds.Ads.Mediation.Callbacks
{
     public interface IBannerAdCallback : IAdCallback
     {
          UnityAction Expanded { get; set; }
          UnityAction Collapsed { get; set; }
          UnityAction Hided { get; set; }
     }
}