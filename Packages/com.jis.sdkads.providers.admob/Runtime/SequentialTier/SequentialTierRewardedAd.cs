#if UNITY_AD_ADMOB
using System;
using GoogleMobileAds.Api;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Common;
using JisSDKAds.Core.Interfaces;
using UnityEngine;

namespace JisSDKAds.Providers.AdMob.SequentialTier
{
    internal sealed class SequentialTierRewardedAd : IRewardedAd
    {
        readonly SequentialTierLoader _loader;

        Action _pendingOnLoaded;
        Action<string> _pendingOnFailed;
        Action _pendingOnRewardGranted;
        Action _pendingOnClosed;
        Action<string> _pendingOnShowFailed;

        public SequentialTierRewardedAd(MonoBehaviour host, SequentialTierConfig config)
        {
            _loader = new SequentialTierLoader(
                host,
                "reward",
                "rewarded",
                AdLoadFormat.Rewarded,
                config,
                () => new AdMobRewardedAdapter());
            RefreshLoaderCallbacks();
        }

        public bool IsLoaded => _loader.IsReady;

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            FlushSupersededLoadCallbacks();
            _pendingOnLoaded = onLoaded;
            _pendingOnFailed = onFailed;
            RefreshLoaderCallbacks();
            DebugAds.LogAdEvent("Rewarded", "load_start", null, "provider=ADMOB tiered");
            _loader.Load();
        }

        public void Show(Action onRewardGranted = null, Action onClosed = null, Action<string> onFailed = null)
        {
            _pendingOnRewardGranted = onRewardGranted;
            _pendingOnClosed = onClosed;
            _pendingOnShowFailed = onFailed;
            RefreshLoaderCallbacks();

            var unitId = _loader.ReadyCache?.AdUnitId;
            DebugAds.LogAdEvent("Rewarded", "show_call", unitId, "provider=ADMOB tiered");
            if (_loader.Show())
                return;

            DebugAds.LogAdEvent("Rewarded", "show_blocked_not_loaded", unitId, "provider=ADMOB tiered");
            var cb = _pendingOnShowFailed;
            _pendingOnShowFailed = null;
            cb?.Invoke("Rewarded not ready");
        }

        public void Destroy() => _loader.Destroy();

        void RefreshLoaderCallbacks()
        {
            _loader.SetCallbacks(
                OnLoaderLoadSuccess,
                OnLoaderLoadFailed,
                new SequentialTierShowHooks
                {
                    onOpened = OnShowOpened,
                    onClosed = OnShowClosed,
                    onRewardGranted = OnRewardGranted,
                    onFailed = OnShowFailed,
                    onPaid = OnPaid
                });
        }

        void OnLoaderLoadSuccess()
        {
            var cb = _pendingOnLoaded;
            ClearPendingLoadCallbacks();
            DebugAds.LogAdEvent("Rewarded", "load_success", _loader.ReadyCache?.AdUnitId, "provider=ADMOB tiered");
            cb?.Invoke();
        }

        void OnLoaderLoadFailed()
        {
            var cb = _pendingOnFailed;
            ClearPendingLoadCallbacks();
            DebugAds.LogAdEvent("Rewarded", "load_fail", null, "provider=ADMOB tiered", "Sequential tier ladder failed");
            cb?.Invoke("Sequential tier ladder failed");
        }

        void FlushSupersededLoadCallbacks()
        {
            if (_pendingOnLoaded == null && _pendingOnFailed == null)
                return;

            var cb = _pendingOnFailed;
            ClearPendingLoadCallbacks();
            cb?.Invoke("Rewarded load superseded by a newer request");
        }

        void ClearPendingLoadCallbacks()
        {
            _pendingOnLoaded = null;
            _pendingOnFailed = null;
        }

        void OnShowOpened()
        {
            // Impression opened; reward is delivered via onRewardGranted from the adapter.
            DebugAds.LogAdEvent("Rewarded", "show_opened", _loader.ReadyCache?.AdUnitId, "provider=ADMOB tiered");
        }

        void OnRewardGranted()
        {
            var cb = _pendingOnRewardGranted;
            _pendingOnRewardGranted = null;
            DebugAds.LogAdEvent("Rewarded", "reward_granted", _loader.ReadyCache?.AdUnitId, "provider=ADMOB tiered");
            cb?.Invoke();
        }

        void OnShowClosed()
        {
            var cb = _pendingOnClosed;
            _pendingOnClosed = null;
            DebugAds.LogAdEvent("Rewarded", "show_closed", _loader.ReadyCache?.AdUnitId, "provider=ADMOB tiered");
            cb?.Invoke();
            // Never start the next load right after close — respect the minimum reload gap.
            AdReloadScheduler.Schedule("admob_tier_rew", () =>
            {
                if (!_loader.IsReady)
                    _loader.Load(forceReload: true);
            });
        }

        void OnShowFailed(SequentialTierShowError? error)
        {
            var cb = _pendingOnShowFailed;
            _pendingOnShowFailed = null;
            var message = string.IsNullOrEmpty(error?.Message) ? "show_failed" : error.Value.Message;
            DebugAds.LogAdEvent("Rewarded", "show_failed", _loader.ReadyCache?.AdUnitId, "provider=ADMOB tiered", message);
            cb?.Invoke(message);
        }

        void OnPaid(SequentialTierPaidEvent paid)
        {
            var cache = _loader.ReadyCache;
            if (cache != null)
                SequentialTierAnalytics.LogPaid(
                    "rewarded",
                    cache.AdUnitId,
                    cache.Tier,
                    paid.Revenue,
                    paid.Currency,
                    paid.Precision);

            AdMobCorePaidTracker.Track("REWARDED", paid);
        }
    }
}
#endif
