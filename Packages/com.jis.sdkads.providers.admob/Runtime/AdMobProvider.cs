#if UNITY_AD_ADMOB
using System;
using System.Collections;
using GoogleMobileAds.Api;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Common;
using JisSDKAds.Core.Interfaces;
using JisSDKAds.Core.Models;
using UnityEngine;

namespace JisSDKAds.Providers.AdMob
{
    [CreateAssetMenu(fileName = "AdMobConfig", menuName = "JisSDKAds/Providers/AdMob Config", order = 2)]
    [UnityEngine.Scripting.Preserve] // loaded by reflection from ProviderConfigFactory
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
        private InterstitialAd _ad; // loaded, not yet shown; the on-screen ad is owned by its own event closures
        private bool _isLoading;
        private int _loadGeneration;
        private readonly AdLoadWaiters _loadWaiters = new AdLoadWaiters();

        Action _pendingOnShown;
        Action _pendingOnClosed;
        Action<string> _pendingOnFailed;

        public AdMobInterstitialAd(string adUnitId) => _adUnitId = adUnitId;
        public bool IsLoaded => _ad != null && _ad.CanShowAd();

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            if (string.IsNullOrWhiteSpace(_adUnitId))
            {
                DebugAds.LogWarning("[AdMob][Interstitial] load_fail ad unit id is empty");
                onFailed?.Invoke("Interstitial ad unit id is empty");
                return;
            }

            // A Load() that joins a queued or running load still gets the result (never dropped).
            // While queued, the pipeline keeps a single pending generic load per format.
            _loadWaiters.Add(onLoaded, onFailed);
            if (_isLoading)
                return;

