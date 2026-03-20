#if UNITY_AD_MAX
using System;
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

        public AdProviderId ProviderId => AdProviderId.Max;
        public IAdService CreateProvider() => new MaxAdProvider(sdkKey, interstitialAdUnitId, rewardedAdUnitId, bannerAdUnitId);
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
        private bool _isInitialized;

        public string ProviderId => "Max";
        public bool IsInitialized => _isInitialized;

        public IInterstitialAd Interstitial { get; }
        public IRewardedAd Rewarded { get; }
        public IBannerAd Banner { get; }

        public MaxAdProvider(string sdkKey, string interstitialAdUnitId, string rewardedAdUnitId, string bannerAdUnitId)
        {
            _sdkKey = sdkKey;
            _interstitialAdUnitId = interstitialAdUnitId;
            _rewardedAdUnitId = rewardedAdUnitId;
            _bannerAdUnitId = bannerAdUnitId;

            Interstitial = new MaxInterstitialAd(_interstitialAdUnitId);
            Rewarded = new MaxRewardedAd(_rewardedAdUnitId);
            Banner = new MaxBannerAd(_bannerAdUnitId);
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

        public MaxInterstitialAd(string adUnitId) => _adUnitId = adUnitId;
        public bool IsLoaded => MaxSdk.IsInterstitialReady(_adUnitId);

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnFailed;
            MaxSdk.LoadInterstitial(_adUnitId);

            void OnLoaded(string id, MaxSdkBase.AdInfo info)
            {
                if (id != _adUnitId) return;
                MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnLoaded;
                MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnFailed;
                onLoaded?.Invoke();
            }

            void OnFailed(string id, MaxSdkBase.ErrorInfo err)
            {
                if (id != _adUnitId) return;
                MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnLoaded;
                MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnFailed;
                onFailed?.Invoke(err.Message);
            }
        }

        public void Show(Action onShown = null, Action onClosed = null, Action<string> onFailed = null)
        {
            if (!IsLoaded)
            {
                onFailed?.Invoke("Interstitial not loaded");
                return;
            }

            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnShown;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnClosedHandler;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnFailedHandler;
            MaxSdk.ShowInterstitial(_adUnitId);

            void OnShown(string id, MaxSdkBase.AdInfo info)
            {
                if (id != _adUnitId) return;
                MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnShown;
                onShown?.Invoke();
            }

            void OnClosedHandler(string id, MaxSdkBase.AdInfo info)
            {
                if (id != _adUnitId) return;
                Unsubscribe();
                onClosed?.Invoke();
            }

            void OnFailedHandler(string id, MaxSdkBase.ErrorInfo err, MaxSdkBase.AdInfo info)
            {
                if (id != _adUnitId) return;
                Unsubscribe();
                onFailed?.Invoke(err.Message);
            }

            void Unsubscribe()
            {
                MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnClosedHandler;
                MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnFailedHandler;
            }
        }
    }

    internal class MaxRewardedAd : IRewardedAd
    {
        private readonly string _adUnitId;

        public MaxRewardedAd(string adUnitId) => _adUnitId = adUnitId;
        public bool IsLoaded => MaxSdk.IsRewardedAdReady(_adUnitId);

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnLoadedHandler;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnFailedHandler;
            MaxSdk.LoadRewardedAd(_adUnitId);

            void OnLoadedHandler(string id, MaxSdkBase.AdInfo info)
            {
                if (id != _adUnitId) return;
                MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnLoadedHandler;
                MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnFailedHandler;
                onLoaded?.Invoke();
            }

            void OnFailedHandler(string id, MaxSdkBase.ErrorInfo err)
            {
                if (id != _adUnitId) return;
                MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnLoadedHandler;
                MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnFailedHandler;
                onFailed?.Invoke(err.Message);
            }
        }

        public void Show(Action onRewardEarned = null, Action onClosed = null, Action<string> onFailed = null)
        {
            if (!IsLoaded)
            {
                onFailed?.Invoke("Rewarded ad not loaded");
                return;
            }

            bool rewardEarned = false;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnReward;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnClosedHandler;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnFailedHandler;
            MaxSdk.ShowRewardedAd(_adUnitId);

            void OnReward(string id, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo info)
            {
                if (id != _adUnitId) return;
                rewardEarned = true;
            }

            void OnClosedHandler(string id, MaxSdkBase.AdInfo info)
            {
                if (id != _adUnitId) return;
                MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnReward;
                MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnClosedHandler;
                MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnFailedHandler;
                if (rewardEarned) onRewardEarned?.Invoke();
                onClosed?.Invoke();
            }

            void OnFailedHandler(string id, MaxSdkBase.ErrorInfo err, MaxSdkBase.AdInfo info)
            {
                if (id != _adUnitId) return;
                MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnReward;
                MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnClosedHandler;
                MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnFailedHandler;
                onFailed?.Invoke(err.Message);
            }
        }
    }

    internal class MaxBannerAd : IBannerAd
    {
        private readonly string _adUnitId;
        private bool _isVisible;

        public MaxBannerAd(string adUnitId) => _adUnitId = adUnitId;
        public bool IsLoaded => true;
        public bool IsVisible => _isVisible;

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnLoadedHandler;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnFailedHandler;
            MaxSdk.CreateBanner(_adUnitId, MaxSdkBase.BannerPosition.BottomCenter);

            void OnLoadedHandler(string id, MaxSdkBase.AdInfo info)
            {
                if (id != _adUnitId) return;
                MaxSdkCallbacks.Banner.OnAdLoadedEvent -= OnLoadedHandler;
                MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= OnFailedHandler;
                onLoaded?.Invoke();
            }

            void OnFailedHandler(string id, MaxSdkBase.ErrorInfo err)
            {
                if (id != _adUnitId) return;
                MaxSdkCallbacks.Banner.OnAdLoadedEvent -= OnLoadedHandler;
                MaxSdkCallbacks.Banner.OnAdLoadFailedEvent -= OnFailedHandler;
                onFailed?.Invoke(err.Message);
            }
        }

        public void Show(Action onShown = null, Action<string> onFailed = null)
        {
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
            MaxSdk.DestroyBanner(_adUnitId);
            _isVisible = false;
        }
    }
}
#endif
