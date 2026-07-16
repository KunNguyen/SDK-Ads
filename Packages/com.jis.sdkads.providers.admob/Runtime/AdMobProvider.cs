#if UNITY_AD_ADMOB
using System;
using GoogleMobileAds.Api;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Common;
using JisSDKAds.Core.Interfaces;
using JisSDKAds.Core.Models;
using UnityEngine;

namespace JisSDKAds.Providers.AdMob
{
    [CreateAssetMenu(fileName = "AdMobConfig", menuName = "JisSDKAds/Providers/AdMob Config", order = 2)]
    public class AdMobConfig : ScriptableObject, IAdProviderConfig
    {
        public string appId;
        public string interstitialAdUnitId;
        public string rewardedAdUnitId;
        public string bannerAdUnitId;
        public string appOpenAdUnitId;
        public AdPosition bannerPosition = AdPosition.Bottom;

        public AdProviderId ProviderId => AdProviderId.AdMob;
        public IAdService CreateProvider() =>
            new AdMobProvider(appId, interstitialAdUnitId, rewardedAdUnitId, bannerAdUnitId, appOpenAdUnitId, bannerPosition);
    }

    /// <summary>
    /// Google AdMob implementation of IAdService.
    /// </summary>
    public class AdMobProvider : IAdService
    {
        private readonly string _appId;
        private readonly string _interstitialAdUnitId;
        private readonly string _rewardedAdUnitId;
        private readonly string _bannerAdUnitId;
        private bool _isInitialized;

        public string ProviderId => "AdMob";
        public bool IsInitialized => _isInitialized;

        public IInterstitialAd Interstitial { get; }
        public IRewardedAd Rewarded { get; }
        public IBannerAd Banner { get; }
        public IAppOpenAd AppOpen { get; }

        public AdMobProvider(
            string appId,
            string interstitialAdUnitId,
            string rewardedAdUnitId,
            string bannerAdUnitId,
            string appOpenAdUnitId = null,
            AdPosition bannerPosition = AdPosition.Bottom)
        {
            _appId = appId;
            _interstitialAdUnitId = interstitialAdUnitId;
            _rewardedAdUnitId = rewardedAdUnitId;
            _bannerAdUnitId = bannerAdUnitId;

            Interstitial = new AdMobInterstitialAd(_interstitialAdUnitId);
            Rewarded = new AdMobRewardedAd(_rewardedAdUnitId);
            Banner = new AdMobBannerAd(_bannerAdUnitId, bannerPosition);
            AppOpen = string.IsNullOrEmpty(appOpenAdUnitId)
                ? NullAppOpenAd.Instance
                : new AdMobAppOpenAd(appOpenAdUnitId);
        }

        public void Initialize(Action onSuccess, Action<string> onFailure)
        {
            if (_isInitialized)
            {
                onSuccess?.Invoke();
                return;
            }

            AdMobMobileAdsInitializer.EnsureInitialized(
                requestConsent: false,
                onComplete: success =>
                {
                    if (!success)
                    {
                        onFailure?.Invoke("AdMob MobileAds.Initialize failed");
                        return;
                    }

                    _isInitialized = true;
                    onSuccess?.Invoke();
                });
        }

        public void SetConsent(bool hasConsent)
        {
            // Use UMP (User Messaging Platform) for consent in production
            // This is a simplified placeholder
        }
    }

    internal class AdMobInterstitialAd : IInterstitialAd
    {
        private readonly string _adUnitId;
        private InterstitialAd _ad;
        private bool _isLoading;

        Action _pendingOnShown;
        Action _pendingOnClosed;
        Action<string> _pendingOnFailed;

        public AdMobInterstitialAd(string adUnitId) => _adUnitId = adUnitId;
        public bool IsLoaded => _ad != null && _ad.CanShowAd();

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            if (_isLoading)
                return;

            AdLoadPipeline.RunInterstitialLoad(() => RunLoad(onLoaded, onFailed));
        }

