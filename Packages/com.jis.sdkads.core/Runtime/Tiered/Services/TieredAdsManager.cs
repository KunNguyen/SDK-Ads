using System;
using System.Collections.Generic;
using System.Diagnostics;
using JisSDKAds.Core.Interfaces;
using JisSDKAds.Core.Tiered.Config;
using JisSDKAds.Core.Tiered.Interfaces;
using JisSDKAds.Core.Tiered.Logging;
using JisSDKAds.Core.Tiered.Models;
using UnityEngine;

namespace JisSDKAds.Core.Tiered.Services
{
    public class TieredAdsManager : ITieredAdsManager
    {
        readonly TieredAdsConfig _config;
        readonly ITieredAdBackend _backend;
        readonly ITierMetricsCollector _metrics;
        readonly ITierPersistenceService _persistence;
        readonly ITierPromotionController _promotion;
        readonly ITierAnalyticsTracker _analytics;
        readonly AdLoadScheduler _scheduler;

        readonly TierInventory _interstitialInventory = new TierInventory { AdsType = AdsFormatType.Interstitial };
        readonly TierInventory _rewardedInventory = new TierInventory { AdsType = AdsFormatType.Rewarded };

        readonly Dictionary<string, IInterstitialAd> _interstitialAds = new Dictionary<string, IInterstitialAd>();
        readonly Dictionary<string, IRewardedAd> _rewardedAds = new Dictionary<string, IRewardedAd>();

        bool _initialized;
        bool _providerReady;

        public bool IsInitialized => _initialized;

        public TieredAdsManager(
            TieredAdsConfig config,
            ITieredAdBackend backend,
            TieredAdsRuntimeHost host,
            ITierMetricsCollector metrics = null,
            ITierPersistenceService persistence = null,
            ITierPromotionController promotion = null,
            ITierAnalyticsTracker analytics = null)
        {
            _config = config;
            _backend = backend;
            _metrics = metrics ?? new TierMetricsCollector(config);
            _persistence = persistence ?? new TierPersistenceService();
            _analytics = analytics ?? new TierAnalyticsTracker(this);
            _promotion = promotion ?? new TierPromotionController(config, _metrics, _analytics);
            _scheduler = new AdLoadScheduler(config, host, GetInventory, ExecuteLoad);
        }

        public TierInventory GetInventory(AdsFormatType format) =>
            format == AdsFormatType.Interstitial ? _interstitialInventory : _rewardedInventory;

        public AdLoadScheduler Scheduler => _scheduler;

        public void SetProviderReady(bool ready)
        {
            _providerReady = ready;
            if (ready && _initialized && _config.EnableTieredInventory)
                _scheduler.EnqueueFullInventoryRefresh();
        }

        public void Initialize()
        {
            if (_initialized) return;

            BindUnitIds(_interstitialInventory, _config.Interstitial);
            BindUnitIds(_rewardedInventory, _config.Rewarded);

            _persistence.LoadAll(_interstitialInventory, _rewardedInventory);
            CreateAdInstances();

            _scheduler.Start();
            _initialized = true;

            TieredAdsLogger.Log("TieredAdsManager initialized.");

            if (_providerReady)
                _scheduler.EnqueueFullInventoryRefresh();
        }

        public void Shutdown()
        {
            _scheduler.Stop();
            _persistence.SaveAll(_interstitialInventory, _rewardedInventory);
            _initialized = false;
        }

        public void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _persistence.SaveAll(_interstitialInventory, _rewardedInventory);
                return;
            }

