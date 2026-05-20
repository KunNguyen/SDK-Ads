using System;
using System.Collections;
using System.Collections.Generic;
using JisSDKAds.Core.Tiered.Config;
using JisSDKAds.Core.Tiered.Interfaces;
using JisSDKAds.Core.Tiered.Logging;
using JisSDKAds.Core.Tiered.Models;
using UnityEngine;

namespace JisSDKAds.Core.Tiered.Services
{
    public class AdLoadScheduler : IAdLoadScheduler
    {
        readonly TieredAdsConfig _config;
        readonly TieredAdsRuntimeHost _host;
        readonly Func<AdsFormatType, TierInventory> _getInventory;
        readonly Action<AdsFormatType, AdTier, Action<bool>> _executeLoad;

        readonly Queue<LoadRequest> _queue = new Queue<LoadRequest>();
        readonly HashSet<string> _pendingKeys = new HashSet<string>();
        readonly HashSet<string> _scheduledRetryKeys = new HashSet<string>();

        Coroutine _processor;
        bool _running;

        public bool IsProcessing { get; private set; }

        public AdLoadScheduler(
            TieredAdsConfig config,
            TieredAdsRuntimeHost host,
            Func<AdsFormatType, TierInventory> getInventory,
            Action<AdsFormatType, AdTier, Action<bool>> executeLoad)
        {
            _config = config;
            _host = host;
            _getInventory = getInventory;
            _executeLoad = executeLoad;
        }

        public void Start()
        {
            if (_running) return;
            _running = true;
            _processor = _host.StartHostCoroutine(ProcessQueue());
        }

        public void Stop()
        {
            _running = false;
            if (_processor != null)
                _host.StopHostCoroutine(_processor);
            _processor = null;
            _queue.Clear();
            _pendingKeys.Clear();
            IsProcessing = false;
        }

        public void Enqueue(AdsFormatType format, AdTier tier, bool force = false)
        {
            if (!_config.IsTieredEnabledFor(format))
                return;

            var inventory = _getInventory(format);
            var unit = inventory?.GetUnit(tier);
            if (unit == null || string.IsNullOrEmpty(unit.UnitId))
                return;

            if (!force)
            {
                if (unit.IsLoaded || unit.IsLoading || unit.IsDisabled)
                    return;
            }

            var key = RequestKey(format, tier);
            if (_pendingKeys.Contains(key))
                return;

            _queue.Enqueue(new LoadRequest(format, tier));
            _pendingKeys.Add(key);
            TieredAdsLogger.LogVerbose($"Enqueued load: {format}/{tier}");
        }

        public void EnqueueFullInventoryRefresh()
        {
            Enqueue(AdsFormatType.Interstitial, AdTier.High);
            Enqueue(AdsFormatType.Rewarded, AdTier.High);
            Enqueue(AdsFormatType.Interstitial, AdTier.Mid);
            Enqueue(AdsFormatType.Rewarded, AdTier.Mid);
            Enqueue(AdsFormatType.Interstitial, AdTier.Low);
            Enqueue(AdsFormatType.Rewarded, AdTier.Low);
        }

        public void ScheduleRetry(AdsFormatType format, AdTier tier, int retryCount)
        {
            var key = $"retry_{RequestKey(format, tier)}";
            if (_scheduledRetryKeys.Contains(key))
                return;

            _scheduledRetryKeys.Add(key);
            var delay = TierRetryPolicy.GetDelay(tier, retryCount);
            _host.StartHostCoroutine(CoDelayedRetry(format, tier, delay, key));
        }

        IEnumerator CoDelayedRetry(AdsFormatType format, AdTier tier, float delay, string key)
        {
            yield return new WaitForSecondsRealtime(delay);
            _scheduledRetryKeys.Remove(key);
            Enqueue(format, tier, force: true);
        }

        IEnumerator ProcessQueue()
        {
            while (_running)
            {
                if (_queue.Count == 0)
                {
                    IsProcessing = false;
                    yield return null;
                    continue;
                }

                IsProcessing = true;
                var batch = DequeueBatch();
                foreach (var request in batch)
                {
                    if (!_running) yield break;

                    var key = RequestKey(request.Format, request.Tier);
                    _pendingKeys.Remove(key);

                    var inventory = _getInventory(request.Format);
                    var unit = inventory?.GetUnit(request.Tier);
                    if (unit == null || !unit.CanAttemptLoad)
                        continue;

                    var completed = false;
                    _executeLoad(request.Format, request.Tier, _ => completed = true);

                    while (!completed)
                        yield return null;

                    if (_config.DelayBetweenLoads > 0f)
                        yield return new WaitForSecondsRealtime(_config.DelayBetweenLoads);
                }
            }
        }

        List<LoadRequest> DequeueBatch()
        {
            var batch = new List<LoadRequest>();
            var max = Mathf.Max(1, _config.MaxParallelLoads);
            while (batch.Count < max && _queue.Count > 0)
                batch.Add(_queue.Dequeue());
            return batch;
        }

        static string RequestKey(AdsFormatType format, AdTier tier) => $"{format}_{tier}";

        readonly struct LoadRequest
        {
            public readonly AdsFormatType Format;
            public readonly AdTier Tier;

            public LoadRequest(AdsFormatType format, AdTier tier)
            {
                Format = format;
                Tier = tier;
            }
        }
    }
}
