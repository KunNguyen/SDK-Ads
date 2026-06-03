using System;
using System.Collections;
using UnityEngine;

namespace JisSDKAds.Ads.SequentialTier
{
    public sealed class SequentialTierReadyCache
    {
        public string AdUnitId;
        public AdTier Tier;
        public ISequentialTierAdAdapter Adapter;
    }

    /// <summary>Sequential Premium→Fill ladder; one tier load at a time. Provider-agnostic.</summary>
    public sealed class SequentialTierLoader
    {
        readonly MonoBehaviour _host;
        readonly string _format;
        readonly AdLoadFormat _loadFormat;
        readonly SequentialTierConfig _config;
        readonly SequentialTierMemory _memory;
        readonly Func<ISequentialTierAdAdapter> _createAdapter;

        ISequentialTierAdAdapter _loadAdapter;
        SequentialTierReadyCache _ready;
        bool _isLoading;
        int _loadGeneration;
        AdTier _currentTier;
        Coroutine _timeoutRoutine;
        DateTime _loadStartedUtc;

        bool _fillHoldActive;
        int _fillHoldAttempt;
        Coroutine _fillHoldRoutine;

        Action _onLoadedSuccess;
        Action _onLoadedFail;
        SequentialTierShowHooks _showHooks;

        public SequentialTierLoader(
            MonoBehaviour host,
            string formatKey,
            string analyticsFormat,
            AdLoadFormat loadFormat,
            SequentialTierConfig config,
            Func<ISequentialTierAdAdapter> createAdapter)
        {
            _host = host;
            _format = analyticsFormat;
            _loadFormat = loadFormat;
            _config = config;
            _memory = new SequentialTierMemory(formatKey);
            _createAdapter = createAdapter;
            _loadAdapter = createAdapter();
            _memory.Load();
            if (host != null)
                AdLoadCoordinator.Instance.Configure(host);
            AdLoadCoordinator.Instance.RegisterLoader(this, loadFormat);
        }

        public bool IsLoading => _isLoading;
        public bool IsReady => _ready?.Adapter != null && _ready.Adapter.IsReady;
        public SequentialTierReadyCache ReadyCache => _ready;
        public AdLoadFormat LoadFormat => _loadFormat;

        public void SetCallbacks(Action onLoadedSuccess, Action onLoadedFail, SequentialTierShowHooks showHooks)
        {
            _onLoadedSuccess = onLoadedSuccess;
            _onLoadedFail = onLoadedFail;
            _showHooks = showHooks;
        }

        public void Load(bool forceReload = false, bool urgent = false)
        {
            if (_isLoading) return;
            if (IsReady && !forceReload) return;

            AdLoadCoordinator.Instance.RequestTierLoad(this, _loadFormat, forceReload, urgent);
        }

        internal void ExecuteLoad(bool forceReload)
        {
            if (_isLoading) return;
            if (IsReady && !forceReload) return;

            if (forceReload)
                ClearReady();

            _isLoading = true;
            _loadGeneration++;
            if (_fillHoldActive && _config.fillHoldMaxRetries > 0 && _fillHoldAttempt < _config.fillHoldMaxRetries)
                TryLoadTier(AdTier.Fill);
            else
                TryLoadTier(_memory.ResolveStartTier(_config));
        }

        /// <summary>Abort in-flight ladder (preempt). Does not clear a ready cached ad.</summary>
        public void CancelActiveLoad()
        {
            if (!_isLoading) return;

            StopTimeout();
            StopFillHold();
            _loadGeneration++;
            _loadAdapter?.Destroy();
            _loadAdapter = _createAdapter();
            _isLoading = false;
        }

