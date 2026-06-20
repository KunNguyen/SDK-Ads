using System;
using System.Collections;
using System.Collections.Generic;
using JisSDKAds.Core.Events;
using JisSDKAds.Core.Interfaces;
using JisSDKAds.Core.Models;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("maxRetries")]
        [Tooltip("Maximum on-demand load attempts per provider before switching to fallback or failing.")]
        [SerializeField, Min(0)] private int maxLoadAttemptsPerProvider = 3;
        [SerializeField] private float retryDelaySeconds = 2f;

        private readonly Dictionary<AdProviderId, IAdService> _providers = new Dictionary<AdProviderId, IAdService>();
        private readonly Dictionary<AdFormat, AdProviderId> _formatProviders = new Dictionary<AdFormat, AdProviderId>();
        private readonly HashSet<AdProviderId> _initializedProviders = new HashSet<AdProviderId>();
        private readonly Dictionary<AdProviderId, string> _failedProviderErrors = new Dictionary<AdProviderId, string>();
        private bool _isInitialized;
        private int _interstitialShowAttemptId;

        public bool IsInitialized => _isInitialized;

        public bool IsProviderInitialized(AdProviderId id) =>
            id != AdProviderId.None && _initializedProviders.Contains(id);

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

        /// <summary>
        /// Re-initialize a single provider after a prior init failure.
        /// </summary>
        public void RetryInitializeProvider(AdProviderId providerId, Action onSuccess = null, Action<string> onFailure = null)
        {
            if (!_providers.TryGetValue(providerId, out var provider))
            {
                onFailure?.Invoke($"Provider {providerId} is not registered.");
                return;
            }

            if (_initializedProviders.Contains(providerId))
            {
                onSuccess?.Invoke();
                return;
            }

            try
            {
                provider.Initialize(
                    () =>
                    {
                        _initializedProviders.Add(providerId);
                        _failedProviderErrors.Remove(providerId);
                        if (!_isInitialized)
                            _isInitialized = true;
                        AdEvents.RaiseProviderInitialized(provider.ProviderId);
                        onSuccess?.Invoke();
                    },
                    err =>
                    {
                        _initializedProviders.Remove(providerId);
                        _failedProviderErrors[providerId] = err;
                        AdEvents.RaiseProviderFailed(provider.ProviderId, err);
                        onFailure?.Invoke(err);
                    });
            }
            catch (Exception ex)
            {
                _initializedProviders.Remove(providerId);
                _failedProviderErrors[providerId] = ex.Message;
                AdEvents.RaiseProviderFailed(provider.ProviderId, ex.Message);
                onFailure?.Invoke(ex.Message);
            }
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
        public void ShowInterstitial(Action onClosed = null, Action<string> onFailed = null, int showAttemptId = 0)
        {
            if (!CanShowFormat(AdFormat.Interstitial))
            {
                onFailed?.Invoke("AdManager not initialized");
                return;
            }

            var provider = GetProviderIdForFormat(AdFormat.Interstitial);
            TryShowInterstitialWithFallback(
                provider,
                ResolveFallbackProvider(provider),
                0,
                onClosed,
                onFailed,
                showAttemptId);
        }

        public void ShowInterstitial(
            AdProviderId provider,
            AdProviderId fallback,
            Action onClosed = null,
            Action<string> onFailed = null,
            int showAttemptId = 0)
        {
            if (!CanShowThroughProviders(provider, fallback))
            {
                onFailed?.Invoke("AdManager not initialized");
                return;
            }

            TryShowInterstitialWithFallback(
                provider,
                fallback,
                0,
                onClosed,
                onFailed,
                showAttemptId,
                forceProviderFallback: true);
        }

        private void TryShowInterstitialWithFallback(
            AdProviderId primary,
            AdProviderId fallback,
            int attempt,
            Action onClosed,
            Action<string> onFailed,
            int showAttemptId,
            bool forceProviderFallback = false)
        {
            var provider = GetProvider(primary);
            if (provider == null)
            {
                TryFallbackOrRetryInterstitial(
                    attempt, primary, fallback, onClosed, onFailed, showAttemptId, forceProviderFallback);
                return;
            }

            if (!provider.Interstitial.IsLoaded)
            {
                if (attempt < maxLoadAttemptsPerProvider)
                {
                    provider.Interstitial.Load(
                        onLoaded: () =>
                        {
                            AdEvents.RaiseInterstitialLoaded(AdFormat.Interstitial, primary.ToString());
                            ShowLoadedInterstitial(provider, primary, onClosed, onFailed, showAttemptId);
                        },
                        onFailed: err =>
                        {
                            AdEvents.RaiseInterstitialFailed(AdFormat.Interstitial, primary.ToString(), err);
                            TryFallbackOrRetryInterstitial(
                                attempt, primary, fallback, onClosed, onFailed, showAttemptId, forceProviderFallback);
                        }
                    );
                    return;
                }

                TryFallbackOrRetryInterstitial(
                    attempt, primary, fallback, onClosed, onFailed, showAttemptId, forceProviderFallback);
                return;
            }

            ShowLoadedInterstitial(provider, primary, onClosed, onFailed, showAttemptId);
        }

        void ShowLoadedInterstitial(
            IAdService provider,
            AdProviderId providerId,
            Action onClosed,
            Action<string> onFailed,
            int showAttemptId)
        {
            _interstitialShowAttemptId = showAttemptId;
            provider.Interstitial.Show(
                onShown: () => AdEvents.RaiseInterstitialShown(AdFormat.Interstitial, _interstitialShowAttemptId),
                onClosed: () =>
                {
                    AdEvents.RaiseInterstitialClosed(AdFormat.Interstitial);
                    onClosed?.Invoke();
                },
                onFailed: err =>
                {
                    AdEvents.RaiseInterstitialFailed(AdFormat.Interstitial, providerId.ToString(), err);
                    onFailed?.Invoke(err);
                });
        }

        /// <summary>
        /// Show rewarded ad with fallback and retry.
        /// </summary>
        public void ShowRewarded(Action onRewardEarned = null, Action onClosed = null, Action<string> onFailed = null)
        {
            if (!CanShowFormat(AdFormat.Rewarded))
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
            if (!CanShowThroughProviders(provider, fallback))
            {
                onFailed?.Invoke("AdManager not initialized");
                return;
            }

            TryShowRewardedWithFallback(
                provider, fallback, 0, onRewardEarned, onClosed, onFailed, forceProviderFallback: true);
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
                TryFallbackOrRetryRewarded(
                    attempt, primary, fallback, onRewardEarned, onClosed, onFailed, forceProviderFallback);
                return;
            }

            if (!provider.Rewarded.IsLoaded)
            {
                if (attempt < maxLoadAttemptsPerProvider)
                {
                    provider.Rewarded.Load(
                        onLoaded: () =>
                        {
                            AdEvents.RaiseRewardedLoaded(AdFormat.Rewarded, primary.ToString());
                            ShowLoadedRewarded(provider, primary, onRewardEarned, onClosed, onFailed);
                        },
                        onFailed: err =>
                        {
                            AdEvents.RaiseRewardedFailed(AdFormat.Rewarded, primary.ToString(), err);
                            TryFallbackOrRetryRewarded(
                                attempt, primary, fallback, onRewardEarned, onClosed, onFailed, forceProviderFallback);
                        }
                    );
                    return;
                }

                TryFallbackOrRetryRewarded(
                    attempt, primary, fallback, onRewardEarned, onClosed, onFailed, forceProviderFallback);
                return;
            }

            ShowLoadedRewarded(provider, primary, onRewardEarned, onClosed, onFailed);
        }

        void ShowLoadedRewarded(
            IAdService provider,
            AdProviderId providerId,
            Action onRewardEarned,
            Action onClosed,
            Action<string> onFailed)
        {
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
                    AdEvents.RaiseRewardedFailed(AdFormat.Rewarded, providerId.ToString(), err);
                    onFailed?.Invoke(err);
                });
        }

        /// <summary>
        /// Show banner. Uses primary provider.
        /// </summary>
        public void ShowBanner(Action onShown = null, Action<string> onFailed = null)
        {
            if (!CanShowFormat(AdFormat.Banner))
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

            var providerId = GetProviderIdForFormat(AdFormat.Banner);
            provider.Banner.Load(
                onLoaded: () => provider.Banner.Show(onShown, onFailed),
                onFailed: err =>
                {
                    AdEvents.RaiseBannerFailed(AdFormat.Banner, providerId.ToString(), err);
                    onFailed?.Invoke(err);
                }
            );
        }

        /// <summary>
        /// Hide banner.
        /// </summary>
        public void HideBanner()
        {
            if (!CanShowFormat(AdFormat.Banner))
                return;
            GetProviderForFormat(AdFormat.Banner)?.Banner.Hide();
        }

        /// <summary>
        /// Show app open ad using primary provider (MAX when configured).
        /// </summary>
        public void ShowAppOpen(Action onClosed = null, Action<string> onFailed = null)
        {
            if (!CanShowFormat(AdFormat.AppOpen))
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

            var providerId = GetProviderIdForFormat(AdFormat.AppOpen);
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
                        AdEvents.RaiseInterstitialFailed(AdFormat.AppOpen, providerId.ToString(), err);
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
            int showAttemptId,
            bool forceProviderFallback = false)
        {
            if (attempt < maxLoadAttemptsPerProvider)
            {
                StartCoroutine(CoDelayedRetry(() =>
                    TryShowInterstitialWithFallback(
                        primary, fallback, attempt + 1, onClosed, onFailed, showAttemptId, forceProviderFallback)));
                return;
            }

            if ((allowCrossProviderFallback || forceProviderFallback) && fallback != AdProviderId.None && fallback != primary)
            {
                TryShowInterstitialWithFallback(
                    fallback,
                    AdProviderId.None,
                    0,
                    onClosed,
                    onFailed,
                    showAttemptId,
                    forceProviderFallback);
                return;
            }

            AdEvents.RaiseInterstitialFailed(
                AdFormat.Interstitial,
                primary.ToString(),
                "Interstitial failed after retries");
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
            if (attempt < maxLoadAttemptsPerProvider)
            {
                StartCoroutine(CoDelayedRetry(() =>
                    TryShowRewardedWithFallback(
                        primary, fallback, attempt + 1, onRewardEarned, onClosed, onFailed, forceProviderFallback)));
                return;
            }

            if ((allowCrossProviderFallback || forceProviderFallback) && fallback != AdProviderId.None && fallback != primary)
            {
                TryShowRewardedWithFallback(
                    fallback,
                    AdProviderId.None,
                    0,
                    onRewardEarned,
                    onClosed,
                    onFailed,
                    forceProviderFallback);
                return;
            }

            AdEvents.RaiseRewardedFailed(
                AdFormat.Rewarded,
                primary.ToString(),
                "Rewarded ad failed after retries");
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

        bool CanShowThroughProviders(AdProviderId primary, AdProviderId fallback)
        {
            if (_isInitialized)
                return true;

            if (primary != AdProviderId.None && IsProviderInitialized(primary))
                return true;

            return fallback != AdProviderId.None && IsProviderInitialized(fallback);
        }

        bool CanShowFormat(AdFormat format)
        {
            if (_isInitialized)
                return true;

            var primary = GetProviderIdForFormat(format);
            return CanShowThroughProviders(primary, ResolveFallbackProvider(primary));
        }
    }
}
