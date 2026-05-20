using JisSDKAds.Core.Tiered.Config;
using JisSDKAds.Core.Tiered.Interfaces;
using JisSDKAds.Core.Tiered.Logging;
using JisSDKAds.Core.Tiered.Models;

namespace JisSDKAds.Core.Tiered.Services
{
    public class TierPromotionController : ITierPromotionController
    {
        readonly TieredAdsConfig _config;
        readonly ITierMetricsCollector _metrics;
        readonly ITierAnalyticsTracker _analytics;

        const int RecoverySuccessRequired = 3;
        const float HighToMidFillRateThreshold = 20f;
        const float MidToLowFillRateThreshold = 15f;
        const float HighResponseTimeThresholdMs = 10000f;
        const int FailStreakThreshold = 5;

        public TierPromotionController(
            TieredAdsConfig config,
            ITierMetricsCollector metrics,
            ITierAnalyticsTracker analytics)
        {
            _config = config;
            _metrics = metrics;
            _analytics = analytics;
        }

        public void EvaluatePromotions(TierInventory inventory)
        {
            if (!_config.EnableDynamicPromotion || inventory.IsPromotionLocked)
                return;

            if (ShouldPromoteHighToMid(inventory))
            {
                inventory.High.DisableTemporarily(_config.TierDisableDuration);
                TryPromote(inventory, AdTier.High, AdTier.Mid,
                    $"FailStreak={_metrics.GetFailStreak(inventory.AdsType, AdTier.High)} " +
                    $"FillRate={_metrics.GetFillRate(inventory.AdsType, AdTier.High):F1}% " +
                    $"AvgMs={_metrics.GetAverageResponseTime(inventory.AdsType, AdTier.High):F0}");
                return;
            }

            if (ShouldPromoteMidToLow(inventory))
            {
                inventory.Mid.DisableTemporarily(_config.TierDisableDuration);
                TryPromote(inventory, AdTier.Mid, AdTier.Low,
                    $"FailStreak={_metrics.GetFailStreak(inventory.AdsType, AdTier.Mid)} " +
                    $"FillRate={_metrics.GetFillRate(inventory.AdsType, AdTier.Mid):F1}%");
            }
        }

        public void EvaluateRecovery(TierInventory inventory)
        {
            if (!_config.EnableDynamicPromotion)
                return;

            TryRecoverTier(inventory, AdTier.High);
            TryRecoverTier(inventory, AdTier.Mid);
        }

        bool ShouldPromoteHighToMid(TierInventory inventory)
        {
            if (inventory.CurrentPrimaryTier != AdTier.High)
                return false;

            var format = inventory.AdsType;
            return _metrics.GetFailStreak(format, AdTier.High) >= FailStreakThreshold
                   || _metrics.GetFillRate(format, AdTier.High) < HighToMidFillRateThreshold
                   || _metrics.GetAverageResponseTime(format, AdTier.High) > HighResponseTimeThresholdMs;
        }

        bool ShouldPromoteMidToLow(TierInventory inventory)
        {
            if (inventory.CurrentPrimaryTier != AdTier.Mid)
                return false;

            var format = inventory.AdsType;
            return _metrics.GetFailStreak(format, AdTier.Mid) >= FailStreakThreshold
                   || _metrics.GetFillRate(format, AdTier.Mid) < MidToLowFillRateThreshold;
        }

        void TryRecoverTier(TierInventory inventory, AdTier tier)
        {
            var unit = inventory.GetUnit(tier);
            if (unit == null || !unit.IsTemporarilyDisabled)
                return;

            if (System.DateTime.UtcNow < unit.TemporaryDisabledUntil)
                return;

            var recoveryCount = _metrics.GetRecoverySuccessCount(inventory.AdsType, tier);
            if (recoveryCount >= RecoverySuccessRequired)
            {
                unit.ClearTemporaryDisable();
                _metrics.ResetRecoveryCount(inventory.AdsType, tier);
                TryRestore(inventory, tier, $"Recovered after {RecoverySuccessRequired} successful loads");
            }
        }

        public bool TryPromote(TierInventory inventory, AdTier from, AdTier to, string reason)
        {
            if (inventory.IsPromotionLocked)
                return false;

            inventory.CurrentPrimaryTier = to;
            inventory.SetPromotionLock(_config.PromotionLockDuration);

            _analytics.Track(new TierAnalyticsEvent
            {
                EventName = "tier_promoted",
                AdsType = inventory.AdsType,
                PreviousTier = from,
                NewTier = to,
                FailCount = inventory.GetUnit(from)?.FailCount ?? 0,
                SuccessCount = inventory.GetUnit(from)?.SuccessCount ?? 0,
                FillRate = _metrics.GetFillRate(inventory.AdsType, from),
                AverageResponseTime = _metrics.GetAverageResponseTime(inventory.AdsType, from),
                PromotionReason = reason
            });

            _analytics.Track(new TierAnalyticsEvent
            {
                EventName = "tier_disabled",
                AdsType = inventory.AdsType,
                PreviousTier = from,
                NewTier = from,
                PromotionReason = reason
            });

            TieredAdsLogger.Warn($"Promoted {inventory.AdsType}: {from} -> {to} ({reason})");
            return true;
        }

        public bool TryRestore(TierInventory inventory, AdTier tier, string reason)
        {
            if (inventory.IsPromotionLocked && tier == AdTier.High)
                return false;

            var previous = inventory.CurrentPrimaryTier;
            inventory.CurrentPrimaryTier = tier;

            _analytics.Track(new TierAnalyticsEvent
            {
                EventName = tier == AdTier.High ? "tier_restored" : "tier_recovered",
                AdsType = inventory.AdsType,
                PreviousTier = previous,
                NewTier = tier,
                RecoveryReason = reason
            });

            TieredAdsLogger.Log($"Restored {inventory.AdsType} primary tier to {tier} ({reason})");
            return true;
        }
    }
}
