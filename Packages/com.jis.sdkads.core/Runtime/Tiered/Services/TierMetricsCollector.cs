using System;
using System.Collections.Generic;
using JisSDKAds.Core.Tiered.Config;
using JisSDKAds.Core.Tiered.Interfaces;
using JisSDKAds.Core.Tiered.Logging;
using JisSDKAds.Core.Tiered.Models;

namespace JisSDKAds.Core.Tiered.Services
{
    public class TierMetricsCollector : ITierMetricsCollector
    {
        readonly TieredAdsConfig _config;
        readonly Dictionary<string, RollingMetrics> _windows = new Dictionary<string, RollingMetrics>();

        public TierMetricsCollector(TieredAdsConfig config)
        {
            _config = config;
        }

        public void RecordLoadAttempt(AdsFormatType format, AdTier tier, bool success, float latencyMs)
        {
            var window = GetWindow(format, tier);
            window.RecordLoad(success, latencyMs);
            TieredAdsLogger.LogVerbose(
                $"Metrics load {format}/{tier}: success={success}, latency={latencyMs:F0}ms, fill={window.FillRate:F1}%");
        }

        public void RecordShowAttempt(AdsFormatType format, AdTier tier, bool success)
        {
            GetWindow(format, tier).RecordShow(success);
        }

        public int GetFailStreak(AdsFormatType format, AdTier tier) => GetWindow(format, tier).FailStreak;

        public float GetFillRate(AdsFormatType format, AdTier tier) => GetWindow(format, tier).FillRate;

        public float GetAverageResponseTime(AdsFormatType format, AdTier tier) =>
            GetWindow(format, tier).AverageResponseTime;

        public float GetShowSuccessRate(AdsFormatType format, AdTier tier) =>
            GetWindow(format, tier).ShowSuccessRate;

        public void RecordRecoverySuccess(AdsFormatType format, AdTier tier) =>
            GetWindow(format, tier).RecoverySuccessCount++;

        public int GetRecoverySuccessCount(AdsFormatType format, AdTier tier) =>
            GetWindow(format, tier).RecoverySuccessCount;

        public void ResetRecoveryCount(AdsFormatType format, AdTier tier) =>
            GetWindow(format, tier).RecoverySuccessCount = 0;

        RollingMetrics GetWindow(AdsFormatType format, AdTier tier)
        {
            var key = $"{format}_{tier}";
            if (!_windows.TryGetValue(key, out var window))
            {
                window = new RollingMetrics(_config.RollingWindowSize);
                _windows[key] = window;
            }

            return window;
        }

        class RollingMetrics
        {
            readonly int _size;
            readonly Queue<LoadSample> _loads = new Queue<LoadSample>();
            readonly Queue<bool> _shows = new Queue<bool>();

            public int FailStreak;
            public int RecoverySuccessCount;

            public RollingMetrics(int size) => _size = size;

            public float FillRate
            {
                get
                {
                    if (_loads.Count == 0) return 0f;
                    var success = 0;
                    foreach (var s in _loads)
                        if (s.Success) success++;
                    return (float)success / _loads.Count * 100f;
                }
            }

            public float AverageResponseTime
            {
                get
                {
                    if (_loads.Count == 0) return 0f;
                    var total = 0f;
                    var count = 0;
                    foreach (var s in _loads)
                    {
                        if (!s.Success) continue;
                        total += s.LatencyMs;
                        count++;
                    }

                    return count > 0 ? total / count : 0f;
                }
            }

            public float ShowSuccessRate
            {
                get
                {
                    if (_shows.Count == 0) return 0f;
                    var success = 0;
                    foreach (var s in _shows)
                        if (s) success++;
                    return (float)success / _shows.Count * 100f;
                }
            }

            public void RecordLoad(bool success, float latencyMs)
            {
                _loads.Enqueue(new LoadSample(success, latencyMs));
                while (_loads.Count > _size)
                    _loads.Dequeue();

                if (success)
                    FailStreak = 0;
                else
                    FailStreak++;
            }

            public void RecordShow(bool success)
            {
                _shows.Enqueue(success);
                while (_shows.Count > _size)
                    _shows.Dequeue();
            }

            struct LoadSample
            {
                public bool Success;
                public float LatencyMs;

                public LoadSample(bool success, float latencyMs)
                {
                    Success = success;
                    LatencyMs = latencyMs;
                }
            }
        }
    }
}