        void RunLoad(Action onLoaded, Action<string> onFailed)
        {
            if (_isLoading)
                return;

            if (_ad != null)
            {
                _ad.Destroy();
                _ad = null;
            }

            _isLoading = true;
            DebugAds.Log($"[AdMob][Interstitial] load_start adUnitId={_adUnitId}");
            var request = new AdRequest();
            InterstitialAd.Load(_adUnitId, request, (ad, error) =>
            {
                var message = error?.GetMessage();
                AdMobMainThread.Run(() =>
                {
                    _isLoading = false;
                    AdLoadPipeline.NotifyInterstitialFinished();
                    if (error != null)
                    {
                        DebugAds.LogWarning($"[AdMob][Interstitial] load_fail adUnitId={_adUnitId} error={message}");
                        onFailed?.Invoke(message);
                        return;
                    }
                    _ad = ad;
                    RegisterEvents();
                    DebugAds.Log($"[AdMob][Interstitial] load_success adUnitId={_adUnitId}");
                    onLoaded?.Invoke();
                });
            });
        }

        private void RegisterEvents()
        {
            if (_ad == null) return;
            // Interstitial is one-time-use. After close/fail, destroy and warm-load the next one.
            _ad.OnAdFullScreenContentOpened += () => AdMobMainThread.Run(() =>
            {
                DebugAds.Log($"[AdMob][Interstitial] show_opened adUnitId={_adUnitId}");
                _pendingOnShown?.Invoke();
            });
            _ad.OnAdFullScreenContentClosed += () => AdMobMainThread.Run(() =>
            {
                DebugAds.Log($"[AdMob][Interstitial] show_closed adUnitId={_adUnitId}");
                _pendingOnClosed?.Invoke();
                DestroyAndWarmReload();
            });
            _ad.OnAdFullScreenContentFailed += err =>
            {
                var message = err.GetMessage();
                AdMobMainThread.Run(() =>
                {
                    DebugAds.LogWarning($"[AdMob][Interstitial] show_failed adUnitId={_adUnitId} error={message}");
                    _pendingOnFailed?.Invoke(message);
                    DestroyAndWarmReload();
                });
            };
        }

        public void Show(Action onShown = null, Action onClosed = null, Action<string> onFailed = null)
        {
            if (_ad == null || !_ad.CanShowAd())
            {
                DebugAds.LogWarning($"[AdMob][Interstitial] show_blocked_not_loaded adUnitId={_adUnitId}");
                onFailed?.Invoke("Interstitial not loaded");
                return;
            }

            _pendingOnShown = onShown;
            _pendingOnClosed = onClosed;
            _pendingOnFailed = onFailed;
            DebugAds.Log($"[AdMob][Interstitial] show_call adUnitId={_adUnitId}");
            _ad.Show();
        }