        public bool Show()
        {
            if (!IsReady)
            {
                Load(urgent: _loadFormat == AdLoadFormat.Rewarded);
                return false;
            }

            var cache = _ready;
            SequentialTierAnalytics.LogShowStart(_format, cache.AdUnitId, cache.Tier);
            var hooks = _showHooks;
            cache.Adapter.RegisterShowCallbacks(new SequentialTierShowHooks
            {
                onClosed = hooks.onClosed,
                onOpened = () =>
                {
                    SequentialTierAnalytics.LogShowSuccess(_format, cache.AdUnitId, cache.Tier);
                    hooks.onOpened?.Invoke();
                },
                onRewardGranted = hooks.onRewardGranted,
                onPaid = hooks.onPaid,
                onFailed = error =>
                {
                    SequentialTierAnalytics.LogShowFail(
                        _format, cache.AdUnitId, cache.Tier,
                        error?.Code ?? 0, error?.Message);
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
                Load(urgent: _loadFormat == AdLoadFormat.Rewarded);
                return false;
            }

            _memory.RecordShowSuccess(cache.Tier);
            return true;
        }

        public void Destroy()
        {
            AdLoadCoordinator.Instance.UnregisterLoader(this, _loadFormat);
            StopTimeout();
            StopFillHold();
            CancelActiveLoad();
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
            }, (code, message) =>
            {
                if (generation != _loadGeneration) return;
                OnTierLoadFail(adUnitId, tier, code, message);
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
            _ready = new SequentialTierReadyCache
            {
                AdUnitId = adUnitId,
                Tier = tier,
                Adapter = _loadAdapter
            };
            _loadAdapter = _createAdapter();

            _memory.RecordSuccess(tier);
            EndLoadSession();
            _onLoadedSuccess?.Invoke();
        }

        void OnTierLoadFail(string adUnitId, AdTier tier, int code, string message)
        {
            StopTimeout();
            var elapsedMs = (long)(DateTime.UtcNow - _loadStartedUtc).TotalMilliseconds;
            SequentialTierAnalytics.LogLoadFail(_format, adUnitId, tier, code, message, elapsedMs);
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

            if (_config.fillHoldMaxRetries > 0 && _config.fillHoldRetryIntervalSeconds > 0f)
            {
                StartFillHoldIfNeeded();
                ScheduleNextFillHoldAttemptOrRestart();
                return;
            }

            FinishLadderFailed("all_tiers_failed");
        }

        void FinishLadderFailed(string reason)
        {
            _memory.RecordLadderFailure();
            EndLoadSession();
            SequentialTierAnalytics.LogLoadFail(_format, "", AdTier.Fill, 0, reason, 0);
            _onLoadedFail?.Invoke();
        }

        void StartFillHoldIfNeeded()
        {
            if (_fillHoldActive) return;
            _fillHoldActive = true;
            _fillHoldAttempt = 0;
        }

        void StopFillHold()
        {
            _fillHoldActive = false;
            _fillHoldAttempt = 0;
            if (_fillHoldRoutine != null && _host != null)
                _host.StopCoroutine(_fillHoldRoutine);
            _fillHoldRoutine = null;
        }

        void ScheduleNextFillHoldAttemptOrRestart()
        {
            if (_fillHoldActive && _fillHoldAttempt < _config.fillHoldMaxRetries)
            {
                _fillHoldAttempt++;
                StopTimeout();
                _isLoading = false;

                if (_fillHoldRoutine != null && _host != null)
                    _host.StopCoroutine(_fillHoldRoutine);
                _fillHoldRoutine = _host.StartCoroutine(CoDelayedFillReload());
                return;
            }

            StopFillHold();
            ReleasePipelineAndReload();
        }

        IEnumerator CoDelayedFillReload()
        {
            yield return new WaitForSecondsRealtime(_config.fillHoldRetryIntervalSeconds);
            if (_host == null) yield break;
            if (IsReady)
            {
                ReleasePipelineAfterResult();
                yield break;
            }

            if (AdLoadCoordinator.Instance.HoldsSessionFor(this))
                ExecuteLoad(forceReload: true);
            else
                Load(forceReload: true);
        }

        void ReleasePipelineAndReload()
        {
            ReleasePipelineAfterResult();
            Load(forceReload: true);
        }

        void ReleasePipelineAfterResult()
        {
            _isLoading = false;
            AdLoadCoordinator.Instance.NotifyTierSessionEnded(this);
        }

        void EndLoadSession()
        {
            if (!_isLoading)
                return;

            ReleasePipelineAfterResult();
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
