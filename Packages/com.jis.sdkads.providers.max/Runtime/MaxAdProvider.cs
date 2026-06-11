#if UNITY_AD_MAX
using System;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Core.Interfaces;
using JisSDKAds.Core.Models;
using UnityEngine;

namespace JisSDKAds.Providers.Max
{
    [CreateAssetMenu(fileName = "MaxAdConfig", menuName = "JisSDKAds/Providers/Max Config", order = 1)]
    public class MaxAdConfig : ScriptableObject, IAdProviderConfig
    {
        public string sdkKey;
        public string interstitialAdUnitId;
        public string rewardedAdUnitId;
        public string bannerAdUnitId;
        public string appOpenAdUnitId;
        public int bannerPosition = (int)MaxSdkBase.BannerPosition.BottomCenter;

        public AdProviderId ProviderId => AdProviderId.Max;
        public IAdService CreateProvider() => new MaxAdProvider(
            sdkKey,
            interstitialAdUnitId,
            rewardedAdUnitId,
            bannerAdUnitId,
            appOpenAdUnitId,
            (MaxSdkBase.BannerPosition)bannerPosition);
    }

    /// <summary>
    /// AppLovin MAX implementation of IAdService.
    /// </summary>
    public class MaxAdProvider : IAdService
    {
        private readonly string _sdkKey;
        private readonly string _interstitialAdUnitId;
        private readonly string _rewardedAdUnitId;
        private readonly string _bannerAdUnitId;
        private readonly string _appOpenAdUnitId;
        private readonly MaxSdkBase.BannerPosition _bannerPosition;
        private bool _isInitialized;

        public string ProviderId => "Max";
        public bool IsInitialized => _isInitialized;

        public IInterstitialAd Interstitial { get; }
        public IRewardedAd Rewarded { get; }
        public IBannerAd Banner { get; }
        public IAppOpenAd AppOpen { get; }

        public MaxAdProvider(
            string sdkKey,
            string interstitialAdUnitId,
            string rewardedAdUnitId,
            string bannerAdUnitId,
            string appOpenAdUnitId = null,
            MaxSdkBase.BannerPosition bannerPosition = MaxSdkBase.BannerPosition.BottomCenter)
        {
            _sdkKey = sdkKey;
            _interstitialAdUnitId = interstitialAdUnitId;
            _rewardedAdUnitId = rewardedAdUnitId;
            _bannerAdUnitId = bannerAdUnitId;
            _appOpenAdUnitId = appOpenAdUnitId;
            _bannerPosition = bannerPosition;

            Interstitial = new MaxInterstitialAd(_interstitialAdUnitId);
            Rewarded = new MaxRewardedAd(_rewardedAdUnitId);
            Banner = new MaxBannerAd(_bannerAdUnitId, _bannerPosition);
            AppOpen = string.IsNullOrEmpty(_appOpenAdUnitId)
                ? NullAppOpenAd.Instance
                : new MaxAppOpenAd(_appOpenAdUnitId);
        }

        public void Initialize(Action onSuccess, Action<string> onFailure)
        {
            if (_isInitialized)
            {
                onSuccess?.Invoke();
                return;
            }

            MaxSdk.SetSdkKey(_sdkKey);
            MaxSdk.SetHasUserConsent(true);
            MaxSdk.SetDoNotSell(false);

            MaxSdkCallbacks.OnSdkInitializedEvent += _ =>
            {
                _isInitialized = true;
                onSuccess?.Invoke();
            };

            MaxSdk.InitializeSdk();
        }

        public void SetConsent(bool hasConsent)
        {
            MaxSdk.SetHasUserConsent(hasConsent);
        }
    }

    internal class MaxInterstitialAd : IInterstitialAd
    {
        private readonly string _adUnitId;
        private Action _pendingOnLoaded;
        private Action<string> _pendingOnLoadFailed;
        private Action _pendingOnShown;
        private Action _pendingOnClosed;
        private Action<string> _pendingOnShowFailed;

        public MaxInterstitialAd(string adUnitId) => _adUnitId = adUnitId;
        public bool IsLoaded => MaxSdk.IsInterstitialReady(_adUnitId);

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            if (string.IsNullOrEmpty(_adUnitId))
            {
                onFailed?.Invoke("Interstitial ad unit id is empty");
                return;
            }

            _pendingOnLoaded = onLoaded;
            _pendingOnLoadFailed = onFailed;

