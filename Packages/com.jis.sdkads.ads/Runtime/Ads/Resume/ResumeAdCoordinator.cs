using System;
using System.Threading.Tasks;
using JisSDKAds.Ads.AppOpen;
using JisSDKAds.Common;
using UnityEngine;

namespace JisSDKAds.Ads.Resume
{
    /// <summary>
    /// Foreground resume policy: when enabled via RC, shows App Open or Interstitial after background.
    /// </summary>
    public sealed class ResumeAdCoordinator : MonoBehaviour
    {
        [SerializeField] float delayBeforeShowSec = 0.3f;
        [SerializeField] float loadingPanelDelayMs = 1000f;

        readonly ResumeAdPolicy _policy = new ResumeAdPolicy();

        JisAds _host;
        AppOpenAdService _appOpen;
        DateTime _pauseTime = DateTime.Now;
        DateTime _lastShowTime = DateTime.Now;
        bool _remoteConfigReady;

        public ResumeAdPolicy Policy => _policy;

        public void Bind(JisAds host, AppOpenAdService appOpen)
        {
            _host = host;
            _appOpen = appOpen;
        }

        public void ApplyRemoteConfig()
        {
            _policy.ApplyFromRemoteConfig();
            _remoteConfigReady = true;
        }

        public void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _pauseTime = DateTime.Now;
                return;
            }

            if (_remoteConfigReady)
                TryShowOnForeground();
        }

        public void TryShowOnForeground()
        {
            if (_host == null || !_host.IsReady || !_host.CanShowAds())
                return;

            if (!_policy.IsEnabled)
                return;

            if (_host.IsShowingAnyAd())
                return;

            var pauseDuration = (float)(DateTime.Now - _pauseTime).TotalSeconds;
            if (pauseDuration < _policy.MinPauseSeconds)
                return;

            var sinceLastShow = (float)(DateTime.Now - _lastShowTime).TotalSeconds;
            if (sinceLastShow < _policy.CappingSeconds)
                return;

            if (!IsSelectedFormatLoaded())
                return;

            _ = ShowWithLoadingAsync();
        }

        bool IsSelectedFormatLoaded()
        {
            return _policy.Format switch
            {
                ResumeAdFormat.Interstitial => _host.IsInterstitialAdLoaded(),
                _ => _appOpen != null && _appOpen.IsLoaded() && _appOpen.IsEnabledByRemoteConfig
            };
        }

        async Task ShowWithLoadingAsync()
        {
            if (loadingPanelDelayMs > 0f)
                await Task.Delay((int)loadingPanelDelayMs);

            if (delayBeforeShowSec > 0f)
                await Task.Delay(TimeSpan.FromSeconds(delayBeforeShowSec));

            if (!_host.CanShowAds() || _host.IsShowingAnyAd())
                return;

            _host.HideBannerForFullscreenAd("resume_ad");
            _host.SetAdsShowingState(true);

            switch (_policy.Format)
            {
                case ResumeAdFormat.Interstitial:
                    _host.ShowInterstitialForResume(
                        onClosed: OnResumeAdFinished,
                        onFailed: _ => OnResumeAdFinished());
                    break;
                default:
                    _appOpen.Show(
                        onClosed: OnResumeAdFinished,
                        onFailed: _ => OnResumeAdFinished());
                    break;
            }
        }

        void OnResumeAdFinished()
        {
            _lastShowTime = DateTime.Now;
            _host.SetAdsShowingState(false);
            _host.ScheduleBannerRestoreAfterFullscreenAd("resume_ad");
        }
    }
}
