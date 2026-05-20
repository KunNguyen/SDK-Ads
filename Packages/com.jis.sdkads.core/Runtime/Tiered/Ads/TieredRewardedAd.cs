using System;
using JisSDKAds.Core.Interfaces;
using JisSDKAds.Core.Tiered.Models;
using JisSDKAds.Core.Tiered.Services;

namespace JisSDKAds.Core.Tiered.Ads
{
    /// <summary>
    /// IRewardedAd facade over tiered inventory. Used by Core AdManager when tiered mode is enabled.
    /// </summary>
    public class TieredRewardedAd : IRewardedAd
    {
        readonly TieredAdsManager _manager;

        public TieredRewardedAd(TieredAdsManager manager)
        {
            _manager = manager;
        }

        public bool IsLoaded => _manager.IsAnyLoaded(AdsFormatType.Rewarded);

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            if (IsLoaded)
            {
                onLoaded?.Invoke();
                return;
            }

            _manager.Scheduler.EnqueueFullInventoryRefresh();
            onLoaded?.Invoke();
        }

        public void Show(Action onRewardEarned = null, Action onClosed = null, Action<string> onFailed = null)
        {
            _manager.ShowRewarded(onRewardEarned, onClosed, onFailed);
        }
    }
}
