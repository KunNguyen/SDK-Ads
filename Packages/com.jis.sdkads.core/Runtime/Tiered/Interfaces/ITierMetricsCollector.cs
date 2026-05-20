using JisSDKAds.Core.Tiered.Models;

namespace JisSDKAds.Core.Tiered.Interfaces
{
    public interface ITierMetricsCollector
    {
        void RecordLoadAttempt(AdsFormatType format, AdTier tier, bool success, float latencyMs);
        void RecordShowAttempt(AdsFormatType format, AdTier tier, bool success);
        int GetFailStreak(AdsFormatType format, AdTier tier);
        float GetFillRate(AdsFormatType format, AdTier tier);
        float GetAverageResponseTime(AdsFormatType format, AdTier tier);
        float GetShowSuccessRate(AdsFormatType format, AdTier tier);
        void RecordRecoverySuccess(AdsFormatType format, AdTier tier);
        int GetRecoverySuccessCount(AdsFormatType format, AdTier tier);
        void ResetRecoveryCount(AdsFormatType format, AdTier tier);
    }
}
