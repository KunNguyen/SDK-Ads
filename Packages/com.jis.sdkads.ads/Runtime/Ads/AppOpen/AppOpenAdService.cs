using System;
using System.Collections;
using JisSDKAds.Common;
using JisSDKAds.Core.Interfaces;
using JisSDKAds.Firebase;
using UnityEngine;

namespace JisSDKAds.Ads.AppOpen
{
    /// <summary>
    /// App Open inventory and cold-start rules (RC: show_open_ads, show_open_ads_first_open).
    /// Resume-on-foreground is handled by <see cref="Resume.ResumeAdCoordinator"/>.
    /// </summary>
    public sealed class AppOpenAdService
    {
        readonly JisAds _host;
        readonly MonoBehaviour _coroutineRunner;

        bool _isActiveByRemoteConfig = true;
        bool _showOnFirstOpen = true;
        bool _isFirstOpen = true;
        bool _showOnColdStart;
        float _firstShowDelayMs = 600f;
        float _firstShowWaitLoadTimeoutSec = 2.5f;
        float _minIntervalBetweenShowsSec = 20f;
        float _lastShowRealtime = -9999f;
        bool _remoteConfigReady;

        public bool IsEnabledByRemoteConfig => _isActiveByRemoteConfig;
        public bool RemoteConfigReady => _remoteConfigReady;

        public AppOpenAdService(JisAds host, MonoBehaviour coroutineRunner)
        {
            _host = host;
            _coroutineRunner = coroutineRunner;
        }

        public void ApplyRemoteConfig()
        {
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
                return;

            _isActiveByRemoteConfig = FirebaseManager.Instance.GetConfigBool(Keys.key_remote_aoa_active);
            _showOnFirstOpen = FirebaseManager.Instance.GetConfigBool(Keys.key_remote_aoa_show_first_time_active);
            _remoteConfigReady = true;

            DebugAds.Log($"[AppOpen] RC active={_isActiveByRemoteConfig} firstOpen={_showOnFirstOpen}");
        }

        public void ConfigureColdStart(bool showOnColdStart, float firstShowDelayMs, float firstShowWaitLoadTimeoutSec, float minIntervalBetweenShowsSec)
        {
            _showOnColdStart = showOnColdStart;
            _firstShowDelayMs = firstShowDelayMs;
            _firstShowWaitLoadTimeoutSec = firstShowWaitLoadTimeoutSec;
            _minIntervalBetweenShowsSec = minIntervalBetweenShowsSec;
        }

        public void BeginAfterSdkReady()
        {
            ApplyRemoteConfig();
            Preload();

            if (_showOnColdStart && _isFirstOpen && _showOnFirstOpen)
                _coroutineRunner.StartCoroutine(CoTryShowFirstOpenSafely());
        }

        public void Preload()
        {
            if (!CanOperate() || !HasAppOpenSupport())
                return;

            GetAppOpen()?.Load(
                onLoaded: () => DebugAds.Log("[AppOpen] Preload success"),
                onFailed: err => DebugAds.LogWarning($"[AppOpen] Preload failed: {err}"));
        }

        public bool IsLoaded() => HasAppOpenSupport() && GetAppOpen() is { IsLoaded: true };

        public bool IsReadyToShow()
        {
#if UNITY_EDITOR
            return false;
#endif
            return CanOperate()
                   && _isActiveByRemoteConfig
                   && IsLoaded()
                   && Application.isFocused
                   && !_host.IsShowingAnyAd();
        }

        public void Show(Action onClosed = null, Action<string> onFailed = null)
        {
            if (Time.realtimeSinceStartup - _lastShowRealtime < _minIntervalBetweenShowsSec)
            {
                onFailed?.Invoke("App open debounced");
                return;
            }

            if (!IsReadyToShow())
            {
                onFailed?.Invoke("App open not ready");
                return;
            }

            _host.HideBannerForFullscreenAd("app_open");
            _host.SetAdsShowingState(true);
            _host.Core.ShowAppOpen(
                onClosed: () =>
                {
                    _lastShowRealtime = Time.realtimeSinceStartup;
                    _isFirstOpen = false;
                    _host.SetAdsShowingState(false);
                    Preload();
                    onClosed?.Invoke();
                    _host.ScheduleBannerRestoreAfterFullscreenAd("app_open");
                },
                onFailed: err =>
                {
                    _host.SetAdsShowingState(false);
                    onFailed?.Invoke(err);
                    _host.ScheduleBannerRestoreAfterFullscreenAd("app_open");
                });
        }

        IEnumerator CoTryShowFirstOpenSafely()
        {
            yield return null;

            if (_firstShowDelayMs > 0f)
                yield return new WaitForSecondsRealtime(_firstShowDelayMs / 1000f);

            if (!_isActiveByRemoteConfig || !_host.CanShowAds())
                yield break;

            if (!Application.isFocused || _host.IsShowingAnyAd())
                yield break;

            var waited = 0f;
            while (waited < _firstShowWaitLoadTimeoutSec && !IsLoaded())
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (IsLoaded())
                Show();
        }

        bool CanOperate() => _host.IsReady && _host.CanShowAds() && HasAppOpenSupport();

        bool HasAppOpenSupport() =>
            _host.Core != null
            && _host.Core.IsInitialized
            && _host.Core.PrimaryProvider?.AppOpen is not NullAppOpenAd;

        IAppOpenAd GetAppOpen() => _host.Core?.PrimaryProvider?.AppOpen;
    }
}
