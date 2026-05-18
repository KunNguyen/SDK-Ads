using System;
using System.Collections;
using System.Collections.Generic;
using JisSDKAds.Core.Events;
using JisSDKAds.Core.Interfaces;
using JisSDKAds.Core.Models;
using UnityEngine;

namespace JisSDKAds.Core
{
    /// <summary>
    /// Unified entry point for ads. Handles init, load, show, fallback, and retry.
    /// No direct dependency on any ad network — uses IAdService providers.
    /// </summary>
    public class AdManager : MonoBehaviour
    {
        public static AdManager Instance { get; private set; }

        [SerializeField] private AdProviderId primaryProvider = AdProviderId.Max;
        [SerializeField] private AdProviderId fallbackProvider = AdProviderId.None;
        [SerializeField] private bool allowCrossProviderFallback;
        [SerializeField] private int maxRetries = 3;
        [SerializeField] private float retryDelaySeconds = 2f;

        private readonly Dictionary<AdProviderId, IAdService> _providers = new Dictionary<AdProviderId, IAdService>();
        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;
        public IAdService PrimaryProvider => GetProvider(primaryProvider);
        public IAdService FallbackProvider => GetProvider(fallbackProvider);

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Register an ad provider. Call this before Initialize.
        /// </summary>
        public void RegisterProvider(AdProviderId id, IAdService service)
        {
            if (service == null) return;
            _providers[id] = service;
        }

        /// <summary>
        /// Set primary and fallback providers. Call before Initialize.
        /// </summary>
        public void SetProviderPriority(AdProviderId primary, AdProviderId fallback)
        {
            primaryProvider = primary;
            fallbackProvider = fallback;
        }

        /// <summary>
        /// Use one mediation per platform. When singleMediationOnly is true, cross-network fallback is disabled.
        /// </summary>
        public void ConfigureSingleMediation(AdProviderId primary, bool singleMediationOnly)
        {
            primaryProvider = primary;
            allowCrossProviderFallback = !singleMediationOnly;
            fallbackProvider = allowCrossProviderFallback ? fallbackProvider : AdProviderId.None;
        }

        /// <summary>
        /// Initialize all registered providers.
        /// </summary>
        public void Initialize(Action onSuccess = null, Action<string> onFailure = null)
        {
            if (_isInitialized)
            {
                onSuccess?.Invoke();
                return;
            }

            StartCoroutine(CoInitialize(onSuccess, onFailure));
        }

        private IEnumerator CoInitialize(Action onSuccess, Action<string> onFailure)
        {
            int completed = 0;
            int total = _providers.Count;
            string lastError = null;

            foreach (var kvp in _providers)
            {
                var provider = kvp.Value;
                provider.Initialize(
                    () =>
                    {
                        completed++;
                        AdEvents.RaiseProviderInitialized(provider.ProviderId);
                    },
                    err =>
                    {
                        completed++;
                        lastError = err;
                        AdEvents.RaiseProviderFailed(provider.ProviderId, err);
                    }
                );
            }

            if (total == 0)
            {
                onFailure?.Invoke("No ad providers registered");
                yield break;
            }

            while (completed < total)
                yield return null;

            _isInitialized = true;
            if (lastError != null && completed == total)
                onFailure?.Invoke(lastError);
            else
                onSuccess?.Invoke();
        }

        /// <summary>
        /// Show interstitial with fallback and retry.
        /// </summary>
        public void ShowInterstitial(Action onClosed = null, Action<string> onFailed = null)
        {
            if (!_isInitialized)
            {
                onFailed?.Invoke("AdManager not initialized");
                return;
            }

            TryShowInterstitialWithFallback(primaryProvider, fallbackProvider, 0, onClosed, onFailed);
        }

        private void TryShowInterstitialWithFallback(AdProviderId primary, AdProviderId fallback, int attempt, Action onClosed, Action<string> onFailed)
        {
            var provider = GetProvider(primary);
            if (provider == null)
            {
                TryFallbackOrRetryInterstitial(attempt, primary, fallback, onClosed, onFailed);
                return;
            }

            if (!provider.Interstitial.IsLoaded)
            {
                if (attempt < maxRetries)
                {
                    provider.Interstitial.Load(
                        onLoaded: () => provider.Interstitial.Show(onShown: null, onClosed, onFailed),
                        onFailed: _ => TryFallbackOrRetryInterstitial(attempt, primary, fallback, onClosed, onFailed)
                    );
                    return;
                }
                TryFallbackOrRetryInterstitial(attempt, primary, fallback, onClosed, onFailed);
                return;
            }

            provider.Interstitial.Show(
                onShown: () => AdEvents.RaiseInterstitialShown(AdFormat.Interstitial),
                onClosed: () =>
                {
                    AdEvents.RaiseInterstitialClosed(AdFormat.Interstitial);
                    onClosed?.Invoke();
                },
                onFailed: err =>
                {
                    AdEvents.RaiseInterstitialFailed(AdFormat.Interstitial, err);
                    TryFallbackOrRetryInterstitial(attempt, primary, fallback, onClosed, onFailed);
                }
            );
        }

        /// <summary>
        /// Show rewarded ad with fallback and retry.
        /// </summary>
        public void ShowRewarded(Action onRewardEarned = null, Action onClosed = null, Action<string> onFailed = null)
        {
            if (!_isInitialized)
            {
                onFailed?.Invoke("AdManager not initialized");
                return;
            }

            TryShowRewardedWithFallback(primaryProvider, fallbackProvider, 0, onRewardEarned, onClosed, onFailed);
        }

