using JisSDKAds.Core.Tiered.Models;

namespace JisSDKAds.Core.Tiered.Interfaces
{
    public class TierAnalyticsEvent
    {
        public string EventName;
        public AdsFormatType AdsType;
        public AdTier PreviousTier;
        public AdTier NewTier;
        public int FailCount;
        public int SuccessCount;
        public float FillRate;
        public float AverageResponseTime;
        public float LoadLatency;
        public string PromotionReason;
        public string RecoveryReason;
    }

    public interface ITierAnalyticsTracker
    {
        void Track(TierAnalyticsEvent evt);
    }
}
