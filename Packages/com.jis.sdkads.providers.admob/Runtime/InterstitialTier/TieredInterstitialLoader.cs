#if UNITY_AD_ADMOB
using System;
using System.Collections;
using GoogleMobileAds.Api;
using JisSDKAds.Ads.InterstitialTier;
using UnityEngine;

namespace JisSDKAds.Providers.AdMob.InterstitialTier
{
    internal sealed class ReadyInterstitialCache
    {
        public string AdUnitId;
        public AdTier Tier;
        public DateTime LoadTimeUtc;
        public AdMobInterstitialAdapter Adapter;
    }

    /// <summary>
    /// Sequential PREMIUM→FILL ladder with per-tier timeout, tier memory, and single in-flight load.
    /// </summary>
    internal sealed class TieredInterstitialLoader
    {
        readonly MonoBehaviour _host;
        readonly InterstitialTierConfig _config;
        readonly InterstitialTierMemory _memory = new InterstitialTierMemory();
        AdMobInterstitialAdapter _loadAdapter = new AdMobInterstitialAdapter();

        ReadyInterstitialCache _ready;
        bool _isLoading;
        int _loadGeneration;
        AdTier _currentTier;
        Coroutine _timeoutRoutine;
        DateTime _loadStartedUtc;

        Action _onLoadedSuccess;
        Action _onLoadedFail;
        Action _onClosed;
        Action _onDisplayed;
        Action _onDisplayedFail;
        Action<AdValue> _onPaid;

        public TieredInterstitialLoader(MonoBehaviour host, InterstitialTierConfig config)
        {
            _host = host;
            _config = config;
            _memory.Load();
        }

        public bool IsLoading => _isLoading;
        public bool IsReady => _ready?.Adapter != null && _ready.Adapter.IsReady;
        public ReadyInterstitialCache ReadyCache => _ready;

        public void SetCallbacks(
            Action onLoadedSuccess,
            Action onLoadedFail,
            Action onClosed,
            Action onDisplayed,
            Action onDisplayedFail,
            Action<AdValue> onPaid)
        {
            _onLoadedSuccess = onLoadedSuccess;
            _onLoadedFail = onLoadedFail;
            _onClosed = onClosed;
            _onDisplayed = onDisplayed;
            _onDisplayedFail = onDisplayedFail;
            _onPaid = onPaid;
        }

        /// <summary>Preload next interstitial. No parallel loads; skips if already loading or ready.</summary>
        public void LoadInterstitial(bool forceReload = false)
        {
            if (_isLoading) return;
            if (IsReady && !forceReload) return;

            if (forceReload)
                ClearReady();

            _isLoading = true;
            _loadGeneration++;
            var startTier = _memory.ResolveStartTier(_config);
            TryLoadTier(startTier);
        }

        public bool ShowInterstitial()
        {
            if (!IsReady)
            {
                LoadInterstitial();
                return false;
            }

            var cache = _ready;
            InterstitialTierAnalytics.LogShowStart(cache.AdUnitId, cache.Tier);

            cache.Adapter.RegisterShowCallbacks(
                OnShowClosed,
                () =>
                {
                    InterstitialTierAnalytics.LogShowSuccess(cache.AdUnitId, cache.Tier);
                    _onDisplayed?.Invoke();
                },
                error =>
                {
                    InterstitialTierAnalytics.LogShowFail(
                        cache.AdUnitId,
                        cache.Tier,
                        error?.GetCode() ?? 0,
                        error?.GetMessage());
                    ClearReady();
                    _onDisplayedFail?.Invoke();
                    LoadInterstitial();
                },
                value =>
                {
                    InterstitialTierAnalytics.LogPaid(
                        cache.AdUnitId,
                        cache.Tier,
                        value.Value / 1_000_000d,
                        value.CurrencyCode,
                        (int)value.Precision);
                    _onPaid?.Invoke(value);
                });

            if (!cache.Adapter.TryShow())
            {
                InterstitialTierAnalytics.LogShowFail(cache.AdUnitId, cache.Tier, 0, "not_ready");
                ClearReady();
                LoadInterstitial();
                return false;
            }

            return true;
        }

        public void Destroy()
        {
            StopTimeout();
            _loadAdapter.Destroy();
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

            InterstitialTierAnalytics.LogLoadStart(adUnitId, tier);
            ScheduleTimeout(tier, adUnitId, generation);

            _loadAdapter.Load(adUnitId, generation, generation, _ =>
            {
                if (generation != _loadGeneration) return;
                OnTierLoadSuccess(adUnitId, tier, generation);
            }, error =>
            {
                if (generation != _loadGeneration) return;
                OnTierLoadFail(adUnitId, tier, error, generation);
            });
        }

        void ScheduleTimeout(AdTier tier, string adUnitId, int generation)
        {
            StopTimeout();
            var timeout = _config.GetTimeoutSeconds(tier);
            if (timeout <= 0f) return;

            _timeoutRoutine = _host.StartCoroutine(TimeoutCoroutine(tier, adUnitId, generation, timeout));
        }

        // Timeout: cancel in-flight load and advance ladder; bump generation to ignore late callbacks.
        IEnumerator TimeoutCoroutine(AdTier tier, string adUnitId, int generation, float timeoutSeconds)
        {
            yield return new WaitForSeconds(timeoutSeconds);
            if (generation != _loadGeneration || !_isLoading || _currentTier != tier)
                yield break;

            var elapsedMs = (long)(DateTime.UtcNow - _loadStartedUtc).TotalMilliseconds;
            InterstitialTierAnalytics.LogLoadTimeout(adUnitId, tier, elapsedMs);

            _loadGeneration++;
            _loadAdapter.Destroy();
            _loadAdapter = new AdMobInterstitialAdapter();
            StopTimeout();
            AdvanceAfterTierFailure(tier);
        }

        void OnTierLoadSuccess(string adUnitId, AdTier tier, int generation)
        {
            StopTimeout();
            var elapsedMs = (long)(DateTime.UtcNow - _loadStartedUtc).TotalMilliseconds;
            InterstitialTierAnalytics.LogLoadSuccess(adUnitId, tier, elapsedMs);

            ClearReady();
            _ready = new ReadyInterstitialCache
            {
                AdUnitId = adUnitId,
                Tier = tier,
                LoadTimeUtc = DateTime.UtcNow,
                Adapter = _loadAdapter
            };
            _loadAdapter = new AdMobInterstitialAdapter();

            _memory.RecordSuccess(tier);
            _isLoading = false;
            _onLoadedSuccess?.Invoke();
        }

        void OnTierLoadFail(string adUnitId, AdTier tier, LoadAdError error, int generation)
        {
            StopTimeout();
            var elapsedMs = (long)(DateTime.UtcNow - _loadStartedUtc).TotalMilliseconds;
            InterstitialTierAnalytics.LogLoadFail(
                adUnitId,
                tier,
                error?.GetCode() ?? 0,
                error?.GetMessage(),
                elapsedMs);
            _loadAdapter.Destroy();
            _loadAdapter = new AdMobInterstitialAdapter();
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
            InterstitialTierAnalytics.LogLoadFail("", AdTier.Fill, 0, reason, 0);
            _onLoadedFail?.Invoke();
        }

        void OnShowClosed()
        {
            var tier = _ready?.Tier ?? AdTier.Fill;
            var unit = _ready?.AdUnitId ?? "";
            ClearReady();
            _onClosed?.Invoke();
            LoadInterstitial();
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
