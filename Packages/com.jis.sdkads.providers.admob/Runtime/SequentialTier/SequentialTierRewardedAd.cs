#if UNITY_AD_ADMOB
using System;
using GoogleMobileAds.Api;
using JisSDKAds.Ads.SequentialTier;
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
                config,
                () => new AdMobRewardedAdapter());
            RefreshLoaderCallbacks();
        }

        public bool IsLoaded => _loader.IsReady;

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            _pendingOnLoaded = onLoaded;
            _pendingOnFailed = onFailed;
            RefreshLoaderCallbacks();
            _loader.Load();
        }

        public void Show(Action onRewardGranted = null, Action onClosed = null, Action<string> onFailed = null)
        {
            _pendingOnRewardGranted = onRewardGranted;
            _pendingOnClosed = onClosed;
            _pendingOnShowFailed = onFailed;
            RefreshLoaderCallbacks();

            if (_loader.Show())
                return;

            _pendingOnShowFailed?.Invoke("Rewarded not ready");
            _loader.Load();
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
                    onPaid = null
                });
        }

        void OnLoaderLoadSuccess()
        {
            var cb = _pendingOnLoaded;
            _pendingOnLoaded = null;
            cb?.Invoke();
        }

        void OnLoaderLoadFailed()
        {
            var cb = _pendingOnFailed;
            _pendingOnFailed = null;
            cb?.Invoke("Sequential tier ladder failed");
        }

        void OnShowOpened()
        {
            // Impression opened; reward is delivered via onRewardGranted from the adapter.
        }

        void OnRewardGranted()
        {
            var cb = _pendingOnRewardGranted;
            _pendingOnRewardGranted = null;
            cb?.Invoke();
        }

        void OnShowClosed()
        {
            var cb = _pendingOnClosed;
            _pendingOnClosed = null;
            cb?.Invoke();
            _loader.Load(forceReload: true);
        }

        void OnShowFailed(AdError error)
        {
            var cb = _pendingOnShowFailed;
            _pendingOnShowFailed = null;
            cb?.Invoke(error?.GetMessage() ?? "show_failed");
            _loader.Load(forceReload: true);
        }
    }
}
#endif
