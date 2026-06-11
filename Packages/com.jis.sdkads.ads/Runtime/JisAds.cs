using System.Collections;
using System.Threading.Tasks;
using JisSDKAds.Ads.AppOpen;
using JisSDKAds.Ads.Integration;
using JisSDKAds.Ads.Resume;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Common;
using JisSDKAds.Core;
using JisSDKAds.Core.Interfaces;
using JisSDKAds.Core.Events;
using JisSDKAds.Core.Models;
using JisSDKAds.Firebase;
using UnityEngine;
using UnityEngine.Events;
namespace JisSDKAds.Ads
{
    /// <summary>
    /// Unified ads entry point: Core <see cref="AdManager"/> (single or sequential-tier inter/reward);
    /// App Open and Resume-on-foreground policies live here (not legacy unit managers).
    /// </summary>
    [DefaultExecutionOrder(-110)]
    public class JisAds : MonoBehaviour
    {
        public static JisAds Instance { get; private set; }
        [SerializeField] private JisSDKAdsSettings settings;
        [SerializeField] private bool useCoreForStandardFormats = true;
        [SerializeField] private bool autoInitializeOnStart = true;
        [SerializeField] private ResumeAdCoordinator resumeCoordinator;
        [SerializeField] private bool showAppOpenOnColdStart;
        [SerializeField] private float appOpenFirstShowDelayMs = 600f;
        [SerializeField] private float appOpenFirstShowWaitLoadTimeoutSec = 2.5f;
        [SerializeField] private float appOpenMinIntervalBetweenShowsSec = 20f;
        [Header("Banner restore after fullscreen ads")]
        [SerializeField] private bool restoreBannerAfterFullscreenAds = true;
        [SerializeField] private float bannerRestoreDelaySec = 0.35f;
        [SerializeField] private float bannerRestoreDebounceSec = 2f;
        [SerializeField] private int bannerRestoreMaxRetries = 4;
        [SerializeField] private float bannerRestoreRetryDelaySec = 3f;
        [Header("Fullscreen in-flight watchdog (recover stuck callbacks)")]
        [SerializeField] private bool enableFullscreenInFlightWatchdog = true;
        [SerializeField] private float fullscreenInFlightWatchdogSec = 60f;
        private AdManager _core;
        private AppOpenAdService _appOpen;
        private bool _isReady;
        private bool _isShowingAd;
        private bool _isRemoveAds;
        private float _appOpenUnscaledTime;
        private float _interstitialNextAllowedUnscaledTime;
        private bool _pendingInterstitialWasShown;
        private bool _pendingInterstitialHadLoadedAdAtShowRequest;
        private bool _pendingInterstitialIsTracking;
        private string _pendingInterstitialPlacement;
        private UnityAction _pendingInterstitialClosedCallback;
        private UnityAction _pendingInterstitialShowSuccessCallback;
        private UnityAction _pendingInterstitialShowFailCallback;
        private bool _interstitialCallbacksInFlight;
        private int _interstitialShowAttemptId;
        private Coroutine _interstitialWatchdogCoroutine;

        private bool _pendingRewardedWasShown;
        private bool _pendingRewardedRewardGranted;
        private bool _pendingRewardedHadLoadedAdAtShowRequest;
        private string _pendingRewardedPlacement;
        private UnityAction<bool> _pendingRewardedClosedCallback;
        private UnityAction _pendingRewardedRewardCallback;
        private UnityAction _pendingRewardedFailCallback;
        private bool _rewardedCallbacksInFlight;
        private int _rewardedShowAttemptId;
        private Coroutine _rewardedWatchdogCoroutine;
        private bool _standardFormatsPreloadedAfterRemoteConfig;
        private readonly int[] _preloadFailCounts = new int[3];
        private readonly bool[] _preloadRetryInFlight = new bool[3];
        private readonly Coroutine[] _preloadRetryCoroutines = new Coroutine[3];
        private const float SinglePreloadRetryDelay1Sec = 30f;
        private const float SinglePreloadRetryDelay2Sec = 60f;
        private const float SinglePreloadRetryDelaySteadySec = 120f;
        private bool _bannerWantsVisible;
        private bool _bannerAutoRefreshEnabled;
        private float _bannerAutoRefreshIntervalSec = BannerRefreshSettings.DefaultIntervalSeconds;
        private Coroutine _bannerAutoRefreshCoroutine;
        private Coroutine _bannerRestoreCoroutine;
        private Coroutine _bannerPauseRestoreCoroutine;
        const string BannerRestoreLogPrefix = "[JisAds][BannerRestore]";
        enum StandardAdPreloadFormat
        {
            Banner,
            Interstitial,
            Rewarded
        }
        public bool IsReady => _isReady;
        public bool UseCoreForStandardFormats => useCoreForStandardFormats && _core != null && _core.IsInitialized;
        public bool HasAppOpenSupport =>
            _core != null
            && _core.IsInitialized
            && _core.PrimaryProvider?.AppOpen is not NullAppOpenAd;
        public JisSDKAdsSettings Settings => settings;
        public AdManager Core => _core;
        public AppOpenAdService AppOpen => _appOpen;
        public ResumeAdCoordinator Resume => resumeCoordinator;
        public void Configure(JisSDKAdsSettings adsSettings, bool useCore, bool autoInit)
        {
            settings = adsSettings;
            useCoreForStandardFormats = useCore;
            autoInitializeOnStart = autoInit;
        }
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            JisSDKPersistence.DontDestroyUnlessUnderPersistentRoot(gameObject);
            if (settings == null)
            {
                Debug.LogError("[JisAds] JisSDKAdsSettings is not assigned.");
                return;
            }
            settings.ApplyRuntimeDebugSettings();
            RefreshRemoveAdsFromPersistence();
            EnsureResumeCoordinator();
            _appOpen = new AppOpenAdService(this, this);
            _appOpenUnscaledTime = Time.unscaledTime;
            AdLoadCoordinator.Instance.Configure(this);
            BindCoreCappingEvents();
            _appOpen.ConfigureColdStart(
                showAppOpenOnColdStart,
                appOpenFirstShowDelayMs,
                appOpenFirstShowWaitLoadTimeoutSec,
                appOpenMinIntervalBetweenShowsSec);
        }
        private void OnApplicationPause(bool pauseStatus)
        {
            resumeCoordinator?.OnApplicationPause(pauseStatus);

            if (!pauseStatus && _bannerWantsVisible && !IsShowingAnyAd())
                ScheduleBannerRestoreOnAppResume();
        }
        private void OnDestroy()
        {
            StopAllPreloadRetries();
            StopBannerAutoRefresh();
            CancelPendingBannerRestore();
            CancelPendingBannerPauseRestore();
            UnbindCoreCappingEvents();
        }
        private void Start()
        {
            if (autoInitializeOnStart)
                _ = InitializeAsync();
        }
        public async Task InitializeAsync(bool fetchRemoteConfig = true)
        {
            settings?.ApplyRuntimeDebugSettings();
            RefreshRemoveAdsFromPersistence();
            DebugAds.LogSdkInit("JisAds", "InitializeAsync", true, $"fetchRemoteConfig={fetchRemoteConfig}");
            AdMobSdkEarlyInitBridge.TryWarmUpFromSettings(settings);
            await InitializeFirebaseAsync(fetchRemoteConfig);
            ApplyRemoteAdInventoryFromConfig();
            InitializeCoreFlow();

            // Core-only readiness: wait a bit for Core to finish initializing.
            var waited = 0f;
            const float timeout = 15f;
            while ((_core == null || !_core.IsInitialized) && waited < timeout)
            {
                waited += Time.unscaledDeltaTime;
                await Task.Yield();
            }

            _isReady = _core != null && _core.IsInitialized;
            if (_isReady)
            {
                ApplyBannerRemoteConfig();
                if (ShouldPreloadAdsOnGameStart())
                {
                    PreloadStandardFormatsAfterRemoteConfig();
                    StartCoroutine(CoInitializeAppOpenAndResume());
                }
                else
                    DebugAds.Log("[JisAds] Startup ad preload skipped (settings or Remove Ads).");
            }

            DebugAds.LogSdkInit(
                "JisAds",
                "InitializeAsync complete",
                _isReady,
                _isReady ? null : "Core AdManager not initialized — check provider init logs above.");

            if (_isReady)
                AdsManager.Instance?.NotifyJisAdsCoreReady();
        }

