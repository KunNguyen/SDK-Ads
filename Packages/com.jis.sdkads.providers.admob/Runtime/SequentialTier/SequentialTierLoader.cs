#if UNITY_AD_ADMOB
using System;
using System.Collections;
using GoogleMobileAds.Api;
using JisSDKAds.Ads.SequentialTier;
using UnityEngine;

namespace JisSDKAds.Providers.AdMob.SequentialTier
{
    internal sealed class ReadySequentialTierCache
    {
        public string AdUnitId;
        public AdTier Tier;
        public ISequentialTierAdAdapter Adapter;
    }

    /// <summary>Sequential Premium→Fill ladder; one tier load at a time.</summary>
    internal sealed class SequentialTierLoader
    {
        readonly MonoBehaviour _host;
        readonly string _format;
        readonly SequentialTierConfig _config;
        readonly SequentialTierMemory _memory;
        readonly Func<ISequentialTierAdAdapter> _createAdapter;

        ISequentialTierAdAdapter _loadAdapter;
        ReadySequentialTierCache _ready;
        bool _isLoading;
        int _loadGeneration;
        AdTier _currentTier;
        Coroutine _timeoutRoutine;
        DateTime _loadStartedUtc;

        Action _onLoadedSuccess;
        Action _onLoadedFail;
        SequentialTierShowHooks _showHooks;

        public SequentialTierLoader(
            MonoBehaviour host,
            string formatKey,
            string analyticsFormat,
            SequentialTierConfig config,
            Func<ISequentialTierAdAdapter> createAdapter)
        {
            _host = host;
            _format = analyticsFormat;
            _config = config;
            _memory = new SequentialTierMemory(formatKey);
            _createAdapter = createAdapter;
            _loadAdapter = createAdapter();
            _memory.Load();
        }

        public bool IsLoading => _isLoading;
        public bool IsReady => _ready?.Adapter != null && _ready.Adapter.IsReady;
        public ReadySequentialTierCache ReadyCache => _ready;

        public void SetCallbacks(Action onLoadedSuccess, Action onLoadedFail, SequentialTierShowHooks showHooks)
        {
            _onLoadedSuccess = onLoadedSuccess;
            _onLoadedFail = onLoadedFail;
            _showHooks = showHooks;
        }

        public void Load(bool forceReload = false)
        {
            if (_isLoading) return;
            if (IsReady && !forceReload) return;

            if (forceReload)
                ClearReady();

            _isLoading = true;
            _loadGeneration++;
            TryLoadTier(_memory.ResolveStartTier(_config));
        }

        public bool Show()
        {
            if (!IsReady)
            {
                Load();
                return false;
            }

            var cache = _ready;
            SequentialTierAnalytics.LogShowStart(_format, cache.AdUnitId, cache.Tier);
            var hooks = _showHooks;
            cache.Adapter.RegisterShowCallbacks(new SequentialTierShowHooks
            {
                onClosed = hooks.onClosed,
                onOpened = hooks.onOpened,
                onRewardGranted = hooks.onRewardGranted,
                onPaid = hooks.onPaid,
                onFailed = error =>
                {
                    SequentialTierAnalytics.LogShowFail(
                        _format, cache.AdUnitId, cache.Tier, error?.GetCode() ?? 0, error?.GetMessage());
                    ClearReady();
                    hooks.onFailed?.Invoke(error);
                    Load();
                }
            });

            if (!cache.Adapter.TryShow())
            {
                SequentialTierAnalytics.LogShowFail(_format, cache.AdUnitId, cache.Tier, 0, "not_ready");
                ClearReady();
                _showHooks.onFailed?.Invoke(null);
                Load();
                return false;
            }

            return true;
        }

        public void Destroy()
        {
            StopTimeout();
            _loadAdapter?.Destroy();
            ClearReady();
            _isLoading = false;
        }

