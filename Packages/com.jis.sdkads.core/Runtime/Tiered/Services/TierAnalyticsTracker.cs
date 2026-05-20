using JisSDKAds.Core.Tiered.Config;
using JisSDKAds.Core.Tiered.Interfaces;
using JisSDKAds.Core.Tiered.Logging;
using JisSDKAds.Core.Tiered.Models;

namespace JisSDKAds.Core.Tiered.Services
{
    public class TierAnalyticsTracker : ITierAnalyticsTracker
    {
        readonly ITieredAdsManager _manager;

        public TierAnalyticsTracker(ITieredAdsManager manager)
        {
            _manager = manager;
        }

        public void Track(TierAnalyticsEvent evt)
        {
            TieredAdsLogger.Log(
                $"{evt.EventName} | {evt.AdsType} | {evt.PreviousTier}->{evt.NewTier} | " +
                $"fail={evt.FailCount} success={evt.SuccessCount} fill={evt.FillRate:F1}% " +
                $"avgMs={evt.AverageResponseTime:F0} latency={evt.LoadLatency:F0} " +
                $"promo={evt.PromotionReason} recovery={evt.RecoveryReason}");

            TieredAdEvents.Raise(evt);
        }
    }

    /// <summary>Static hooks for game / Firebase bridge.</summary>
    public static class TieredAdEvents
    {
        public static event System.Action<TierAnalyticsEvent> OnAnalyticsEvent;

        public static void Raise(TierAnalyticsEvent evt) => OnAnalyticsEvent?.Invoke(evt);
    }
}