        void PreloadStandardFormatsAfterRemoteConfig()
        {
            if (Application.isEditor)
                return;
            if (_standardFormatsPreloadedAfterRemoteConfig)
                return;
            if (!ShouldPreloadAdsOnGameStart())
            {
                _standardFormatsPreloadedAfterRemoteConfig = true;
                return;
            }

            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
                return;

            if (_core == null || !_core.IsInitialized)
                return;

            _standardFormatsPreloadedAfterRemoteConfig = true;
            PreloadBannerAd(isStartup: true);
            PreloadRewardedAd();
            StartCoroutine(CoDeferredInterstitialPreload());
        }

        IEnumerator CoDeferredInterstitialPreload()
        {
            var coordinator = AdLoadCoordinator.Instance;
            var maxWaitSec = coordinator.GetMaxDeferredInterstitialPreloadSeconds();
            var startedAt = Time.unscaledTime;
            var beganBecauseIdle = false;

            if (coordinator.IsPipelineIdle)
            {
                beganBecauseIdle = true;
            }
            else
            {
                void OnPipelineIdle() => beganBecauseIdle = true;
                coordinator.PipelineBecameIdle += OnPipelineIdle;
                try
                {
                    while (!beganBecauseIdle)
                    {
                        if (maxWaitSec > 0f && Time.unscaledTime - startedAt >= maxWaitSec)
                            break;

                        yield return null;
                    }
                }
                finally
                {
                    coordinator.PipelineBecameIdle -= OnPipelineIdle;
                }
            }

            if (!ShouldPreloadAdsOnGameStart() || !UseCoreForStandardFormats)
                yield break;

            var waited = Time.unscaledTime - startedAt;
            if (beganBecauseIdle)
            {
                DebugAds.Log(
                    $"[JisAds] Interstitial preload starting after pipeline idle ({waited:0.#}s, max={maxWaitSec:0.#}s).");
            }
            else
            {
                DebugAds.Log(
                    $"[JisAds] Interstitial preload starting after max defer wait ({waited:0.#}s, pipeline still busy).");
            }

            PreloadInterstitialAd();
        }

        void PreloadBannerAd(bool isStartup = false)
        {
            if (!UseCoreForStandardFormats || _core?.PrimaryProvider?.Banner == null)
                return;

            var preserveVisible = _bannerWantsVisible;
            _core.PrimaryProvider.Banner.Load(
                onLoaded: () =>
                {
                    OnPreloadSucceeded(StandardAdPreloadFormat.Banner);
                    DebugAds.Log("[JisAds] Preload Banner: loaded");
                    if (isStartup)
                        TryShowBannerOnStartIfConfigured();
                    else if (preserveVisible)
                        ShowBannerAds();
                },
                onFailed: err =>
                {
                    DebugAds.LogWarning($"[JisAds] Preload Banner failed: {err}");
                    HandlePreloadFailed(StandardAdPreloadFormat.Banner);
                });
        }

        void PreloadInterstitialAd()
        {
            if (!UseCoreForStandardFormats || _core?.PrimaryProvider?.Interstitial == null)
                return;

            _core.PrimaryProvider.Interstitial.Load(
                onLoaded: () =>
                {
                    OnPreloadSucceeded(StandardAdPreloadFormat.Interstitial);
                    DebugAds.Log("[JisAds] Preload Interstitial: loaded");
                },
                onFailed: err =>
                {
                    DebugAds.LogWarning($"[JisAds] Preload Interstitial failed: {err}");
                    HandlePreloadFailed(StandardAdPreloadFormat.Interstitial);
                });
        }

        void PreloadRewardedAd()
        {
            if (!UseCoreForStandardFormats || _core?.PrimaryProvider?.Rewarded == null)
                return;

            _core.PrimaryProvider.Rewarded.Load(
                onLoaded: () =>
                {
                    OnPreloadSucceeded(StandardAdPreloadFormat.Rewarded);
                    DebugAds.Log("[JisAds] Preload Rewarded: loaded");
                },
                onFailed: err =>
                {
                    DebugAds.LogWarning($"[JisAds] Preload Rewarded failed: {err}");
                    HandlePreloadFailed(StandardAdPreloadFormat.Rewarded);
                });
        }

