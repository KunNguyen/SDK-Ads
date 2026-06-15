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
        private readonly Dictionary<AdFormat, AdProviderId> _formatProviders = new Dictionary<AdFormat, AdProviderId>();
        private readonly HashSet<AdProviderId> _initializedProviders = new HashSet<AdProviderId>();
        private readonly Dictionary<AdProviderId, string> _failedProviderErrors = new Dictionary<AdProviderId, string>();
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
            _initializedProviders.Remove(id);
            _failedProviderErrors.Remove(id);
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
        /// Route a specific format through a specific provider. Formats without an override use the primary provider.
        /// </summary>
        public void SetFormatProvider(AdFormat format, AdProviderId provider)
        {
            if (provider == AdProviderId.None)
                _formatProviders.Remove(format);
            else
                _formatProviders[format] = provider;
        }

        public AdProviderId GetProviderIdForFormat(AdFormat format) =>
            _formatProviders.TryGetValue(format, out var provider) ? provider : primaryProvider;

        public IAdService GetProviderForFormat(AdFormat format) =>
            GetProvider(GetProviderIdForFormat(format));

        public bool HasProvider(AdProviderId id) => GetProvider(id) != null;

        public bool IsInterstitialLoaded(AdProviderId id) =>
            GetProvider(id)?.Interstitial.IsLoaded ?? false;

        public bool IsRewardedLoaded(AdProviderId id) =>
            GetProvider(id)?.Rewarded.IsLoaded ?? false;

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
            int initializedCount = 0;

            _initializedProviders.Clear();
            _failedProviderErrors.Clear();

            foreach (var kvp in _providers)
            {
                var providerId = kvp.Key;
                var provider = kvp.Value;
                try
                {
                    provider.Initialize(
                        () =>
                        {
                            completed++;
                            initializedCount++;
                            _initializedProviders.Add(providerId);
                            _failedProviderErrors.Remove(providerId);
                            AdEvents.RaiseProviderInitialized(provider.ProviderId);
                        },
                        err =>
                        {
                            completed++;
                            lastError = err;
                            _initializedProviders.Remove(providerId);
                            _failedProviderErrors[providerId] = err;
                            AdEvents.RaiseProviderFailed(provider.ProviderId, err);
                        }
                    );
                }
                catch (Exception ex)
                {
                    completed++;
                    lastError = ex.Message;
                    _initializedProviders.Remove(providerId);
                    _failedProviderErrors[providerId] = ex.Message;
                    AdEvents.RaiseProviderFailed(provider.ProviderId, ex.Message);
                }
            }

            if (total == 0)
            {
                onFailure?.Invoke("No ad providers registered");
                yield break;
            }

            while (completed < total)
                yield return null;

            if (initializedCount > 0)
            {
                _isInitialized = true;
                onSuccess?.Invoke();
                yield break;
            }

            _isInitialized = false;
            onFailure?.Invoke(lastError ?? "All ad providers failed to initialize");
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

            var provider = GetProviderIdForFormat(AdFormat.Interstitial);
            TryShowInterstitialWithFallback(provider, ResolveFallbackProvider(provider), 0, onClosed, onFailed);
        }

        public void ShowInterstitial(
            AdProviderId provider,
            AdProviderId fallback,
            Action onClosed = null,
            Action<string> onFailed = null)
        {
            if (!_isInitialized)
            {
                onFailed?.Invoke("AdManager not initialized");
                return;
            }

            TryShowInterstitialWithFallback(provider, fallback, 0, onClosed, onFailed, forceProviderFallback: true);
        }

        private void TryShowInterstitialWithFallback(
            AdProviderId primary,
            AdProviderId fallback,
            int attempt,
            Action onClosed,
            Action<string> onFailed,
            bool forceProviderFallback = false)
        {
            var provider = GetProvider(primary);
            if (provider == null)
            {
                TryFallbackOrRetryInterstitial(attempt, primary, fallback, onClosed, onFailed, forceProviderFallback);
                return;
            }

            if (!provider.Interstitial.IsLoaded)
            {
                if (attempt < maxRetries)
                {
                    provider.Interstitial.Load(
                        onLoaded: () => provider.Interstitial.Show(
                            onShown: () => AdEvents.RaiseInterstitialShown(AdFormat.Interstitial),
                            onClosed: () =>
                            {
                                AdEvents.RaiseInterstitialClosed(AdFormat.Interstitial);
                                onClosed?.Invoke();
                            },
                            onFailed: err =>
                            {
                                AdEvents.RaiseInterstitialFailed(AdFormat.Interstitial, err);
                                TryFallbackOrRetryInterstitial(attempt, primary, fallback, onClosed, onFailed, forceProviderFallback);
                            }),
                        onFailed: _ => TryFallbackOrRetryInterstitial(attempt, primary, fallback, onClosed, onFailed, forceProviderFallback)
                    );
                    return;
                }
                TryFallbackOrRetryInterstitial(attempt, primary, fallback, onClosed, onFailed, forceProviderFallback);
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
                    TryFallbackOrRetryInterstitial(attempt, primary, fallback, onClosed, onFailed, forceProviderFallback);
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

            var provider = GetProviderIdForFormat(AdFormat.Rewarded);
            TryShowRewardedWithFallback(provider, ResolveFallbackProvider(provider), 0, onRewardEarned, onClosed, onFailed);
        }

        public void ShowRewarded(
            AdProviderId provider,
            AdProviderId fallback,
            Action onRewardEarned = null,
            Action onClosed = null,
            Action<string> onFailed = null)
        {
            if (!_isInitialized)
            {
                onFailed?.Invoke("AdManager not initialized");
                return;
            }

            TryShowRewardedWithFallback(provider, fallback, 0, onRewardEarned, onClosed, onFailed, forceProviderFallback: true);
        }

        private void TryShowRewardedWithFallback(
            AdProviderId primary,
            AdProviderId fallback,
            int attempt,
            Action onRewardEarned,
            Action onClosed,
            Action<string> onFailed,
            bool forceProviderFallback = false)
        {
            var provider = GetProvider(primary);
            if (provider == null)
            {
                TryFallbackOrRetryRewarded(attempt, primary, fallback, onRewardEarned, onClosed, onFailed, forceProviderFallback);
                return;
            }

            if (!provider.Rewarded.IsLoaded)
            {
                if (attempt < maxRetries)
                {
                    provider.Rewarded.Load(
                        onLoaded: () => provider.Rewarded.Show(
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
                                TryFallbackOrRetryRewarded(attempt, primary, fallback, onRewardEarned, onClosed, onFailed, forceProviderFallback);
                            }),
                        onFailed: _ => TryFallbackOrRetryRewarded(attempt, primary, fallback, onRewardEarned, onClosed, onFailed, forceProviderFallback)
                    );
                    return;
                }
                TryFallbackOrRetryRewarded(attempt, primary, fallback, onRewardEarned, onClosed, onFailed, forceProviderFallback);
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
                    TryFallbackOrRetryRewarded(attempt, primary, fallback, onRewardEarned, onClosed, onFailed, forceProviderFallback);
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

            var provider = GetProviderForFormat(AdFormat.Banner);
            if (provider == null)
            {
                onFailed?.Invoke($"No provider for {GetProviderIdForFormat(AdFormat.Banner)}");
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
            GetProviderForFormat(AdFormat.Banner)?.Banner.Hide();
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

            var provider = GetProviderForFormat(AdFormat.AppOpen);
            var appOpen = provider?.AppOpen;
            if (appOpen == null || appOpen is NullAppOpenAd)
            {
                onFailed?.Invoke($"App open not supported for {GetProviderIdForFormat(AdFormat.AppOpen)}");
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
            var appOpen = GetProviderForFormat(AdFormat.AppOpen)?.AppOpen;
            return appOpen != null && appOpen is not NullAppOpenAd && appOpen.IsLoaded;
        }

        private AdProviderId ResolveFallbackProvider(AdProviderId activeProvider)
        {
            if (!allowCrossProviderFallback || fallbackProvider == AdProviderId.None || fallbackProvider == activeProvider)
                return AdProviderId.None;
            return fallbackProvider;
        }

        private void TryFallbackOrRetryInterstitial(
            int attempt,
            AdProviderId primary,
            AdProviderId fallback,
            Action onClosed,
            Action<string> onFailed,
            bool forceProviderFallback = false)
        {
            if ((allowCrossProviderFallback || forceProviderFallback) && fallback != AdProviderId.None && fallback != primary)
            {
                StartCoroutine(CoDelayedRetry(() => TryShowInterstitialWithFallback(fallback, primary, attempt + 1, onClosed, onFailed, forceProviderFallback)));
                return;
            }
            if (attempt < maxRetries)
                StartCoroutine(CoDelayedRetry(() => TryShowInterstitialWithFallback(primary, fallback, attempt + 1, onClosed, onFailed, forceProviderFallback)));
            else
                onFailed?.Invoke("Interstitial failed after retries");
        }

        private void TryFallbackOrRetryRewarded(
            int attempt,
            AdProviderId primary,
            AdProviderId fallback,
            Action onRewardEarned,
            Action onClosed,
            Action<string> onFailed,
            bool forceProviderFallback = false)
        {
            if ((allowCrossProviderFallback || forceProviderFallback) && fallback != AdProviderId.None && fallback != primary)
            {
                StartCoroutine(CoDelayedRetry(() => TryShowRewardedWithFallback(fallback, primary, attempt + 1, onRewardEarned, onClosed, onFailed, forceProviderFallback)));
                return;
            }
            if (attempt < maxRetries)
                StartCoroutine(CoDelayedRetry(() => TryShowRewardedWithFallback(primary, fallback, attempt + 1, onRewardEarned, onClosed, onFailed, forceProviderFallback)));
            else
                onFailed?.Invoke("Rewarded ad failed after retries");
        }

        private IEnumerator CoDelayedRetry(Action action)
        {
            yield return new WaitForSecondsRealtime(retryDelaySeconds);
            action?.Invoke();
        }

        public IAdService GetProvider(AdProviderId id)
        {
            if (id == AdProviderId.None)
                return null;

            if (!_providers.TryGetValue(id, out var provider))
                return null;

            if (_isInitialized && !_initializedProviders.Contains(id))
                return null;

            return provider;
        }
    }
}