            AdLoadPipeline.RunInterstitialLoad(RunLoad);
        }

        void RunLoad()
        {
            if (_isLoading)
            {
                // Never hold the pipeline slot without a load running.
                AdLoadPipeline.NotifyInterstitialFinished();
                return;
            }

            if (_ad != null)
            {
                _ad.Destroy();
                _ad = null;
            }

            _isLoading = true;
            var generation = ++_loadGeneration;
            DebugAds.Log($"[AdMob][Interstitial] load_start adUnitId={_adUnitId}");
            AdMobLoadWatchdog.Start(() =>
                FailLoad(generation, $"load timed out after {AdMobLoadWatchdog.TimeoutSeconds}s"));
            try
            {
                var request = new AdRequest();
                InterstitialAd.Load(_adUnitId, request, (ad, error) =>
                {
                    var message = error != null
                        ? error.GetMessage() ?? "load error"
                        : ad == null ? "InterstitialAd.Load returned no ad and no error" : null;
                    AdMobMainThread.Run(() => OnLoadResult(generation, ad, message));
                });
            }
            catch (Exception e)
            {
                FailLoad(generation, $"InterstitialAd.Load threw: {e.Message}");
            }
        }

        void OnLoadResult(int generation, InterstitialAd ad, string error)
        {
            if (!_isLoading || generation != _loadGeneration)
            {
                // Expired (watchdog/exception) generation: drop the late ad so it doesn't leak.
                DebugAds.LogWarning($"[AdMob][Interstitial] load_result_ignored_expired adUnitId={_adUnitId}");
                DestroyAd(ad);
                return;
            }

            if (error != null)
            {
                DestroyAd(ad);
                FailLoad(generation, error);
                return;
            }

            _isLoading = false;
            AdLoadPipeline.NotifyInterstitialFinished();
            _ad = ad;
            RegisterEvents(ad);
            DebugAds.Log($"[AdMob][Interstitial] load_success adUnitId={_adUnitId}");
            _loadWaiters.NotifyLoaded();
        }

        void FailLoad(int generation, string error)
        {
            if (!_isLoading || generation != _loadGeneration)
                return;

            _isLoading = false;
            AdLoadPipeline.NotifyInterstitialFinished();
            DebugAds.LogWarning($"[AdMob][Interstitial] load_fail adUnitId={_adUnitId} error={error}");
            _loadWaiters.NotifyFailed(error);
        }

        private void RegisterEvents(InterstitialAd ad)
        {
            // Interstitial is one-time-use. After close/fail, destroy this instance; JisAds schedules the next load.
            ad.OnAdFullScreenContentOpened += () => AdMobMainThread.Run(() =>
            {
                DebugAds.Log($"[AdMob][Interstitial] show_opened adUnitId={_adUnitId}");
                _pendingOnShown?.Invoke();
            });
            ad.OnAdFullScreenContentClosed += () => AdMobMainThread.Run(() =>
            {
                DebugAds.Log($"[AdMob][Interstitial] show_closed adUnitId={_adUnitId}");
                _pendingOnClosed?.Invoke();
                DestroyAd(ad);
            });
            ad.OnAdFullScreenContentFailed += err =>
            {
                var message = err.GetMessage();
                AdMobMainThread.Run(() =>
                {
                    DebugAds.LogWarning($"[AdMob][Interstitial] show_failed adUnitId={_adUnitId} error={message}");
                    _pendingOnFailed?.Invoke(message);
                    DestroyAd(ad);
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
            // Hand the instance to its close/fail handlers so a Load during the show can't destroy it.
            var ad = _ad;
            _ad = null;
            ad.Show();
        }

        static void DestroyAd(InterstitialAd ad)
        {
            try
            {
                ad?.Destroy();
            }
            catch
            {
                // best effort
            }
        }
    }

    internal class AdMobRewardedAd : IRewardedAd
    {
        private readonly string _adUnitId;
        private RewardedAd _ad; // loaded, not yet shown; the on-screen ad is owned by its own event closures
        private bool _isLoading;
        private int _loadGeneration;
        private readonly AdLoadWaiters _loadWaiters = new AdLoadWaiters();

        Action _pendingOnRewardEarned;
        Action _pendingOnClosed;
        Action<string> _pendingOnFailed;
        RewardedShowCompletionTracker _showCompletion = new RewardedShowCompletionTracker();

        public AdMobRewardedAd(string adUnitId) => _adUnitId = adUnitId;
        public bool IsLoaded => _ad != null && _ad.CanShowAd();

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            if (string.IsNullOrWhiteSpace(_adUnitId))
            {
                DebugAds.LogWarning("[AdMob][Rewarded] load_fail ad unit id is empty");
                onFailed?.Invoke("Rewarded ad unit id is empty");
                return;
            }

            // A Load() that joins a queued or running load still gets the result (never dropped).
            // While queued, the pipeline keeps a single pending generic load per format.
            _loadWaiters.Add(onLoaded, onFailed);
            if (_isLoading)
                return;

            AdLoadPipeline.RunRewardedLoad(RunLoad);
        }

        void RunLoad()
        {
            if (_isLoading)
            {
                // Never hold the pipeline slot without a load running.
                AdLoadPipeline.NotifyRewardedFinished();
                return;
            }

            if (_ad != null)
            {
                _ad.Destroy();
                _ad = null;
            }

            _isLoading = true;
            var generation = ++_loadGeneration;
            DebugAds.Log($"[AdMob][Rewarded] load_start adUnitId={_adUnitId}");
            AdMobLoadWatchdog.Start(() =>
                FailLoad(generation, $"load timed out after {AdMobLoadWatchdog.TimeoutSeconds}s"));
            try
            {
                var request = new AdRequest();
                RewardedAd.Load(_adUnitId, request, (ad, error) =>
                {
                    var message = error != null
                        ? error.GetMessage() ?? "load error"
                        : ad == null ? "RewardedAd.Load returned no ad and no error" : null;
                    AdMobMainThread.Run(() => OnLoadResult(generation, ad, message));
                });
            }
            catch (Exception e)
            {
                FailLoad(generation, $"RewardedAd.Load threw: {e.Message}");
            }
        }

        void OnLoadResult(int generation, RewardedAd ad, string error)
        {
            if (!_isLoading || generation != _loadGeneration)
            {
                // Expired (watchdog/exception) generation: drop the late ad so it doesn't leak.
                DebugAds.LogWarning($"[AdMob][Rewarded] load_result_ignored_expired adUnitId={_adUnitId}");
                DestroyAd(ad);
                return;
            }

            if (error != null)
            {
                DestroyAd(ad);
                FailLoad(generation, error);
                return;
            }

            _isLoading = false;
            AdLoadPipeline.NotifyRewardedFinished();
            _ad = ad;
            RegisterEvents(ad);
            DebugAds.Log($"[AdMob][Rewarded] load_success adUnitId={_adUnitId}");
            _loadWaiters.NotifyLoaded();
        }

        void FailLoad(int generation, string error)
        {
            if (!_isLoading || generation != _loadGeneration)
                return;

            _isLoading = false;
            AdLoadPipeline.NotifyRewardedFinished();
            DebugAds.LogWarning($"[AdMob][Rewarded] load_fail adUnitId={_adUnitId} error={error}");
            _loadWaiters.NotifyFailed(error);
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
            // Hand the instance to its close/fail handlers so a Load during the show can't destroy it.
            var ad = _ad;
            _ad = null;
            ad.Show(_ => AdMobMainThread.Run(() => _showCompletion.NotifyRewardGranted(() =>
            {
                DebugAds.Log($"[AdMob][Rewarded] reward_granted adUnitId={_adUnitId}");
                _pendingOnRewardEarned?.Invoke();
            })));
        }

        void RegisterEvents(RewardedAd ad)
        {
            // Rewarded is one-time-use. After close/fail, destroy this instance; JisAds schedules the next load.
            ad.OnAdFullScreenContentOpened += () => AdMobMainThread.Run(() =>
            {
                DebugAds.Log($"[AdMob][Rewarded] show_opened adUnitId={_adUnitId}");
            });
            ad.OnAdFullScreenContentClosed += () => AdMobMainThread.Run(() =>
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
                    DestroyAd(ad);
                });
            });
            ad.OnAdFullScreenContentFailed += err =>
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
                    DestroyAd(ad);
                });
            };
        }

        static void DestroyAd(RewardedAd ad)
        {
            try
            {
                ad?.Destroy();
            }
            catch
            {
                // best effort
            }
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
        // App open ads expire 4 hours after load; an older ad must not count as loaded.
        static readonly TimeSpan MaxAdAge = TimeSpan.FromHours(4);

        private readonly string _adUnitId;
        private AppOpenAd _ad; // loaded, not yet shown; the on-screen ad is owned by its own event closures
        private DateTime _loadedAtUtc;
        private bool _isLoading;
        private int _loadGeneration;
        private readonly AdLoadWaiters _loadWaiters = new AdLoadWaiters();

        public AdMobAppOpenAd(string adUnitId) => _adUnitId = adUnitId;
        public bool IsLoaded => _ad != null && _ad.CanShowAd() && DateTime.UtcNow - _loadedAtUtc < MaxAdAge;

        public void Load(Action onLoaded = null, Action<string> onFailed = null)
        {
            if (string.IsNullOrWhiteSpace(_adUnitId))
            {
                DebugAds.LogWarning("[AdMob][AppOpen] load_fail ad unit id is empty");
                onFailed?.Invoke("App open ad unit id is empty");
                return;
            }

            // Join an in-flight load; a still-valid ad answers immediately instead of being destroyed.
            _loadWaiters.Add(onLoaded, onFailed);
            if (_isLoading)
                return;

            if (IsLoaded)
            {
                _loadWaiters.NotifyLoaded();
                return;
            }

            if (_ad != null)
            {
                DestroyAd(_ad);
                _ad = null;
            }

            _isLoading = true;
            var generation = ++_loadGeneration;
            DebugAds.Log($"[AdMob][AppOpen] load_start adUnitId={_adUnitId}");
            AdMobLoadWatchdog.Start(() =>
                FailLoad(generation, $"load timed out after {AdMobLoadWatchdog.TimeoutSeconds}s"));
            try
            {
                var request = new AdRequest();
                AppOpenAd.Load(_adUnitId, request, (ad, error) =>
                {
                    var message = error != null
                        ? error.GetMessage() ?? "load error"
                        : ad == null ? "AppOpenAd.Load returned no ad and no error" : null;
                    AdMobMainThread.Run(() => OnLoadResult(generation, ad, message));
                });
            }
            catch (Exception e)
            {
                FailLoad(generation, $"AppOpenAd.Load threw: {e.Message}");
            }
        }

        void OnLoadResult(int generation, AppOpenAd ad, string error)
        {
            if (!_isLoading || generation != _loadGeneration)
            {
                // Expired (watchdog/exception) generation: drop the late ad so it doesn't leak.
                DebugAds.LogWarning($"[AdMob][AppOpen] load_result_ignored_expired adUnitId={_adUnitId}");
                DestroyAd(ad);
                return;
            }

            if (error != null)
            {
                DestroyAd(ad);
                FailLoad(generation, error);
                return;
            }

            _isLoading = false;
            _ad = ad;
            _loadedAtUtc = DateTime.UtcNow;
            DebugAds.Log($"[AdMob][AppOpen] load_success adUnitId={_adUnitId}");
            _loadWaiters.NotifyLoaded();
        }

        void FailLoad(int generation, string error)
        {
            if (!_isLoading || generation != _loadGeneration)
                return;

            _isLoading = false;
            DebugAds.LogWarning($"[AdMob][AppOpen] load_fail adUnitId={_adUnitId} error={error}");
            _loadWaiters.NotifyFailed(error);
        }

        public void Show(Action onShown = null, Action onClosed = null, Action<string> onFailed = null)
        {
            if (!IsLoaded)
            {
                DebugAds.LogWarning($"[AdMob][AppOpen] show_blocked_not_loaded adUnitId={_adUnitId}");
                onFailed?.Invoke("App open ad not loaded");
                return;
            }

            DebugAds.Log($"[AdMob][AppOpen] show_call adUnitId={_adUnitId}");
            // Hand the instance to its close/fail handlers so a Load during the show can't destroy it.
            var ad = _ad;
            _ad = null;
            ad.OnAdFullScreenContentOpened += () => AdMobMainThread.Run(() =>
            {
                DebugAds.Log($"[AdMob][AppOpen] show_opened adUnitId={_adUnitId}");
                onShown?.Invoke();
            });
            ad.OnAdFullScreenContentClosed += () => AdMobMainThread.Run(() =>
            {
                DestroyAd(ad);
                DebugAds.Log($"[AdMob][AppOpen] show_closed adUnitId={_adUnitId}");
                onClosed?.Invoke();
            });
            ad.OnAdFullScreenContentFailed += err =>
            {
                var message = err.GetMessage();
                AdMobMainThread.Run(() =>
                {
                    DestroyAd(ad);
                    DebugAds.LogWarning($"[AdMob][AppOpen] show_failed adUnitId={_adUnitId} error={message}");
                    onFailed?.Invoke(message);
                });
            };
            ad.Show();
        }

        static void DestroyAd(AppOpenAd ad)
        {
            try
            {
                ad?.Destroy();
            }
            catch
            {
                // best effort
            }
        }
    }

    /// <summary>
    /// Provider-side load timeout. Realtime, so it keeps running while Time.timeScale == 0.
    /// </summary>
    internal static class AdMobLoadWatchdog
    {
        public const float TimeoutSeconds = 45f;

        public static void Start(Action onExpired) =>
            AdMobInitRunner.Instance.StartCoroutine(CoExpire(onExpired));

        static IEnumerator CoExpire(Action onExpired)
        {
            yield return new WaitForSecondsRealtime(TimeoutSeconds);
            onExpired();
        }
    }
}
#endif