        static int PreloadFormatIndex(StandardAdPreloadFormat format) => (int)format;

        bool IsSingleInventoryForFormat(StandardAdPreloadFormat format)
        {
            var profile = settings?.GetActiveProfile();
            var setup = profile?.sdkSetup;
            if (setup?.admobAdsSetup == null)
                return true;

            var admob = setup.admobAdsSetup;
            return format switch
            {
                StandardAdPreloadFormat.Banner => true,
                StandardAdPreloadFormat.Interstitial =>
                    setup.interstitialAdsMediationType != AdsMediationType.ADMOB
                    || !admob.InterstitialTierConfig.enableSequentialLadder,
                StandardAdPreloadFormat.Rewarded =>
                    setup.rewardedAdsMediationType != AdsMediationType.ADMOB
                    || !admob.RewardedTierConfig.enableSequentialLadder,
                _ => true
            };
        }

        void OnPreloadSucceeded(StandardAdPreloadFormat format)
        {
            var idx = PreloadFormatIndex(format);
            _preloadFailCounts[idx] = 0;
            StopPreloadRetryCoroutine(format);
        }

        void HandlePreloadFailed(StandardAdPreloadFormat format)
        {
            if (!ShouldPreloadAdsOnGameStart())
                return;

            var idx = PreloadFormatIndex(format);
            if (_preloadRetryInFlight[idx])
                return;

            _preloadFailCounts[idx]++;

            var single = IsSingleInventoryForFormat(format);
            var delay = GetPreloadRetryDelaySeconds(single, _preloadFailCounts[idx]);
            DebugAds.Log(
                $"[JisAds] Preload {format} retry #{_preloadFailCounts[idx]} in {delay:0.#}s " +
                $"(mode={(single ? "single" : "sequential")})");
            _preloadRetryCoroutines[idx] = StartCoroutine(CoDelayedPreloadRetry(format, delay));
        }

        static float GetPreloadRetryDelaySeconds(bool singleInventory, int failCount)
        {
            if (singleInventory)
            {
                return failCount switch
                {
                    1 => SinglePreloadRetryDelay1Sec,
                    2 => SinglePreloadRetryDelay2Sec,
                    _ => SinglePreloadRetryDelaySteadySec
                };
            }

            return failCount switch
            {
                1 => 2f,
                2 => 5f,
                3 => 10f,
                _ => SinglePreloadRetryDelaySteadySec
            };
        }

        IEnumerator CoDelayedPreloadRetry(StandardAdPreloadFormat format, float delay)
        {
            var idx = PreloadFormatIndex(format);
            _preloadRetryInFlight[idx] = true;
            yield return new WaitForSecondsRealtime(delay);
            _preloadRetryInFlight[idx] = false;
            _preloadRetryCoroutines[idx] = null;

            if (!ShouldPreloadAdsOnGameStart())
                yield break;

            switch (format)
            {
                case StandardAdPreloadFormat.Banner:
                    PreloadBannerAd();
                    break;
                case StandardAdPreloadFormat.Interstitial:
                    PreloadInterstitialAd();
                    break;
                case StandardAdPreloadFormat.Rewarded:
                    PreloadRewardedAd();
                    break;
            }
        }

        void StopPreloadRetryCoroutine(StandardAdPreloadFormat format)
        {
            var idx = PreloadFormatIndex(format);
            if (_preloadRetryCoroutines[idx] != null)
            {
                StopCoroutine(_preloadRetryCoroutines[idx]);
                _preloadRetryCoroutines[idx] = null;
            }

            _preloadRetryInFlight[idx] = false;
        }

        void StopAllPreloadRetries()
        {
            StopPreloadRetryCoroutine(StandardAdPreloadFormat.Banner);
            StopPreloadRetryCoroutine(StandardAdPreloadFormat.Interstitial);
            StopPreloadRetryCoroutine(StandardAdPreloadFormat.Rewarded);
        }

        public void ApplyBannerRemoteConfig()
        {
            var setup = settings?.GetActiveProfile()?.sdkSetup;
            BannerRefreshSettings.Resolve(setup, out _bannerAutoRefreshEnabled, out _bannerAutoRefreshIntervalSec);
            DebugAds.Log(
                $"[JisAds] Banner auto-refresh={_bannerAutoRefreshEnabled} interval={_bannerAutoRefreshIntervalSec:0.#}s");
            RestartBannerAutoRefresh();
        }

        void RestartBannerAutoRefresh()
        {
            StopBannerAutoRefresh();
            if (!_bannerAutoRefreshEnabled || !CanShowAds() || !UseCoreForStandardFormats)
                return;

            _bannerAutoRefreshCoroutine = StartCoroutine(CoBannerAutoRefresh());
        }

        void StopBannerAutoRefresh()
        {
            if (_bannerAutoRefreshCoroutine != null)
            {
                StopCoroutine(_bannerAutoRefreshCoroutine);
                _bannerAutoRefreshCoroutine = null;
            }
        }

        IEnumerator CoBannerAutoRefresh()
        {
            while (_bannerAutoRefreshEnabled && CanShowAds() && UseCoreForStandardFormats)
            {
                yield return new WaitForSecondsRealtime(_bannerAutoRefreshIntervalSec);
                if (!_bannerAutoRefreshEnabled || !CanShowAds())
                    yield break;

                if (!_bannerWantsVisible)
                    continue;

                // Never destroy/reload the banner while a fullscreen ad is on screen — it races
                // with the hide/restore flow and can leave the banner in a bad state.
                if (IsShowingAnyAd())
                    continue;

                RefreshVisibleBanner();
            }
        }