        void TryLoadTier(AdTier tier)
        {
            var entry = _config.GetEntry(tier);
            var adUnitId = entry != null && entry.HasUnitId
                ? entry.ResolveAdUnitId()
                : _config.ResolveDefaultAdUnitId();

            if (string.IsNullOrEmpty(adUnitId))
            {
                if (tier < AdTier.Fill)
                {
                    TryLoadTier(tier + 1);
                    return;
                }

                FinishLadderFailed("no_ad_unit_configured");
                return;
            }

            _currentTier = tier;
            _loadStartedUtc = DateTime.UtcNow;
            var generation = _loadGeneration;

            SequentialTierAnalytics.LogLoadStart(_format, adUnitId, tier);
            ScheduleTimeout(tier, adUnitId, generation);

            _loadAdapter.Load(adUnitId, generation, generation, () =>
            {
                if (generation != _loadGeneration) return;
                OnTierLoadSuccess(adUnitId, tier);
            }, error =>
            {
                if (generation != _loadGeneration) return;
                OnTierLoadFail(adUnitId, tier, error);
            });
        }

        void ScheduleTimeout(AdTier tier, string adUnitId, int generation)
        {
            StopTimeout();
            var timeout = _config.GetTimeoutSeconds(tier);
            if (timeout <= 0f) return;
            _timeoutRoutine = _host.StartCoroutine(TimeoutCoroutine(tier, adUnitId, generation, timeout));
        }

        IEnumerator TimeoutCoroutine(AdTier tier, string adUnitId, int generation, float timeoutSeconds)
        {
            yield return new WaitForSeconds(timeoutSeconds);
            if (generation != _loadGeneration || !_isLoading || _currentTier != tier)
                yield break;

            var elapsedMs = (long)(DateTime.UtcNow - _loadStartedUtc).TotalMilliseconds;
            SequentialTierAnalytics.LogLoadTimeout(_format, adUnitId, tier, elapsedMs);

            _loadGeneration++;
            _loadAdapter.Destroy();
            _loadAdapter = _createAdapter();
            StopTimeout();
            AdvanceAfterTierFailure(tier);
        }

        void OnTierLoadSuccess(string adUnitId, AdTier tier)
        {
            StopTimeout();
            var elapsedMs = (long)(DateTime.UtcNow - _loadStartedUtc).TotalMilliseconds;
            SequentialTierAnalytics.LogLoadSuccess(_format, adUnitId, tier, elapsedMs);

            ClearReady();
            _ready = new ReadySequentialTierCache
            {
                AdUnitId = adUnitId,
                Tier = tier,
                Adapter = _loadAdapter
            };
            _loadAdapter = _createAdapter();

            _memory.RecordSuccess(tier);
            _isLoading = false;
            _onLoadedSuccess?.Invoke();
        }

        void OnTierLoadFail(string adUnitId, AdTier tier, LoadAdError error)
        {
            StopTimeout();
            var elapsedMs = (long)(DateTime.UtcNow - _loadStartedUtc).TotalMilliseconds;
            SequentialTierAnalytics.LogLoadFail(
                _format, adUnitId, tier, error?.GetCode() ?? 0, error?.GetMessage(), elapsedMs);
            _loadAdapter.Destroy();
            _loadAdapter = _createAdapter();
            AdvanceAfterTierFailure(tier);
        }

        void AdvanceAfterTierFailure(AdTier failedTier)
        {
            if (failedTier < AdTier.Fill)
            {
                TryLoadTier(failedTier + 1);
                return;
            }

            FinishLadderFailed("all_tiers_failed");
        }

        void FinishLadderFailed(string reason)
        {
            _memory.RecordLadderFailure();
            _isLoading = false;
            SequentialTierAnalytics.LogLoadFail(_format, "", AdTier.Fill, 0, reason, 0);
            _onLoadedFail?.Invoke();
        }

        void ClearReady()
        {
            _ready?.Adapter?.Destroy();
            _ready = null;
        }

        void StopTimeout()
        {
            if (_timeoutRoutine != null && _host != null)
                _host.StopCoroutine(_timeoutRoutine);
            _timeoutRoutine = null;
        }
    }
}
#endif