            // Idempotent: never stack handlers when Load is called repeatedly before a result.
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnLoadResult;
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnLoadResult;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnLoadFailed;
            MaxSdk.LoadInterstitial(_adUnitId);
        }

        void OnLoadResult(string id, MaxSdkBase.AdInfo info)
        {
            if (id != _adUnitId) return;
            UnsubscribeLoad();
            var cb = _pendingOnLoaded;
            _pendingOnLoaded = null;
            _pendingOnLoadFailed = null;
            cb?.Invoke();
        }

        void OnLoadFailed(string id, MaxSdkBase.ErrorInfo err)
        {
            if (id != _adUnitId) return;
            UnsubscribeLoad();
            var cb = _pendingOnLoadFailed;
            _pendingOnLoaded = null;
            _pendingOnLoadFailed = null;
            cb?.Invoke(err.Message);
        }

        void UnsubscribeLoad()
        {
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnLoadResult;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnLoadFailed;
        }

        public void Show(Action onShown = null, Action onClosed = null, Action<string> onFailed = null)
        {
            if (!IsLoaded)
            {
                onFailed?.Invoke("Interstitial not loaded");
                return;
            }

            _pendingOnShown = onShown;
            _pendingOnClosed = onClosed;
            _pendingOnShowFailed = onFailed;

            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnShown;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnShown;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnHidden;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnHidden;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnDisplayFailed;
            MaxSdk.ShowInterstitial(_adUnitId);
        }

        void OnShown(string id, MaxSdkBase.AdInfo info)
        {
            if (id != _adUnitId) return;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnShown;
            var cb = _pendingOnShown;
            _pendingOnShown = null;
            cb?.Invoke();
        }

        void OnHidden(string id, MaxSdkBase.AdInfo info)
        {
            if (id != _adUnitId) return;
            UnsubscribeShow();
            var cb = _pendingOnClosed;
            ClearShowCallbacks();
            cb?.Invoke();
            // MAX interstitial is one-time-use â€” warm-load the next impression after close.
            WarmReload();
        }

        void OnDisplayFailed(string id, MaxSdkBase.ErrorInfo err, MaxSdkBase.AdInfo info)
        {
            if (id != _adUnitId) return;
            UnsubscribeShow();
            var cb = _pendingOnShowFailed;
            ClearShowCallbacks();
            cb?.Invoke(err.Message);
            // Display failed consumed/invalidated the loaded ad — reload a fresh one.
            WarmReload();
        }

        void UnsubscribeShow()
        {
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnShown;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnHidden;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnDisplayFailed;
        }

        void ClearShowCallbacks()
        {
            _pendingOnShown = null;
            _pendingOnClosed = null;
            _pendingOnShowFailed = null;
        }

