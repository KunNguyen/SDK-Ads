using System;
using System.Collections;
using JisSDKAds.Common;
using UnityEngine;

namespace JisSDKAds.Ads.SequentialTier
{
    public sealed class SequentialTierReadyCache
    {
        public string AdUnitId;
        public AdTier Tier;
        public ISequentialTierAdAdapter Adapter;
    }

    /// <summary>Sequential Premium->Fill ladder. Tier unit IDs come from Remote Config only.</summary>
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

        int _fillFailureCount;
        bool _isLoadingFallback;
        Coroutine _fillRetryRoutine;
        float _pendingFillRetryDelaySec;

        const float SafetyLoadTimeoutSeconds = 45f;

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
            _fillFailureCount = 0;
            _isLoadingFallback = false;
            TryLoadTier(_memory.ResolveStartTier(_config));
        }

        public void CancelActiveLoad()
        {
            if (!_isLoading) return;

            StopTimeout();
            StopFillRetry();
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
                    // The failed display consumed the ad — reload, but not sooner than the minimum gap.
                    AdReloadScheduler.Schedule($"tier_{_format}_showfail", () =>
                    {
                        if (!IsReady)
                            Load();
                    });
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
            StopFillRetry();
            CancelActiveLoad();
            _loadAdapter?.Destroy();
            ClearReady();
            _isLoading = false;
        }

        void TryLoadTier(AdTier tier)
        {
            var adUnitId = _config.GetEntry(tier)?.ResolveRemoteAdUnitId();

            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                if (tier < AdTier.Fill)
                {
                    TryLoadTier(tier + 1);
                    return;
                }

                FinishLadderFailed("no_remote_tier_ad_unit_configured");
                return;
            }

            TryLoadAdUnit(adUnitId.Trim(), tier, isFallback: false);
        }

        void TryLoadFallbackAfterFillFailures()
        {
            var fallbackAdUnitId = _config.ResolveDefaultAdUnitId();
            if (string.IsNullOrWhiteSpace(fallbackAdUnitId))
            {
                FinishLadderFailed("fallback_ad_unit_not_configured");
                return;
            }

            DebugAds.Log(
                $"[SequentialTier] {_format} Fill failed {_fillFailureCount} time(s) - loading local fallback unit.");
            TryLoadAdUnit(fallbackAdUnitId.Trim(), AdTier.Fill, isFallback: true);
        }

        void TryLoadAdUnit(string adUnitId, AdTier tier, bool isFallback)
        {
            _isLoadingFallback = isFallback;
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
            if (timeout <= 0f)
                timeout = SafetyLoadTimeoutSeconds;
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
            if (_isLoadingFallback)
            {
                FinishLadderFailed("fallback_ad_unit_timeout");
                yield break;
            }
            AdvanceAfterTierFailure(tier);
        }

        void OnTierLoadSuccess(string adUnitId, AdTier tier)
        {
            StopTimeout();
            StopFillRetry();
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

            if (!_isLoadingFallback)
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

            if (_isLoadingFallback)
            {
                FinishLadderFailed("fallback_ad_unit_failed");
                return;
            }

            AdvanceAfterTierFailure(tier);
        }

        void AdvanceAfterTierFailure(AdTier failedTier)
        {
            if (failedTier < AdTier.Fill)
            {
                TryLoadTier(failedTier + 1);
                return;
            }

            _fillFailureCount++;
            if (_config.ShouldScheduleFillRetry(_fillFailureCount))
            {
                ScheduleFillRetry(_config.GetFillRetryDelaySeconds(_fillFailureCount));
                return;
            }

            TryLoadFallbackAfterFillFailures();
        }

        void FinishLadderFailed(string reason)
        {
            if (reason == "no_remote_tier_ad_unit_configured")
                DebugAds.LogWarning(
                    $"[SequentialTier] {_format} ladder has NO Remote Config tier ad unit id. " +
                    "Tiered inventory requires Remote Config tier keys; local fallback is used only after Fill fails.");

            _memory.RecordLadderFailure();
            EndLoadSession();
            SequentialTierAnalytics.LogLoadFail(_format, "", AdTier.Fill, 0, reason, 0);
            _onLoadedFail?.Invoke();
        }

        void StopFillRetry()
        {
            if (_fillRetryRoutine != null && _host != null)
                _host.StopCoroutine(_fillRetryRoutine);
            _fillRetryRoutine = null;
        }

        void ScheduleFillRetry(float delaySeconds)
        {
            StopTimeout();
            if (_fillRetryRoutine != null && _host != null)
                _host.StopCoroutine(_fillRetryRoutine);
            _pendingFillRetryDelaySec = Mathf.Max(0f, delaySeconds);
            _fillRetryRoutine = _host.StartCoroutine(CoDelayedFillRetry());
        }

        IEnumerator CoDelayedFillRetry()
        {
            if (_pendingFillRetryDelaySec > 0f)
                yield return new WaitForSecondsRealtime(_pendingFillRetryDelaySec);
            _fillRetryRoutine = null;
            if (_host == null) yield break;
            if (IsReady)
            {
                ReleasePipelineAfterResult();
                yield break;
            }

            if (AdLoadCoordinator.Instance.HoldsSessionFor(this))
            {
                TryLoadTier(AdTier.Fill);
            }
            else
            {
                ReleasePipelineAfterResult();
                Load(forceReload: true);
            }
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