        void RefreshVisibleBanner()
        {
            if (!UseCoreForStandardFormats || _core?.PrimaryProvider?.Banner == null)
                return;

            DebugAds.Log("[JisAds] Banner auto-refresh reload");
            _core.PrimaryProvider.Banner.Load(
                onLoaded: () =>
                {
                    if (_bannerWantsVisible)
                        _core.ShowBanner(
                            onShown: () => DebugAds.Log("[JisAds] Banner auto-refresh shown"),
                            onFailed: err => DebugAds.LogWarning($"[JisAds] Banner auto-refresh show failed: {err}"));
                },
                onFailed: err => DebugAds.LogWarning($"[JisAds] Banner auto-refresh load failed: {err}"));
        }
        IEnumerator CoInitializeAppOpenAndResume()
        {
            if (_core != null)
            {
                var waited = 0f;
                const float timeout = 15f;
                while (!_core.IsInitialized && waited < timeout)
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            resumeCoordinator?.Bind(this, _appOpen);
            resumeCoordinator?.ApplyRemoteConfig();
            _appOpen?.BeginAfterSdkReady();
        }
        public void RefreshAppOpenAndResumeRemoteConfig()
        {
            _appOpen?.ApplyRemoteConfig();
            resumeCoordinator?.ApplyRemoteConfig();
            ApplyBannerRemoteConfig();
            RecoverStandardPreloadsAfterRemoteConfigRefresh();
        }

        /// <summary>
        /// When Remote Config refreshes (e.g. sequential-tier ad unit IDs arrive late), re-apply the
        /// resolved IDs and re-arm interstitial/rewarded preloads that may have permanently given up
        /// earlier (the tiered preload retry stops after a few "no ad unit configured" failures).
        /// </summary>
        void RecoverStandardPreloadsAfterRemoteConfigRefresh()
        {
            if (!UseCoreForStandardFormats || !CanShowAds())
                return;

            // Push the latest RC-resolved unit IDs onto the (shared) sequential-tier config instances.
            ApplyRemoteAdInventoryFromConfig();

            // Clear the "stopped after N failures" state so late-arriving inventory can load again.
            _preloadFailCounts[PreloadFormatIndex(StandardAdPreloadFormat.Interstitial)] = 0;
            _preloadFailCounts[PreloadFormatIndex(StandardAdPreloadFormat.Rewarded)] = 0;

            RequestInterstitialLoadIfNeeded();
            RequestRewardedLoadIfNeeded();
        }
        public async Task InitializeFirebaseAsync(bool fetchRemoteConfig = true)
        {
            if (!fetchRemoteConfig)
                return;

            // Core-only: rely on FirebaseManager directly instead of AdsManager legacy bootstrap.
            if (FirebaseManager.Instance == null)
            {
                var go = new GameObject("JisSDKAds_FirebaseManager");
                go.AddComponent<FirebaseManager>();
            }

            await FirebaseManager.Instance.FetchRemoteConfigAsync();
        }
        void EnsureResumeCoordinator()
        {
            if (resumeCoordinator != null)
                return;
            resumeCoordinator = GetComponent<ResumeAdCoordinator>();
            if (resumeCoordinator == null)
                resumeCoordinator = gameObject.AddComponent<ResumeAdCoordinator>();
        }
        void ApplyRemoteAdInventoryFromConfig()
        {
            if (settings == null)
                return;

            var profile = settings.GetActiveProfile();
            if (profile?.sdkSetup == null)
                return;

            AdInventoryRemoteConfigResolver.ApplyInventoryModesFromRemoteConfig(profile.sdkSetup);
            SequentialTierRemoteConfigResolver.ApplyResolvedIdsToAdmobSetup(profile);
        }

        void InitializeCoreFlow()
        {
            var profile = settings.GetActiveProfile();
            if (profile == null) return;
            _core = FindFirstObjectByType<AdManager>();
            if (_core == null)
            {
                var go = new GameObject("JisAds_Core_AdManager");
                go.transform.SetParent(transform);
                _core = go.AddComponent<AdManager>();
            }
            var providerId = profile.ProviderId;
            _core.ConfigureSingleMediation(providerId, settings.singleMediationOnly);
            var providerConfig = ProviderConfigFactory.CreateFromSdkSetup(profile);
            if (providerConfig == null)
            {
                Debug.LogWarning($"[JisAds] No Core provider for {profile.mediation}.");
                useCoreForStandardFormats = false;
                return;
            }
            var provider = providerConfig.CreateProvider();
            provider = DecorateSequentialAdsIfEnabled(provider, profile);
            _core.RegisterProvider(providerConfig.ProviderId, provider);
            _core.Initialize(
                onSuccess: () => DebugAds.LogSdkInit("JisAds", "Core AdManager", true),
                onFailure: err =>
                {
                    DebugAds.LogSdkInit("JisAds", "Core AdManager", false, err);
                    useCoreForStandardFormats = false;
                });
        }

        IAdService DecorateSequentialAdsIfEnabled(IAdService provider, PlatformAdsProfile profile)
        {
#if UNITY_AD_ADMOB
            if (profile?.mediation != AdsMediationType.ADMOB)
                return provider;

            var admob = profile.sdkSetup?.admobAdsSetup;
            var interstitialConfig = admob?.InterstitialTierConfig;
            var rewardedConfig = admob?.RewardedTierConfig;

            var decorated = AdMobSequentialTierReflection.TryDecorate(
                provider,
                this,
                interstitialConfig,
                rewardedConfig);

            if (!ReferenceEquals(decorated, provider))
            {
                if (interstitialConfig != null && interstitialConfig.enableSequentialLadder)
                    DebugAds.Log("[JisAds] Interstitial uses SequentialTier ladder (Premium→Fill) via Core.");
                if (rewardedConfig != null && rewardedConfig.enableSequentialLadder)
                    DebugAds.Log("[JisAds] Rewarded uses SequentialTier ladder (Premium→Fill) via Core.");
            }

            return decorated;
#else
            return provider;
#endif
        }
        #region State helpers
        public bool CanShowAds() => !_isRemoveAds;
        public bool IsShowingAnyAd() => _isShowingAd;
        public void SetAdsShowingState(bool isShowing) => _isShowingAd = isShowing;

        public void SetRemoveAds(bool isRemove)
        {
            if (_isRemoveAds == isRemove)
                return;

            _isRemoveAds = isRemove;
            PlayerPrefs.SetInt(Keys.key_local_remove_ads, isRemove ? 1 : 0);
            PlayerPrefs.Save();

            if (_isRemoveAds)
                ApplyRemoveAdsSideEffects();

            var legacy = AdsManager.Instance;
            if (legacy != null && legacy.IsRemoveAds != isRemove)
                legacy.SetRemoveAds(isRemove);
        }

        public bool IsRemoveAds => _isRemoveAds;

        void RefreshRemoveAdsFromPersistence() =>
            _isRemoveAds = PlayerPrefs.GetInt(Keys.key_local_remove_ads, 0) == 1;

        bool ShouldPreloadAdsOnGameStart()
        {
            if (settings != null && !settings.preloadAdsOnGameStart)
                return false;
            if (settings != null && settings.skipStartupAdLoadWhenRemoveAds && _isRemoveAds)
                return false;
            return true;
        }

        void ApplyRemoveAdsSideEffects()
        {
            _standardFormatsPreloadedAfterRemoteConfig = true;
            StopAllPreloadRetries();
            _bannerWantsVisible = false;
            StopBannerAutoRefresh();
            HideBannerAds();
            DebugAds.Log("[JisAds] Remove Ads active — startup loads suppressed; banner/interstitial show blocked.");
        }
        #endregion
        #region Standard formats
        public void ShowInterstitial(
            UnityAction closedCallback = null,
            UnityAction showSuccessCallback = null,
            UnityAction showFailCallback = null,
            bool isTracking = true,
            bool isSkipCapping = false) =>
            ShowInterstitial("", closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
        public void ShowInterstitial(
            string interstitialPlacement,
            UnityAction closedCallback = null,
            UnityAction showSuccessCallback = null,
            UnityAction showFailCallback = null,
            bool isTracking = true,
            bool isSkipCapping = false)
        {
            if (_interstitialCallbacksInFlight)
            {
                DebugAds.LogWarning("[JisAds] Interstitial show blocked: previous interstitial still in-flight.");
                showFailCallback?.Invoke();
                return;
            }

            if (!isSkipCapping && ShouldBlockInterstitialByCapping(out var remainingSeconds, out var reason))
            {
                DebugAds.Log($"[JisAds] Interstitial skipped due to capping ({reason}). Remaining={remainingSeconds:0.##}s");
                closedCallback?.Invoke();
                return;
            }

            // For platforms that pause the game loop during interstitial, only mark "show success"
            // after we know the ad actually opened; then invoke success together with close.
            _interstitialCallbacksInFlight = true;
            _interstitialShowAttemptId++;
            StartInterstitialInFlightWatchdog(_interstitialShowAttemptId);
            _pendingInterstitialWasShown = false;
            _pendingInterstitialHadLoadedAdAtShowRequest = IsInterstitialAdLoaded();
            _pendingInterstitialIsTracking = isTracking;
            _pendingInterstitialPlacement = interstitialPlacement;
            _pendingInterstitialClosedCallback = closedCallback;
            _pendingInterstitialShowSuccessCallback = showSuccessCallback;
            _pendingInterstitialShowFailCallback = showFailCallback;

            TrackPendingInterstitialClick();

            if (UseCoreForStandardFormats)
            {
                SetAdsShowingState(true);
                HideBannerForFullscreenAd("interstitial");
                _core.ShowInterstitial(
                    onClosed: () =>
                    {
                        ConsumePendingInterstitialCallbacksOnClose();
                    },
                    onFailed: err =>
                    {
                        Debug.LogWarning($"[JisAds] Core interstitial failed: {err}");
                        TrackPendingInterstitialShowFailure(isTracking);
                        ConsumePendingInterstitialCallbacksOnFail();
                    });
                return;
            }
            Debug.LogWarning("[JisAds] Legacy interstitial is removed. Enable Core AdManager.");
            TrackPendingInterstitialShowFailure(isTracking);
            ConsumePendingInterstitialCallbacksOnFail();
        }

        void TrackPendingInterstitialShowFailure(bool isTracking)
        {
            if (!isTracking)
                return;

            var tracker = AdsTracker.Instance;
            if (tracker == null)
            {
                DebugAds.LogWarning("[JisAds] AdsTracker.Instance is null. Skipping interstitial failure tracking.");
                return;
            }

            if (!_pendingInterstitialHadLoadedAdAtShowRequest)
                tracker.TrackAdsInterstitial_ShowFailByLoad();
            else
                tracker.TrackAdsInterstitial_ShowFail();
        }

        void TrackPendingInterstitialClick()
        {
            if (!_pendingInterstitialIsTracking)
                return;

            var tracker = AdsTracker.Instance;
            if (tracker == null)
            {
                DebugAds.LogWarning("[JisAds] AdsTracker.Instance is null. Skipping interstitial click tracking.");
                return;
            }

            tracker.TrackAdsInterstitial_ClickOnButton(_pendingInterstitialPlacement);
        }

        void TrackPendingInterstitialShowSuccess()
        {
            if (!_pendingInterstitialIsTracking)
                return;

            var tracker = AdsTracker.Instance;
            if (tracker == null)
            {
                DebugAds.LogWarning("[JisAds] AdsTracker.Instance is null. Skipping interstitial show tracking.");
                return;
            }

            tracker.TrackAdsInterstitial_ShowSuccess(_pendingInterstitialPlacement);
        }

        void ConsumePendingInterstitialCallbacksOnClose()
        {
            if (!_interstitialCallbacksInFlight)
                return;

            var didShow = _pendingInterstitialWasShown;
            var onSuccess = _pendingInterstitialShowSuccessCallback;
            var onClosed = _pendingInterstitialClosedCallback;
            ClearPendingInterstitialCallbacks();

            if (didShow)
                onSuccess?.Invoke();
            onClosed?.Invoke();
            ScheduleBannerRestoreAfterFullscreenAd("interstitial");
        }

        void ConsumePendingInterstitialCallbacksOnFail()
        {
            if (!_interstitialCallbacksInFlight)
                return;

            var onFail = _pendingInterstitialShowFailCallback;
            ClearPendingInterstitialCallbacks();
            onFail?.Invoke();
            ScheduleBannerRestoreAfterFullscreenAd("interstitial");
        }

        void ClearPendingInterstitialCallbacks()
        {
            _pendingInterstitialWasShown = false;
            _pendingInterstitialHadLoadedAdAtShowRequest = false;
            _pendingInterstitialIsTracking = false;
            _pendingInterstitialPlacement = null;
            _pendingInterstitialClosedCallback = null;
            _pendingInterstitialShowSuccessCallback = null;
            _pendingInterstitialShowFailCallback = null;
            _interstitialCallbacksInFlight = false;
            StopInterstitialInFlightWatchdog();
            SetAdsShowingState(false);
        }

        bool ShouldBlockInterstitialByCapping(out float remainingSeconds, out string reason)
        {
            remainingSeconds = 0f;
            reason = "none";

            // Type 1: time since app open
            var fromOpen = GetCappingFromAppOpenSeconds();
            if (fromOpen > 0f)
            {
                var elapsed = Time.unscaledTime - _appOpenUnscaledTime;
                if (elapsed < fromOpen)
                {
                    remainingSeconds = Mathf.Max(0f, fromOpen - elapsed);
                    reason = "from_app_open";
                    return true;
                }
            }

            // Type 2: time between successful interstitial shows (also reset by rewarded watch)
            var between = GetCappingBetweenShowsSeconds();
            if (between > 0f && !IsCoreInterstitialCooldownFinished())
            {
                remainingSeconds = GetCoreInterstitialCooldownRemainingSeconds();
                reason = "between_shows";
                return true;
            }

            return false;
        }

        float GetCappingFromAppOpenSeconds()
        {
            if (FirebaseManager.Instance == null)
                return 0f;
            return (float)FirebaseManager.Instance.GetConfigDouble(Keys.key_remote_interstitial_capping_from_app_open_seconds);
        }

        float GetCappingBetweenShowsSeconds()
        {
            if (FirebaseManager.Instance == null)
                return 0f;
            return (float)FirebaseManager.Instance.GetConfigDouble(Keys.key_remote_interstitial_capping_between_shows_seconds);
        }

        bool IsCoreInterstitialCooldownFinished() =>
            Time.unscaledTime >= _interstitialNextAllowedUnscaledTime;

        float GetCoreInterstitialCooldownRemainingSeconds() =>
            Mathf.Max(0f, _interstitialNextAllowedUnscaledTime - Time.unscaledTime);

        void ResetCoreInterstitialBetweenShowsCooldown()
        {
            var seconds = GetCappingBetweenShowsSeconds();
            if (seconds <= 0f)
                return;
            _interstitialNextAllowedUnscaledTime = Time.unscaledTime + seconds;
        }

        void BindCoreCappingEvents()
        {
            AdEvents.OnInterstitialShown += OnCoreInterstitialShown;
            AdEvents.OnRewardEarned += OnCoreRewardEarned;
        }

        void UnbindCoreCappingEvents()
        {
            AdEvents.OnInterstitialShown -= OnCoreInterstitialShown;
            AdEvents.OnRewardEarned -= OnCoreRewardEarned;
        }

        void OnCoreInterstitialShown(AdFormat format)
        {
            if (format != AdFormat.Interstitial)
                return;
            // Only mark "shown" for the current in-flight show attempt.
            if (_interstitialCallbacksInFlight)
            {
                _pendingInterstitialWasShown = true;
                TrackPendingInterstitialShowSuccess();
            }
            ResetCoreInterstitialBetweenShowsCooldown();
        }

        void OnCoreRewardEarned(AdFormat format)
        {
            if (format != AdFormat.Rewarded)
                return;
            // Requirement: watching a rewarded ad resets type-2 timer as well.
            ResetCoreInterstitialBetweenShowsCooldown();
        }
        /// <summary>Interstitial for foreground resume — bypasses legacy gameplay cooldown.</summary>
        public void ShowInterstitialForResume(System.Action onClosed = null, System.Action<string> onFailed = null)
        {
            if (UseCoreForStandardFormats)
            {
                _core.ShowInterstitial(onClosed, onFailed);
                return;
            }
            onFailed?.Invoke("Resume interstitial unavailable — Core AdManager not initialized.");
        }
        public void ShowRewardVideo(
            string rewardedPlacement,
            UnityAction successCallback,
            UnityAction<bool> closedCallback = null,
            UnityAction failedCallback = null)
        {
            if (_rewardedCallbacksInFlight)
            {
                DebugAds.LogWarning("[JisAds] Rewarded show blocked: previous rewarded still in-flight.");
                failedCallback?.Invoke();
                return;
            }

            _rewardedCallbacksInFlight = true;
            _rewardedShowAttemptId++;
            StartRewardedInFlightWatchdog(_rewardedShowAttemptId);
            _pendingRewardedWasShown = false;
            _pendingRewardedRewardGranted = false;
            _pendingRewardedHadLoadedAdAtShowRequest = IsRewardedVideoLoaded();
            _pendingRewardedPlacement = rewardedPlacement;
            _pendingRewardedRewardCallback = successCallback;
            _pendingRewardedClosedCallback = closedCallback;
            _pendingRewardedFailCallback = failedCallback;
            SetAdsShowingState(true);

            TrackRewardedClick();

            if (UseCoreForStandardFormats)
            {
                if (!IsRewardedVideoLoaded())
                    AdLoadCoordinator.Instance.PrepareUrgentRewarded();

                HideBannerForFullscreenAd("rewarded");
                _core.ShowRewarded(
                    onRewardEarned: ConsumePendingRewardedCallbacksOnRewardGranted,
                    onClosed: () => ConsumePendingRewardedCallbacksOnClose(_pendingRewardedRewardGranted),
                    onFailed: err =>
                    {
                        Debug.LogWarning($"[JisAds] Core rewarded failed: {err}");
                        TrackPendingRewardedShowFailure();
                        ConsumePendingRewardedCallbacksOnFail();
                    });
                return;
            }
            Debug.LogWarning("[JisAds] Legacy rewarded is removed. Enable Core AdManager.");
            TrackPendingRewardedShowFailure();
            ConsumePendingRewardedCallbacksOnFail();
        }

        void TrackPendingRewardedShowFailure()
        {
            var tracker = AdsTracker.Instance;
            if (tracker == null)
            {
                DebugAds.LogWarning("[JisAds] AdsTracker.Instance is null. Skipping rewarded failure tracking.");
                return;
            }

            if (!_pendingRewardedHadLoadedAdAtShowRequest)
                tracker.TrackAdsReward_ShowFailByLoad();
            else
                tracker.TrackAdsReward_ShowFail();
        }

        void TrackRewardedClick()
        {
            var tracker = AdsTracker.Instance;
            if (tracker == null)
            {
                DebugAds.LogWarning("[JisAds] AdsTracker.Instance is null. Skipping rewarded click tracking.");
                return;
            }

            tracker.TrackAdsReward_ClickOnButton();
        }

        void ConsumePendingRewardedCallbacksOnRewardGranted()
        {
            if (!_rewardedCallbacksInFlight || _pendingRewardedRewardGranted)
                return;
            _pendingRewardedRewardGranted = true;
            AdsTracker.Instance?.TrackAdsReward_ShowCompleted(_pendingRewardedPlacement);
            _pendingRewardedRewardCallback?.Invoke();
        }

        void ConsumePendingRewardedCallbacksOnClose(bool closedOk)
        {
            if (!_rewardedCallbacksInFlight)
                return;

            var onClosed = _pendingRewardedClosedCallback;
            ClearPendingRewardedCallbacks();
            onClosed?.Invoke(closedOk);
            ScheduleBannerRestoreAfterFullscreenAd("rewarded");
        }

        void ConsumePendingRewardedCallbacksOnFail()
        {
            if (!_rewardedCallbacksInFlight)
                return;

            var onFail = _pendingRewardedFailCallback;
            ClearPendingRewardedCallbacks();
            onFail?.Invoke();
            ScheduleBannerRestoreAfterFullscreenAd("rewarded");
        }

        void ClearPendingRewardedCallbacks()
        {
            _pendingRewardedWasShown = false;
            _pendingRewardedRewardGranted = false;
            _pendingRewardedHadLoadedAdAtShowRequest = false;
            _pendingRewardedPlacement = null;
            _pendingRewardedClosedCallback = null;
            _pendingRewardedRewardCallback = null;
            _pendingRewardedFailCallback = null;
            _rewardedCallbacksInFlight = false;
            StopRewardedInFlightWatchdog();
            SetAdsShowingState(false);
        }

        void StartInterstitialInFlightWatchdog(int attemptId)
        {
            StopInterstitialInFlightWatchdog();
            if (!enableFullscreenInFlightWatchdog || fullscreenInFlightWatchdogSec <= 0f)
                return;
            _interstitialWatchdogCoroutine = StartCoroutine(CoInterstitialInFlightWatchdog(attemptId));
        }

        void StopInterstitialInFlightWatchdog()
        {
            if (_interstitialWatchdogCoroutine == null)
                return;
            StopCoroutine(_interstitialWatchdogCoroutine);
            _interstitialWatchdogCoroutine = null;
        }

        IEnumerator CoInterstitialInFlightWatchdog(int attemptId)
        {
            yield return new WaitForSecondsRealtime(fullscreenInFlightWatchdogSec);
            _interstitialWatchdogCoroutine = null;

            if (!_interstitialCallbacksInFlight || attemptId != _interstitialShowAttemptId)
                yield break;

            DebugAds.LogWarning(
                "[JisAds] Interstitial in-flight watchdog fired — close callback never arrived. Recovering so future shows aren't blocked.");
            ConsumePendingInterstitialCallbacksOnFail();
            RequestInterstitialLoadIfNeeded();
        }

        void StartRewardedInFlightWatchdog(int attemptId)
        {
            StopRewardedInFlightWatchdog();
            if (!enableFullscreenInFlightWatchdog || fullscreenInFlightWatchdogSec <= 0f)
                return;
            _rewardedWatchdogCoroutine = StartCoroutine(CoRewardedInFlightWatchdog(attemptId));
        }

        void StopRewardedInFlightWatchdog()
        {
            if (_rewardedWatchdogCoroutine == null)
                return;
            StopCoroutine(_rewardedWatchdogCoroutine);
            _rewardedWatchdogCoroutine = null;
        }

        IEnumerator CoRewardedInFlightWatchdog(int attemptId)
        {
            yield return new WaitForSecondsRealtime(fullscreenInFlightWatchdogSec);
            _rewardedWatchdogCoroutine = null;

            if (!_rewardedCallbacksInFlight || attemptId != _rewardedShowAttemptId)
                yield break;

            DebugAds.LogWarning(
                "[JisAds] Rewarded in-flight watchdog fired — close callback never arrived. Recovering so future shows aren't blocked.");
            ConsumePendingRewardedCallbacksOnFail();
            RequestRewardedLoadIfNeeded();
        }

        public void ShowBannerAds()
        {
            if (!CanShowAds())
            {
                DebugAds.Log("[JisAds] ShowBannerAds skipped — remove ads active.");
                return;
            }

            if (UseCoreForStandardFormats)
            {
                _bannerWantsVisible = true;
                _core.ShowBanner(
                    onShown: () => DebugAds.Log("[JisAds] Banner shown"),
                    onFailed: err => Debug.LogWarning($"[JisAds] Banner show failed: {err}"));
                RestartBannerAutoRefresh();
                return;
            }

            Debug.LogWarning("[JisAds] Banner unavailable — Core AdManager not initialized. Check AdMob init logs.");
        }

        void TryShowBannerOnStartIfConfigured()
        {
            var setup = settings?.GetActiveProfile()?.sdkSetup;
            if (setup == null || !setup.isBannerShowingOnStart)
                return;

            if (!CanShowAds() || !UseCoreForStandardFormats)
                return;

            DebugAds.Log("[JisAds] isBannerShowingOnStart=true — showing banner");
            ShowBannerAds();
        }

        public void HideBannerAds()
        {
            _bannerWantsVisible = false;
            CancelPendingBannerRestore();
            CancelPendingBannerPauseRestore();
            if (UseCoreForStandardFormats)
            {
                _core.HideBanner();
                StopBannerAutoRefresh();
                return;
            }
        }

        /// <summary>
        /// Hides the native banner before a fullscreen ad without clearing <see cref="_bannerWantsVisible"/>.
        /// </summary>
        public void HideBannerForFullscreenAd(string reason)
        {
            if (!restoreBannerAfterFullscreenAds || !CanShowAds() || !UseCoreForStandardFormats)
                return;
            if (!_bannerWantsVisible)
                return;

            DebugAds.Log($"{BannerRestoreLogPrefix} hide reason={reason}");
            _core.HideBanner();
        }

        /// <summary>
        /// Debounced destroy+recreate+show after fullscreen ads when the banner should stay visible.
        /// </summary>
        public void ScheduleBannerRestoreAfterFullscreenAd(string reason)
        {
            if (!restoreBannerAfterFullscreenAds || !CanShowAds() || !UseCoreForStandardFormats)
                return;
            if (!_bannerWantsVisible)
                return;

            CancelPendingBannerRestore();
            _bannerRestoreCoroutine = StartCoroutine(CoRestoreBannerAfterFullscreenAd(reason));
        }

        void ScheduleBannerRestoreOnAppResume()
        {
            if (!restoreBannerAfterFullscreenAds || !CanShowAds() || !UseCoreForStandardFormats)
                return;
            if (!_bannerWantsVisible || IsShowingAnyAd())
                return;

            CancelPendingBannerPauseRestore();
            _bannerPauseRestoreCoroutine = StartCoroutine(CoDebouncedBannerRestoreOnAppResume());
        }

        void CancelPendingBannerRestore()
        {
            if (_bannerRestoreCoroutine == null)
                return;

            StopCoroutine(_bannerRestoreCoroutine);
            _bannerRestoreCoroutine = null;
        }

        void CancelPendingBannerPauseRestore()
        {
            if (_bannerPauseRestoreCoroutine == null)
                return;

            StopCoroutine(_bannerPauseRestoreCoroutine);
            _bannerPauseRestoreCoroutine = null;
        }

        IEnumerator CoRestoreBannerAfterFullscreenAd(string reason)
        {
            if (bannerRestoreDelaySec > 0f)
                yield return new WaitForSecondsRealtime(bannerRestoreDelaySec);

            // Wait (bounded) until no fullscreen ad is on screen instead of silently dropping the restore.
            var fullscreenWait = 0f;
            const float maxFullscreenWaitSec = 30f;
            while (IsShowingAnyAd() && fullscreenWait < maxFullscreenWaitSec)
            {
                if (!BannerRestorePreconditionsMet())
                {
                    _bannerRestoreCoroutine = null;
                    yield break;
                }
                fullscreenWait += 0.2f;
                yield return new WaitForSecondsRealtime(0.2f);
            }

            var maxAttempts = Mathf.Max(1, bannerRestoreMaxRetries);
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (!BannerRestorePreconditionsMet() || IsShowingAnyAd())
                {
                    _bannerRestoreCoroutine = null;
                    yield break;
                }

                DebugAds.Log($"{BannerRestoreLogPrefix} restore start reason={reason} attempt={attempt}/{maxAttempts}");

                var done = false;
                var succeeded = false;
                _core.PrimaryProvider.Banner.Load(
                    onLoaded: () =>
                    {
                        if (!_bannerWantsVisible || !CanShowAds())
                        {
                            done = true;
                            succeeded = true; // intent changed — stop retrying.
                            return;
                        }

                        _core.ShowBanner(
                            onShown: () =>
                            {
                                DebugAds.Log($"{BannerRestoreLogPrefix} restore shown reason={reason}");
                                succeeded = true;
                                done = true;
                            },
                            onFailed: err =>
                            {
                                DebugAds.LogWarning($"{BannerRestoreLogPrefix} restore show failed reason={reason} error={err}");
                                done = true;
                            });
                    },
                    onFailed: err =>
                    {
                        DebugAds.LogWarning($"{BannerRestoreLogPrefix} restore load failed reason={reason} attempt={attempt} error={err}");
                        done = true;
                    });

                var waited = 0f;
                const float perAttemptTimeoutSec = 15f;
                while (!done && waited < perAttemptTimeoutSec)
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (succeeded)
                {
                    _bannerRestoreCoroutine = null;
                    yield break;
                }

                if (attempt < maxAttempts)
                    yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, bannerRestoreRetryDelaySec));
            }

