#if UNITY_AD_ADMOB
using System;
using GoogleMobileAds.Api;
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

            if (_ad != null)
            {
                _ad.Destroy();
                _ad = null;
            }

            _isLoading = true;
            var request = new AdRequest();
            InterstitialAd.Load(_adUnitId, request, (ad, error) =>
            {
                _isLoading = false;
                if (error != null)
                {
                    onFailed?.Invoke(error.GetMessage());
                    return;
                }
                _ad = ad;
                RegisterEvents();
                onLoaded?.Invoke();
            });
        }

        private void RegisterEvents()
        {
            if (_ad == null) return;
            // Interstitial is one-time-use. After close/fail, destroy and warm-load the next one.
            _ad.OnAdFullScreenContentOpened += () => _pendingOnShown?.Invoke();
            _ad.OnAdFullScreenContentClosed += () =>
            {
                _pendingOnClosed?.Invoke();
                DestroyAndWarmReload();
            };
            _ad.OnAdFullScreenContentFailed += err =>
            {
                _pendingOnFailed?.Invoke(err.GetMessage());
                DestroyAndWarmReload();
            };
        }

        public void Show(Action onShown = null, Action onClosed = null, Action<string> onFailed = null)
        {
            if (_ad == null || !_ad.CanShowAd())
            {
                onFailed?.Invoke("Interstitial not loaded");
                return;
            }

            _pendingOnShown = onShown;
            _pendingOnClosed = onClosed;
            _pendingOnFailed = onFailed;
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

            // Fire-and-forget preload for next impression.
            Load();
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

        public AdMobRewardedAd(string adUnitId) => _adUnitId = adUnitId;
        public bool IsLoaded => _ad != null && _ad.CanShowAd();

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            if (_isLoading)
                return;

            if (_ad != null)
            {
                _ad.Destroy();
                _ad = null;
            }

            _isLoading = true;
            var request = new AdRequest();
            RewardedAd.Load(_adUnitId, request, (ad, error) =>
            {
                _isLoading = false;
                if (error != null)
                {
                    onFailed?.Invoke(error.GetMessage());
                    return;
                }
                _ad = ad;
                RegisterEvents();
                onLoaded?.Invoke();
            });
        }

        public void Show(Action onRewardEarned = null, Action onClosed = null, Action<string> onFailed = null)
        {
            if (_ad == null || !_ad.CanShowAd())
            {
                onFailed?.Invoke("Rewarded ad not loaded");
                return;
            }

            _pendingOnRewardEarned = onRewardEarned;
            _pendingOnClosed = onClosed;
            _pendingOnFailed = onFailed;

            _ad.Show(_ => _pendingOnRewardEarned?.Invoke());
        }

        void RegisterEvents()
        {
            if (_ad == null) return;

            // Rewarded is one-time-use. After close/fail, destroy and warm-load the next one.
            _ad.OnAdFullScreenContentClosed += () =>
            {
                _pendingOnClosed?.Invoke();
                DestroyAndWarmReload();
            };
            _ad.OnAdFullScreenContentFailed += err =>
            {
                _pendingOnFailed?.Invoke(err.GetMessage());
                DestroyAndWarmReload();
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

            // Fire-and-forget preload for next impression.
            Load();
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

            _banner = new BannerView(_adUnitId.Trim(), AdSize.Banner, _position);
            _banner.OnBannerAdLoaded += () =>
            {
                _isAdLoaded = true;
                onLoaded?.Invoke();
            };
            _banner.OnBannerAdLoadFailed += err =>
            {
                _isAdLoaded = false;
                onFailed?.Invoke(err.GetMessage());
            };
            _banner.LoadAd(new AdRequest());
        }

        public void Show(Action onShown = null, Action<string> onFailed = null)
        {
            if (_banner == null)
            {
                onFailed?.Invoke("Banner not loaded");
                return;
            }
            _banner.Show();
            _isVisible = true;
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

            var request = new AdRequest();
            AppOpenAd.Load(_adUnitId, request, (ad, error) =>
            {
                if (error != null)
                {
                    onFailed?.Invoke(error.GetMessage());
                    return;
                }

                _ad = ad;
                onLoaded?.Invoke();
            });
        }

        public void Show(Action onShown = null, Action onClosed = null, Action<string> onFailed = null)
        {
            if (_ad == null || !_ad.CanShowAd())
            {
                onFailed?.Invoke("App open ad not loaded");
                return;
            }

            _ad.OnAdFullScreenContentOpened += () => onShown?.Invoke();
            _ad.OnAdFullScreenContentClosed += () =>
            {
                _ad.Destroy();
                _ad = null;
                onClosed?.Invoke();
            };
            _ad.OnAdFullScreenContentFailed += err =>
            {
                _ad.Destroy();
                _ad = null;
                onFailed?.Invoke(err.GetMessage());
            };
            _ad.Show();
        }
    }
}
#endif