            _persistence.LoadAll(_interstitialInventory, _rewardedInventory);
            _promotion.EvaluateRecovery(_interstitialInventory);
            _promotion.EvaluateRecovery(_rewardedInventory);
            _scheduler.EnqueueFullInventoryRefresh();
        }

        public void EnqueueReload(AdsFormatType format, AdTier tier)
        {
            if (!_config.IsTieredEnabledFor(format))
                return;

            _scheduler.Enqueue(format, tier);
        }

        public bool IsAnyLoaded(AdsFormatType format)
        {
            if (!_config.IsTieredEnabledFor(format))
                return false;

            var inventory = GetInventory(format);
            foreach (var unit in inventory.AllUnits)
            {
                if (unit.IsLoaded && !unit.IsDisabled)
                    return true;
            }

            return false;
        }

        #region Show — Interstitial

        public void ShowInterstitial(Action onClosed = null, Action<string> onFailed = null)
        {
            ShowTiered(
                AdsFormatType.Interstitial,
                _interstitialInventory,
                tier => _interstitialAds.TryGetValue(tier.ToString(), out var ad) ? ad : null,
                onClosed,
                onFailed);
        }

        public void ShowRewarded(
            Action onRewardEarned = null,
            Action onClosed = null,
            Action<string> onFailed = null)
        {
            ShowTieredRewarded(
                _rewardedInventory,
                onRewardEarned,
                onClosed,
                onFailed);
        }

        void ShowTiered(
            AdsFormatType format,
            TierInventory inventory,
            Func<AdTier, IInterstitialAd> getAd,
            Action onClosed,
            Action<string> onFailed)
        {
            if (!_providerReady)
            {
                onFailed?.Invoke("Provider not ready");
                return;
            }

            var tier = SelectShowTier(inventory);
            if (tier == null)
            {
                onFailed?.Invoke("No tier available");
                return;
            }

            var ad = getAd(tier.Value);
            if (ad == null || !ad.IsLoaded)
            {
                _metrics.RecordShowAttempt(format, tier.Value, false);
                TrackShowFail(format, tier.Value, inventory);
                onFailed?.Invoke($"Tier {tier.Value} not loaded");
                return;
            }

            ad.Show(
                onShown: null,
                onClosed: () =>
                {
                    var unit = inventory.GetUnit(tier.Value);
                    unit?.MarkConsumed();
                    inventory.LastSuccessfulTier = tier.Value;
                    _metrics.RecordShowAttempt(format, tier.Value, true);
                    TrackShowSuccess(format, tier.Value, inventory);
                    _persistence.Save(inventory);
                    EnqueueReload(format, tier.Value);
                    onClosed?.Invoke();
                },
                onFailed: err =>
                {
                    var unit = inventory.GetUnit(tier.Value);
                    unit?.MarkConsumed();
                    _metrics.RecordShowAttempt(format, tier.Value, false);
                    TrackShowFail(format, tier.Value, inventory);
                    _promotion.EvaluatePromotions(inventory);
                    EnqueueReload(format, tier.Value);
                    onFailed?.Invoke(err);
                });
        }

        void ShowTieredRewarded(
            TierInventory inventory,
            Action onRewardEarned,
            Action onClosed,
            Action<string> onFailed)
        {
            if (!_providerReady)
            {
                onFailed?.Invoke("Provider not ready");
                return;
            }

            var tier = SelectShowTier(inventory);
            if (tier == null)
            {
                onFailed?.Invoke("No tier available");
                return;
            }

            if (!_rewardedAds.TryGetValue(tier.Value.ToString(), out var ad) || ad == null || !ad.IsLoaded)
            {
                _metrics.RecordShowAttempt(AdsFormatType.Rewarded, tier.Value, false);
                TrackShowFail(AdsFormatType.Rewarded, tier.Value, inventory);
                onFailed?.Invoke($"Tier {tier.Value} not loaded");
                return;
            }

            ad.Show(
                onRewardEarned: () =>
                {
                    onRewardEarned?.Invoke();
                },
                onClosed: () =>
                {
                    var unit = inventory.GetUnit(tier.Value);
                    unit?.MarkConsumed();
                    inventory.LastSuccessfulTier = tier.Value;
                    _metrics.RecordShowAttempt(AdsFormatType.Rewarded, tier.Value, true);
                    TrackShowSuccess(AdsFormatType.Rewarded, tier.Value, inventory);
                    _persistence.Save(inventory);
                    EnqueueReload(AdsFormatType.Rewarded, tier.Value);
                    onClosed?.Invoke();
                },
                onFailed: err =>
                {
                    var unit = inventory.GetUnit(tier.Value);
                    unit?.MarkConsumed();
                    _metrics.RecordShowAttempt(AdsFormatType.Rewarded, tier.Value, false);
                    TrackShowFail(AdsFormatType.Rewarded, tier.Value, inventory);
                    _promotion.EvaluatePromotions(inventory);
                    EnqueueReload(AdsFormatType.Rewarded, tier.Value);
                    onFailed?.Invoke(err);
                });
        }

        AdTier? SelectShowTier(TierInventory inventory)
        {
            var candidates = BuildShowOrder(inventory);
            foreach (var tier in candidates)
            {
                var unit = inventory.GetUnit(tier);
                if (unit == null || unit.IsDisabled || string.IsNullOrEmpty(unit.UnitId))
                    continue;

                if (unit.IsLoaded)
                    return tier;

                if (IsAdLoaded(inventory.AdsType, tier))
                {
                    unit.IsLoaded = true;
                    return tier;
                }
            }

            return null;
        }

        bool IsAdLoaded(AdsFormatType format, AdTier tier)
        {
            if (format == AdsFormatType.Interstitial &&
                _interstitialAds.TryGetValue(tier.ToString(), out var inter))
                return inter.IsLoaded;

            if (format == AdsFormatType.Rewarded &&
                _rewardedAds.TryGetValue(tier.ToString(), out var reward))
                return reward.IsLoaded;

            return false;
        }

        List<AdTier> BuildShowOrder(TierInventory inventory)
        {
            var order = new List<AdTier>();
            var seen = new HashSet<AdTier>();

            void Add(AdTier tier)
            {
                if (seen.Add(tier))
                    order.Add(tier);
            }

            Add(inventory.CurrentPrimaryTier);

            if (_config.PreferLastSuccessfulTier)
                Add(inventory.LastSuccessfulTier);

            for (var t = AdTier.High; t <= AdTier.Low; t++)
                Add(t);

            for (var t = AdTier.Low; t >= AdTier.High; t--)
                Add(t);

            return order;
        }

        #endregion

        void ExecuteLoad(AdsFormatType format, AdTier tier, Action<bool> onComplete)
        {
            var inventory = GetInventory(format);
            var unit = inventory.GetUnit(tier);
            if (unit == null || string.IsNullOrEmpty(unit.UnitId) || unit.IsDisabled)
            {
                onComplete?.Invoke(false);
                return;
            }

            unit.MarkLoadStarted();
            var sw = Stopwatch.StartNew();

            if (format == AdsFormatType.Interstitial)
                LoadInterstitial(unit, inventory, sw, onComplete);
            else
                LoadRewarded(unit, inventory, sw, onComplete);
        }

        void LoadInterstitial(TierAdUnit unit, TierInventory inventory, Stopwatch sw, Action<bool> onComplete)
        {
            if (!_interstitialAds.TryGetValue(unit.Tier.ToString(), out var ad))
            {
                unit.MarkLoadFailed();
                onComplete?.Invoke(false);
                return;
            }

            ad.Load(
                onLoaded: () =>
                {
                    sw.Stop();
                    unit.MarkLoadSuccess((float)sw.ElapsedMilliseconds);
                    _metrics.RecordLoadAttempt(AdsFormatType.Interstitial, unit.Tier, true, (float)sw.ElapsedMilliseconds);
                    TrackLoadSuccess(AdsFormatType.Interstitial, unit, inventory, (float)sw.ElapsedMilliseconds);
                    HandleRecoveryLoad(inventory, unit.Tier);
                    _persistence.Save(inventory);
                    onComplete?.Invoke(true);
                },
                onFailed: err =>
                {
                    sw.Stop();
                    unit.MarkLoadFailed();
                    _metrics.RecordLoadAttempt(AdsFormatType.Interstitial, unit.Tier, false, (float)sw.ElapsedMilliseconds);
                    TrackLoadFail(AdsFormatType.Interstitial, unit, inventory, (float)sw.ElapsedMilliseconds);
                    _promotion.EvaluatePromotions(inventory);
                    _scheduler.ScheduleRetry(AdsFormatType.Interstitial, unit.Tier, unit.RetryCount);
                    _persistence.Save(inventory);
                    TieredAdsLogger.Warn($"Interstitial load failed {unit.Tier}: {err}");
                    onComplete?.Invoke(false);
                });
        }

        void LoadRewarded(TierAdUnit unit, TierInventory inventory, Stopwatch sw, Action<bool> onComplete)
        {
            if (!_rewardedAds.TryGetValue(unit.Tier.ToString(), out var ad))
            {
                unit.MarkLoadFailed();
                onComplete?.Invoke(false);
                return;
            }

            ad.Load(
                onLoaded: () =>
                {
                    sw.Stop();
                    unit.MarkLoadSuccess((float)sw.ElapsedMilliseconds);
                    _metrics.RecordLoadAttempt(AdsFormatType.Rewarded, unit.Tier, true, (float)sw.ElapsedMilliseconds);
                    TrackLoadSuccess(AdsFormatType.Rewarded, unit, inventory, (float)sw.ElapsedMilliseconds);
                    HandleRecoveryLoad(inventory, unit.Tier);
                    _persistence.Save(inventory);
                    onComplete?.Invoke(true);
                },
                onFailed: err =>
                {
                    sw.Stop();
                    unit.MarkLoadFailed();
                    _metrics.RecordLoadAttempt(AdsFormatType.Rewarded, unit.Tier, false, (float)sw.ElapsedMilliseconds);
                    TrackLoadFail(AdsFormatType.Rewarded, unit, inventory, (float)sw.ElapsedMilliseconds);
                    _promotion.EvaluatePromotions(inventory);
                    _scheduler.ScheduleRetry(AdsFormatType.Rewarded, unit.Tier, unit.RetryCount);
                    _persistence.Save(inventory);
                    TieredAdsLogger.Warn($"Rewarded load failed {unit.Tier}: {err}");
                    onComplete?.Invoke(false);
                });
        }

        void HandleRecoveryLoad(TierInventory inventory, AdTier tier)
        {
            var unit = inventory.GetUnit(tier);
            if (unit == null || !unit.IsTemporarilyDisabled)
                return;

            _metrics.RecordRecoverySuccess(inventory.AdsType, tier);
            if (_metrics.GetRecoverySuccessCount(inventory.AdsType, tier) >= 3)
                _promotion.TryRestore(inventory, tier, "Recovery load threshold met");
        }

        void BindUnitIds(TierInventory inventory, TierUnit tierUnit)
        {
            inventory.High.UnitId = tierUnit?.High;
            inventory.Mid.UnitId = tierUnit?.Mid;
            inventory.Low.UnitId = tierUnit?.Low;
        }

        void CreateAdInstances()
        {
            _interstitialAds.Clear();
            _rewardedAds.Clear();

            foreach (var unit in _interstitialInventory.AllUnits)
            {
                if (string.IsNullOrEmpty(unit.UnitId)) continue;
                _interstitialAds[unit.Tier.ToString()] = _backend.CreateInterstitial(unit.UnitId);
            }

            foreach (var unit in _rewardedInventory.AllUnits)
            {
                if (string.IsNullOrEmpty(unit.UnitId)) continue;
                _rewardedAds[unit.Tier.ToString()] = _backend.CreateRewarded(unit.UnitId);
            }
        }

        void TrackLoadSuccess(AdsFormatType format, TierAdUnit unit, TierInventory inventory, float latency)
        {
            _analytics.Track(new TierAnalyticsEvent
            {
                EventName = "tier_load_success",
                AdsType = format,
                NewTier = unit.Tier,
                SuccessCount = unit.SuccessCount,
                FailCount = unit.FailCount,
                FillRate = unit.FillRate,
                AverageResponseTime = unit.AverageResponseTime,
                LoadLatency = latency
            });
        }

        void TrackLoadFail(AdsFormatType format, TierAdUnit unit, TierInventory inventory, float latency)
        {
            _analytics.Track(new TierAnalyticsEvent
            {
                EventName = "tier_load_fail",
                AdsType = format,
                NewTier = unit.Tier,
                SuccessCount = unit.SuccessCount,
                FailCount = unit.FailCount,
                FillRate = unit.FillRate,
                LoadLatency = latency
            });
        }

        void TrackShowSuccess(AdsFormatType format, AdTier tier, TierInventory inventory)
        {
            var unit = inventory.GetUnit(tier);
            _analytics.Track(new TierAnalyticsEvent
            {
                EventName = "tier_show_success",
                AdsType = format,
                NewTier = tier,
                SuccessCount = unit?.SuccessCount ?? 0,
                FailCount = unit?.FailCount ?? 0,
                FillRate = unit?.FillRate ?? 0f
            });
        }

        void TrackShowFail(AdsFormatType format, AdTier tier, TierInventory inventory)
        {
            var unit = inventory.GetUnit(tier);
            _analytics.Track(new TierAnalyticsEvent
            {
                EventName = "tier_show_fail",
                AdsType = format,
                NewTier = tier,
                SuccessCount = unit?.SuccessCount ?? 0,
                FailCount = unit?.FailCount ?? 0,
                FillRate = unit?.FillRate ?? 0f
            });
        }
    }
}