            DebugAds.LogWarning($"{BannerRestoreLogPrefix} restore failed after {maxAttempts} attempts reason={reason}");
            _bannerRestoreCoroutine = null;
        }

        bool BannerRestorePreconditionsMet() =>
            restoreBannerAfterFullscreenAds && CanShowAds() && UseCoreForStandardFormats && _bannerWantsVisible;

        IEnumerator CoDebouncedBannerRestoreOnAppResume()
        {
            if (bannerRestoreDebounceSec > 0f)
                yield return new WaitForSecondsRealtime(bannerRestoreDebounceSec);

            _bannerPauseRestoreCoroutine = null;

            if (!restoreBannerAfterFullscreenAds || !CanShowAds() || !UseCoreForStandardFormats)
                yield break;
            if (!_bannerWantsVisible || IsShowingAnyAd())
                yield break;

            ScheduleBannerRestoreAfterFullscreenAd("app_resume");
        }
        /// <summary>Warm-load interstitial through the global load pipeline (serialized with rewarded).</summary>
        public void RequestInterstitialLoadIfNeeded()
        {
            if (!UseCoreForStandardFormats || !CanShowAds())
                return;
            if (IsInterstitialAdLoaded())
                return;
            PreloadInterstitialAd();
        }

        /// <summary>Warm-load rewarded through the global load pipeline (serialized with interstitial).</summary>
        public void RequestRewardedLoadIfNeeded()
        {
            if (!UseCoreForStandardFormats || !CanShowAds())
                return;
            if (IsRewardedVideoLoaded())
                return;
            PreloadRewardedAd();
        }

        public bool IsInterstitialAdLoaded() =>
            UseCoreForStandardFormats && (_core.PrimaryProvider?.Interstitial.IsLoaded ?? false);

        public bool CanShowInterstitialAd() => IsInterstitialAdLoaded();

        public bool IsRewardedVideoLoaded() =>
            UseCoreForStandardFormats && (_core.PrimaryProvider?.Rewarded.IsLoaded ?? false);
        public bool CanShowRewardedVideo() => IsRewardedVideoLoaded();

        public bool IsBannerAdLoaded() =>
            UseCoreForStandardFormats && (_core.PrimaryProvider?.Banner?.IsLoaded ?? false);

        public bool CanShowBannerAd() => IsBannerAdLoaded();
        #endregion
        #region App Open
        public void ShowAppOpenAd() => _appOpen?.Show();
        public bool IsAppOpenAdLoaded() => _appOpen != null && _appOpen.IsLoaded();
        public void PreloadAppOpenAd() => _appOpen?.Preload();
        #endregion
    }
}