        void DestroyAndWarmReload()
        {
            try
            {
                _ad?.Destroy();
            }
            catch
            {
                // best effort
            }
            _ad = null;

            // Preload the next impression, but never sooner than the minimum reload gap after close/fail.
            AdReloadScheduler.Schedule("admob_int_" + _adUnitId, () => Load());
        }
    }

    internal class AdMobRewardedAd : IRewardedAd
    {
        private readonly string _adUnitId;
        private RewardedAd _ad;
        private bool _isLoading;

        Action _pendingOnRewardEarned;
        Action _pendingOnClosed;
        Action<string> _pendingOnFailed;
        RewardedShowCompletionTracker _showCompletion = new RewardedShowCompletionTracker();

        public AdMobRewardedAd(string adUnitId) => _adUnitId = adUnitId;
        public bool IsLoaded => _ad != null && _ad.CanShowAd();

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            if (_isLoading)
                return;

            AdLoadPipeline.RunRewardedLoad(() => RunLoad(onLoaded, onFailed));
        }

        void RunLoad(Action onLoaded, Action<string> onFailed)
        {
            if (_isLoading)
                return;

            if (_ad != null)
            {
                _ad.Destroy();
                _ad = null;
            }

            _isLoading = true;
            DebugAds.Log($"[AdMob][Rewarded] load_start adUnitId={_adUnitId}");
            var request = new AdRequest();
            RewardedAd.Load(_adUnitId, request, (ad, error) =>
            {
                var message = error?.GetMessage();
                AdMobMainThread.Run(() =>
                {
                    _isLoading = false;
                    AdLoadPipeline.NotifyRewardedFinished();
                    if (error != null)
                    {
                        DebugAds.LogWarning($"[AdMob][Rewarded] load_fail adUnitId={_adUnitId} error={message}");
                        onFailed?.Invoke(message);
                        return;
                    }
                    _ad = ad;
                    RegisterEvents();
                    DebugAds.Log($"[AdMob][Rewarded] load_success adUnitId={_adUnitId}");
                    onLoaded?.Invoke();
                });
            });
        }

        public void Show(Action onRewardEarned = null, Action onClosed = null, Action<string> onFailed = null)
        {
            if (_ad == null || !_ad.CanShowAd())
            {
                DebugAds.LogWarning($"[AdMob][Rewarded] show_blocked_not_loaded adUnitId={_adUnitId}");
                onFailed?.Invoke("Rewarded ad not loaded");
                return;
            }

            _pendingOnRewardEarned = onRewardEarned;
            _pendingOnClosed = onClosed;
            _pendingOnFailed = onFailed;
            _showCompletion.Reset();

            DebugAds.Log($"[AdMob][Rewarded] show_call adUnitId={_adUnitId}");
            _ad.Show(_ => AdMobMainThread.Run(() => _showCompletion.NotifyRewardGranted(() =>
            {
                DebugAds.Log($"[AdMob][Rewarded] reward_granted adUnitId={_adUnitId}");
                _pendingOnRewardEarned?.Invoke();
            })));
        }

        void RegisterEvents()
        {
            if (_ad == null) return;

            // Rewarded is one-time-use. After close/fail, destroy and warm-load the next one.
            _ad.OnAdFullScreenContentOpened += () => AdMobMainThread.Run(() =>
            {
                DebugAds.Log($"[AdMob][Rewarded] show_opened adUnitId={_adUnitId}");
            });
            _ad.OnAdFullScreenContentClosed += () => AdMobMainThread.Run(() =>
            {
                DebugAds.Log($"[AdMob][Rewarded] show_closed adUnitId={_adUnitId}");
                _showCompletion.NotifyFullscreenClosed(() =>
                {
                    if (!_showCompletion.RewardGranted)
                        DebugAds.Log($"[AdMob][Rewarded] show_closed_without_reward adUnitId={_adUnitId}");

                    var onClosed = _pendingOnClosed;
                    _pendingOnClosed = null;
                    _pendingOnRewardEarned = null;
                    _pendingOnFailed = null;
                    onClosed?.Invoke();
                    DestroyAndWarmReload();
                });
            });
            _ad.OnAdFullScreenContentFailed += err =>
            {
                var message = err.GetMessage();
                AdMobMainThread.Run(() =>
                {
                    DebugAds.LogWarning($"[AdMob][Rewarded] show_failed adUnitId={_adUnitId} error={message}");
                    _showCompletion.Reset();
                    _pendingOnFailed?.Invoke(message);
                    _pendingOnFailed = null;
                    _pendingOnRewardEarned = null;
                    _pendingOnClosed = null;
                    DestroyAndWarmReload();
                });
            };
        }

        void DestroyAndWarmReload()
        {
            try
            {
                _ad?.Destroy();
            }
            catch
            {
                // best effort
            }
            _ad = null;

            // Preload the next impression, but never sooner than the minimum reload gap after close/fail.
            AdReloadScheduler.Schedule("admob_rew_" + _adUnitId, () => Load());
        }
    }

    internal class AdMobBannerAd : IBannerAd
    {
        private readonly string _adUnitId;
        private readonly AdPosition _position;
        private BannerView _banner;
        private bool _isVisible;
        private bool _isAdLoaded;

        public AdMobBannerAd(string adUnitId, AdPosition position = AdPosition.Bottom)
        {
            _adUnitId = adUnitId;
            _position = position;
        }

        public bool IsLoaded => _isAdLoaded && _banner != null;
        public bool IsVisible => _isVisible;

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            if (string.IsNullOrWhiteSpace(_adUnitId))
            {
                onFailed?.Invoke("Banner ad unit id is empty");
                return;
            }

            if (_banner != null)
            {
                _banner.Destroy();
                _banner = null;
            }

            _isAdLoaded = false;
            _isVisible = false;

            DebugAds.Log($"[AdMob][Banner] load_start adUnitId={_adUnitId}");
            _banner = new BannerView(_adUnitId.Trim(), AdSize.Banner, _position);
            _banner.OnBannerAdLoaded += () => AdMobMainThread.Run(() =>
            {
                _isAdLoaded = true;
                DebugAds.Log($"[AdMob][Banner] load_success adUnitId={_adUnitId}");
                onLoaded?.Invoke();
            });
            _banner.OnBannerAdLoadFailed += err =>
            {
                var message = err.GetMessage();
                AdMobMainThread.Run(() =>
                {
                    _isAdLoaded = false;
                    DebugAds.LogWarning($"[AdMob][Banner] load_fail adUnitId={_adUnitId} error={message}");
                    onFailed?.Invoke(message);
                });
            };
            _banner.LoadAd(new AdRequest());
        }

        public void Show(Action onShown = null, Action<string> onFailed = null)
        {
            if (_banner == null || !_isAdLoaded)
            {
                DebugAds.LogWarning($"[AdMob][Banner] show_blocked_not_loaded adUnitId={_adUnitId}");
                onFailed?.Invoke("Banner not loaded");
                return;
            }
            DebugAds.Log($"[AdMob][Banner] show_call adUnitId={_adUnitId}");
            _banner.Show();
            _isVisible = true;
            DebugAds.Log($"[AdMob][Banner] show_shown adUnitId={_adUnitId}");
            onShown?.Invoke();
        }

        public void Hide()
        {
            _banner?.Hide();
            _isVisible = false;
        }

        public void Destroy()
        {
            _banner?.Destroy();
            _banner = null;
            _isVisible = false;
        }
    }

    internal class AdMobAppOpenAd : IAppOpenAd
    {
        private readonly string _adUnitId;
        private AppOpenAd _ad;

        public AdMobAppOpenAd(string adUnitId) => _adUnitId = adUnitId;
        public bool IsLoaded => _ad != null && _ad.CanShowAd();

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            if (_ad != null)
            {
                _ad.Destroy();
                _ad = null;
            }

            DebugAds.Log($"[AdMob][AppOpen] load_start adUnitId={_adUnitId}");
            var request = new AdRequest();
            AppOpenAd.Load(_adUnitId, request, (ad, error) =>
            {
                var message = error?.GetMessage();
                AdMobMainThread.Run(() =>
                {
                    if (error != null)
                    {
                        DebugAds.LogWarning($"[AdMob][AppOpen] load_fail adUnitId={_adUnitId} error={message}");
                        onFailed?.Invoke(message);
                        return;
                    }

                    _ad = ad;
                    DebugAds.Log($"[AdMob][AppOpen] load_success adUnitId={_adUnitId}");
                    onLoaded?.Invoke();
                });
            });
        }

        public void Show(Action onShown = null, Action onClosed = null, Action<string> onFailed = null)
        {
            if (_ad == null || !_ad.CanShowAd())
            {
                DebugAds.LogWarning($"[AdMob][AppOpen] show_blocked_not_loaded adUnitId={_adUnitId}");
                onFailed?.Invoke("App open ad not loaded");
                return;
            }

            DebugAds.Log($"[AdMob][AppOpen] show_call adUnitId={_adUnitId}");
            _ad.OnAdFullScreenContentOpened += () => AdMobMainThread.Run(() =>
            {
                DebugAds.Log($"[AdMob][AppOpen] show_opened adUnitId={_adUnitId}");
                onShown?.Invoke();
            });
            _ad.OnAdFullScreenContentClosed += () => AdMobMainThread.Run(() =>
            {
                _ad?.Destroy();
                _ad = null;
                DebugAds.Log($"[AdMob][AppOpen] show_closed adUnitId={_adUnitId}");
                onClosed?.Invoke();
            });
            _ad.OnAdFullScreenContentFailed += err =>
            {
                var message = err.GetMessage();
                AdMobMainThread.Run(() =>
                {
                    _ad?.Destroy();
                    _ad = null;
                    DebugAds.LogWarning($"[AdMob][AppOpen] show_failed adUnitId={_adUnitId} error={message}");
                    onFailed?.Invoke(message);
                });
            };
            _ad.Show();
        }
    }
}
#endif