        private void TryShowRewardedWithFallback(AdProviderId primary, AdProviderId fallback, int attempt, Action onRewardEarned, Action onClosed, Action<string> onFailed)
        {
            var provider = GetProvider(primary);
            if (provider == null || !provider.Rewarded.IsLoaded)
            {
                if (attempt < maxRetries)
                {
                    provider?.Rewarded.Load(
                        onLoaded: () => provider.Rewarded.Show(onRewardEarned, onClosed, onFailed),
                        onFailed: _ => TryFallbackOrRetryRewarded(attempt, primary, fallback, onRewardEarned, onClosed, onFailed)
                    );
                    return;
                }
                TryFallbackOrRetryRewarded(attempt, primary, fallback, onRewardEarned, onClosed, onFailed);
                return;
            }

            provider.Rewarded.Show(
                onRewardEarned: () =>
                {
                    AdEvents.RaiseRewardEarned(AdFormat.Rewarded);
                    onRewardEarned?.Invoke();
                },
                onClosed: () =>
                {
                    AdEvents.RaiseRewardedClosed(AdFormat.Rewarded);
                    onClosed?.Invoke();
                },
                onFailed: err =>
                {
                    AdEvents.RaiseRewardedFailed(AdFormat.Rewarded, err);
                    TryFallbackOrRetryRewarded(attempt, primary, fallback, onRewardEarned, onClosed, onFailed);
                }
            );
        }

        /// <summary>
        /// Show banner. Uses primary provider.
        /// </summary>
        public void ShowBanner(Action onShown = null, Action<string> onFailed = null)
        {
            if (!_isInitialized)
            {
                onFailed?.Invoke("AdManager not initialized");
                return;
            }

            var provider = GetProvider(primaryProvider);
            if (provider == null)
            {
                onFailed?.Invoke($"No provider for {primaryProvider}");
                return;
            }

            if (provider.Banner.IsLoaded)
            {
                provider.Banner.Show(onShown, onFailed);
                return;
            }

            provider.Banner.Load(
                onLoaded: () => provider.Banner.Show(onShown, onFailed),
                onFailed: err =>
                {
                    AdEvents.RaiseBannerFailed(AdFormat.Banner, err);
                    onFailed?.Invoke(err);
                }
            );
        }

        /// <summary>
        /// Hide banner.
        /// </summary>
        public void HideBanner()
        {
            if (!_isInitialized) return;
            GetProvider(primaryProvider)?.Banner.Hide();
        }

        /// <summary>
        /// Show app open ad using primary provider (MAX when configured).
        /// </summary>
        public void ShowAppOpen(Action onClosed = null, Action<string> onFailed = null)
        {
            if (!_isInitialized)
            {
                onFailed?.Invoke("AdManager not initialized");
                return;
            }

            var provider = GetProvider(primaryProvider);
            var appOpen = provider?.AppOpen;
            if (appOpen == null || appOpen is NullAppOpenAd)
            {
                onFailed?.Invoke($"App open not supported for {primaryProvider}");
                return;
            }

            if (appOpen.IsLoaded)
            {
                appOpen.Show(
                    onClosed: () =>
                    {
                        AdEvents.RaiseInterstitialClosed(AdFormat.AppOpen);
                        onClosed?.Invoke();
                    },
                    onFailed: err =>
                    {
                        AdEvents.RaiseInterstitialFailed(AdFormat.AppOpen, err);
                        onFailed?.Invoke(err);
                    });
                return;
            }

            appOpen.Load(
                onLoaded: () => ShowAppOpen(onClosed, onFailed),
                onFailed: onFailed);
        }

        public bool IsAppOpenLoaded()
        {
            if (!_isInitialized) return false;
            var appOpen = GetProvider(primaryProvider)?.AppOpen;
            return appOpen != null && appOpen is not NullAppOpenAd && appOpen.IsLoaded;
        }

        private void TryFallbackOrRetryInterstitial(int attempt, AdProviderId primary, AdProviderId fallback, Action onClosed, Action<string> onFailed)
        {
            if (allowCrossProviderFallback && fallback != AdProviderId.None && fallback != primary)
            {
                StartCoroutine(CoDelayedRetry(() => TryShowInterstitialWithFallback(fallback, primary, attempt + 1, onClosed, onFailed)));
                return;
            }
            if (attempt < maxRetries)
                StartCoroutine(CoDelayedRetry(() => TryShowInterstitialWithFallback(primary, fallback, attempt + 1, onClosed, onFailed)));
            else
                onFailed?.Invoke("Interstitial failed after retries");
        }

        private void TryFallbackOrRetryRewarded(int attempt, AdProviderId primary, AdProviderId fallback, Action onRewardEarned, Action onClosed, Action<string> onFailed)
        {
            if (allowCrossProviderFallback && fallback != AdProviderId.None && fallback != primary)
            {
                StartCoroutine(CoDelayedRetry(() => TryShowRewardedWithFallback(fallback, primary, attempt + 1, onRewardEarned, onClosed, onFailed)));
                return;
            }
            if (attempt < maxRetries)
                StartCoroutine(CoDelayedRetry(() => TryShowRewardedWithFallback(primary, fallback, attempt + 1, onRewardEarned, onClosed, onFailed)));
            else
                onFailed?.Invoke("Rewarded ad failed after retries");
        }

        private IEnumerator CoDelayedRetry(Action action)
        {
            yield return new WaitForSecondsRealtime(retryDelaySeconds);
            action?.Invoke();
        }

        private IAdService GetProvider(AdProviderId id)
        {
            return id == AdProviderId.None ? null : _providers.TryGetValue(id, out var p) ? p : null;
        }
    }
}
