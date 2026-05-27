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

        public AdProviderId ProviderId => AdProviderId.AdMob;
        public IAdService CreateProvider() => new AdMobProvider(appId, interstitialAdUnitId, rewardedAdUnitId, bannerAdUnitId, appOpenAdUnitId);
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

        public AdMobProvider(string appId, string interstitialAdUnitId, string rewardedAdUnitId, string bannerAdUnitId, string appOpenAdUnitId = null)
        {
            _appId = appId;
            _interstitialAdUnitId = interstitialAdUnitId;
            _rewardedAdUnitId = rewardedAdUnitId;
            _bannerAdUnitId = bannerAdUnitId;

            Interstitial = new AdMobInterstitialAd(_interstitialAdUnitId);
            Rewarded = new AdMobRewardedAd(_rewardedAdUnitId);
            Banner = new AdMobBannerAd(_bannerAdUnitId);
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

        public AdMobInterstitialAd(string adUnitId) => _adUnitId = adUnitId;
        public bool IsLoaded => _ad != null && _ad.CanShowAd();

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            if (_ad != null)
            {
                _ad.Destroy();
                _ad = null;
            }

            var request = new AdRequest();
            InterstitialAd.Load(_adUnitId, request, (ad, error) =>
            {
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
            _ad.OnAdFullScreenContentClosed += () => { };
            _ad.OnAdFullScreenContentFailed += _ => { };
        }

        public void Show(Action onShown = null, Action onClosed = null, Action<string> onFailed = null)
        {
            if (_ad == null || !_ad.CanShowAd())
            {
                onFailed?.Invoke("Interstitial not loaded");
                return;
            }

            _ad.OnAdFullScreenContentOpened += () => onShown?.Invoke();
            _ad.OnAdFullScreenContentClosed += () => onClosed?.Invoke();
            _ad.OnAdFullScreenContentFailed += err => onFailed?.Invoke(err.GetMessage());
            _ad.Show();
        }
    }

    internal class AdMobRewardedAd : IRewardedAd
    {
        private readonly string _adUnitId;
        private RewardedAd _ad;

        public AdMobRewardedAd(string adUnitId) => _adUnitId = adUnitId;
        public bool IsLoaded => _ad != null && _ad.CanShowAd();

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            if (_ad != null)
            {
                _ad.Destroy();
                _ad = null;
            }

            var request = new AdRequest();
            RewardedAd.Load(_adUnitId, request, (ad, error) =>
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

        public void Show(Action onRewardEarned = null, Action onClosed = null, Action<string> onFailed = null)
        {
            if (_ad == null || !_ad.CanShowAd())
            {
                onFailed?.Invoke("Rewarded ad not loaded");
                return;
            }

            _ad.OnAdFullScreenContentOpened += () => { };
            _ad.OnAdFullScreenContentClosed += () => onClosed?.Invoke();
            _ad.OnAdFullScreenContentFailed += err => onFailed?.Invoke(err.GetMessage());
            _ad.OnAdPaid += _ => onRewardEarned?.Invoke();
            _ad.Show(reward => onRewardEarned?.Invoke());
        }
    }

    internal class AdMobBannerAd : IBannerAd
    {
        private readonly string _adUnitId;
        private BannerView _banner;
        private bool _isVisible;

        public AdMobBannerAd(string adUnitId) => _adUnitId = adUnitId;
        public bool IsLoaded => _banner != null;
        public bool IsVisible => _isVisible;

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            if (_banner != null)
            {
                _banner.Destroy();
                _banner = null;
            }

            _banner = new BannerView(_adUnitId, AdSize.Banner, AdPosition.Bottom);
            _banner.OnBannerAdLoaded += () => onLoaded?.Invoke();
            _banner.OnBannerAdLoadFailed += err => onFailed?.Invoke(err.GetMessage());
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