        void WarmReload()
        {
            if (string.IsNullOrEmpty(_adUnitId) || MaxSdk.IsInterstitialReady(_adUnitId))
                return;
            MaxSdk.LoadInterstitial(_adUnitId);
        }
    }

    internal class MaxRewardedAd : IRewardedAd
    {
        private readonly string _adUnitId;
        private Action _pendingOnLoaded;
        private Action<string> _pendingOnLoadFailed;
        private Action _pendingOnRewardEarned;
        private Action _pendingOnClosed;
        private Action<string> _pendingOnShowFailed;
        private readonly RewardedShowCompletionTracker _showCompletion = new RewardedShowCompletionTracker();

        public MaxRewardedAd(string adUnitId) => _adUnitId = adUnitId;
        public bool IsLoaded => MaxSdk.IsRewardedAdReady(_adUnitId);

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            if (string.IsNullOrEmpty(_adUnitId))
            {
                onFailed?.Invoke("Rewarded ad unit id is empty");
                return;
            }

            _pendingOnLoaded = onLoaded;
            _pendingOnLoadFailed = onFailed;

            // Idempotent: never stack handlers when Load is called repeatedly before a result.
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnLoadResult;
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnLoadResult;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnLoadFailed;
            MaxSdk.LoadRewardedAd(_adUnitId);
        }

        void OnLoadResult(string id, MaxSdkBase.AdInfo info)
        {
            if (id != _adUnitId) return;
            UnsubscribeLoad();
            var cb = _pendingOnLoaded;
            _pendingOnLoaded = null;
            _pendingOnLoadFailed = null;
            cb?.Invoke();
        }

        void OnLoadFailed(string id, MaxSdkBase.ErrorInfo err)
        {
            if (id != _adUnitId) return;
            UnsubscribeLoad();
            var cb = _pendingOnLoadFailed;
            _pendingOnLoaded = null;
            _pendingOnLoadFailed = null;
            cb?.Invoke(err.Message);
        }

        void UnsubscribeLoad()
        {
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnLoadResult;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnLoadFailed;
        }

        public void Show(Action onRewardEarned = null, Action onClosed = null, Action<string> onFailed = null)
        {
            if (!IsLoaded)
            {
                onFailed?.Invoke("Rewarded ad not loaded");
                return;
            }

            _pendingOnRewardEarned = onRewardEarned;
            _pendingOnClosed = onClosed;
            _pendingOnShowFailed = onFailed;
            _showCompletion.Reset();

            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnReward;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnReward;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnHidden;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnHidden;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnDisplayFailed;
            MaxSdk.ShowRewardedAd(_adUnitId);
        }

        void OnReward(string id, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo info)
        {
            if (id != _adUnitId) return;
            _showCompletion.NotifyRewardGranted(() => _pendingOnRewardEarned?.Invoke());
        }

        void OnHidden(string id, MaxSdkBase.AdInfo info)
        {
            if (id != _adUnitId) return;
            UnsubscribeShowExceptReward();
            _showCompletion.NotifyFullscreenClosed(() =>
            {
                var onClosed = _pendingOnClosed;
                UnsubscribeShow();
                ClearShowCallbacks();
                onClosed?.Invoke();
                WarmReload();
            });
        }

        void OnDisplayFailed(string id, MaxSdkBase.ErrorInfo err, MaxSdkBase.AdInfo info)
        {
            if (id != _adUnitId) return;
            UnsubscribeShow();
            var cb = _pendingOnShowFailed;
            ClearShowCallbacks();
            cb?.Invoke(err.Message);
            // Display failed consumed/invalidated the loaded ad — reload a fresh one.
            WarmReload();
        }

        void UnsubscribeShow()
        {
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnReward;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnHidden;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnDisplayFailed;
        }

        void UnsubscribeShowExceptReward()
        {
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnHidden;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnDisplayFailed;
        }

        void ClearShowCallbacks()
        {
            _pendingOnRewardEarned = null;
            _pendingOnClosed = null;
            _pendingOnShowFailed = null;
        }

        void WarmReload()
        {
            if (string.IsNullOrEmpty(_adUnitId) || MaxSdk.IsRewardedAdReady(_adUnitId))
                return;
            MaxSdk.LoadRewardedAd(_adUnitId);
        }
    }

    internal class MaxBannerAd : IBannerAd
    {
        private readonly string _adUnitId;
        private readonly MaxSdkBase.BannerPosition _position;
        private bool _isVisible;
        private bool _isLoaded;
        private bool _created;
        private Action _pendingOnLoaded;
        private Action<string> _pendingOnFailed;

        public MaxBannerAd(string adUnitId, MaxSdkBase.BannerPosition position)
        {
            _adUnitId = adUnitId;
            _position = position;
        }

        public bool IsLoaded => _isLoaded;
        public bool IsVisible => _isVisible;

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            if (string.IsNullOrEmpty(_adUnitId))
            {
                onFailed?.Invoke("Banner ad unit id is empty");
                return;
            }

            // MAX banners auto-refresh — keep persistent load callbacks subscribed idempotently.
            MaxSdkCallbacks.Banner.OnAdLoadedEvent -= OnLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= OnFailed;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnFailed;

            if (!_created)
            {
                _pendingOnLoaded = onLoaded;
                _pendingOnFailed = onFailed;
                MaxSdk.CreateBanner(_adUnitId, _position);
                _created = true;
                return;
            }

            // Already created (and auto-refreshing). Report immediately if a fill exists.
            if (_isLoaded)
            {
                onLoaded?.Invoke();
                return;
            }

            _pendingOnLoaded = onLoaded;
            _pendingOnFailed = onFailed;
        }

        void OnLoaded(string id, MaxSdkBase.AdInfo info)
        {
            if (id != _adUnitId) return;
            _isLoaded = true;
            var cb = _pendingOnLoaded;
            _pendingOnLoaded = null;
            _pendingOnFailed = null;
            cb?.Invoke();
        }

        void OnFailed(string id, MaxSdkBase.ErrorInfo err)
        {
            if (id != _adUnitId) return;
            _isLoaded = false;
            var cb = _pendingOnFailed;
            _pendingOnFailed = null;
            cb?.Invoke(err.Message);
        }

        public void Show(Action onShown = null, Action<string> onFailed = null)
        {
            if (!_created)
            {
                onFailed?.Invoke("Banner not created — call Load first");
                return;
            }

            MaxSdk.ShowBanner(_adUnitId);
            _isVisible = true;
            onShown?.Invoke();
        }

        public void Hide()
        {
            MaxSdk.HideBanner(_adUnitId);
            _isVisible = false;
        }

        public void Destroy()
        {
            MaxSdkCallbacks.Banner.OnAdLoadedEvent -= OnLoaded;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= OnFailed;
            MaxSdk.DestroyBanner(_adUnitId);
            _isVisible = false;
            _isLoaded = false;
            _created = false;
        }
    }

    internal class MaxAppOpenAd : IAppOpenAd
    {
        private readonly string _adUnitId;
        private Action _pendingOnLoaded;
        private Action<string> _pendingOnLoadFailed;
        private Action _pendingOnShown;
        private Action _pendingOnClosed;
        private Action<string> _pendingOnShowFailed;

        public MaxAppOpenAd(string adUnitId) => _adUnitId = adUnitId;
        public bool IsLoaded => !string.IsNullOrEmpty(_adUnitId) && MaxSdk.IsAppOpenAdReady(_adUnitId);

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            if (string.IsNullOrEmpty(_adUnitId))
            {
                onFailed?.Invoke("App open ad unit id is empty");
                return;
            }

            _pendingOnLoaded = onLoaded;
            _pendingOnLoadFailed = onFailed;

            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent -= OnLoadResult;
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnLoadResult;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent -= OnLoadFailed;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnLoadFailed;
            MaxSdk.LoadAppOpenAd(_adUnitId);
        }

        void OnLoadResult(string id, MaxSdkBase.AdInfo info)
        {
            if (id != _adUnitId) return;
            UnsubscribeLoad();
            var cb = _pendingOnLoaded;
            _pendingOnLoaded = null;
            _pendingOnLoadFailed = null;
            cb?.Invoke();
        }

        void OnLoadFailed(string id, MaxSdkBase.ErrorInfo err)
        {
            if (id != _adUnitId) return;
            UnsubscribeLoad();
            var cb = _pendingOnLoadFailed;
            _pendingOnLoaded = null;
            _pendingOnLoadFailed = null;
            cb?.Invoke(err.Message);
        }

        void UnsubscribeLoad()
        {
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent -= OnLoadResult;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent -= OnLoadFailed;
        }

        public void Show(Action onShown = null, Action onClosed = null, Action<string> onFailed = null)
        {
            if (!IsLoaded)
            {
                onFailed?.Invoke("App open ad not loaded");
                return;
            }

            _pendingOnShown = onShown;
            _pendingOnClosed = onClosed;
            _pendingOnShowFailed = onFailed;

            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent -= OnShown;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnShown;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent -= OnHidden;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnHidden;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent -= OnDisplayFailed;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnDisplayFailed;
            MaxSdk.ShowAppOpenAd(_adUnitId);
        }

        void OnShown(string id, MaxSdkBase.AdInfo info)
        {
            if (id != _adUnitId) return;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent -= OnShown;
            var cb = _pendingOnShown;
            _pendingOnShown = null;
            cb?.Invoke();
        }

        void OnHidden(string id, MaxSdkBase.AdInfo info)
        {
            if (id != _adUnitId) return;
            UnsubscribeShow();
            var cb = _pendingOnClosed;
            ClearShowCallbacks();
            cb?.Invoke();
        }

        void OnDisplayFailed(string id, MaxSdkBase.ErrorInfo err, MaxSdkBase.AdInfo info)
        {
            if (id != _adUnitId) return;
            UnsubscribeShow();
            var cb = _pendingOnShowFailed;
            ClearShowCallbacks();
            cb?.Invoke(err.Message);
        }

        void UnsubscribeShow()
        {
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent -= OnShown;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent -= OnHidden;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent -= OnDisplayFailed;
        }

        void ClearShowCallbacks()
        {
            _pendingOnShown = null;
            _pendingOnClosed = null;
            _pendingOnShowFailed = null;
        }
    }
}
#endif
