using System;
using System.Collections;
using System.Collections.Generic;
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
        private bool _immediateFormatsPreloadedOnCoreReady;
        private bool _fullscreenFormatsPreloadedAfterRemoteConfig;
        private bool _pendingImmediatePreloadAfterCoreReady;
        private bool _pendingFullscreenPreloadAfterRemoteConfig;
        private bool _pendingRecoverFullscreenPreloadsAfterCoreReady;
        private bool _bannerStartupPreloadStarted;
        private bool _appOpenStartupPreloadStarted;
        private bool _legacyBridgeNotified;
        private bool _appOpenResumeInitStarted;
        private bool _isApplyingRemoteConfig;
        private TaskCompletionSource<bool> _initializeAsyncGate;
        const float CoreInitWaitTimeoutSec = 45f;
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
        /// <summary>Game routes show/load APIs through JisAds Core (even while Core is still initializing).</summary>
        public bool UsesCoreRouting => useCoreForStandardFormats;
        public bool UseCoreForStandardFormats => useCoreForStandardFormats && _core != null && _core.IsInitialized;
        public bool HasAppOpenSupport =>
            _core != null
            && _core.IsInitialized
            && _core.GetProviderForFormat(AdFormat.AppOpen)?.AppOpen is not NullAppOpenAd;
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
            WarmUpAdSdksEarly();
            AdEvents.OnProviderInitialized += HandleProviderInitialized;
        }

        void WarmUpAdSdksEarly()
        {
            if (settings == null)
                return;

            AdMobSdkEarlyInitBridge.TryWarmUpFromSettings(settings);
            MaxSdkEarlyInitBridge.TryWarmUpFromSettings(settings);
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
            AdEvents.OnProviderInitialized -= HandleProviderInitialized;
        }
        private void Start()
        {
            if (autoInitializeOnStart)
                _ = InitializeAsync();
        }
        public async Task InitializeAsync(bool fetchRemoteConfig = true)
        {
            if (_core != null && _core.IsInitialized)
            {
                TryCompleteStartupAfterCoreReady();
                return;
            }

            Task<bool> inFlight = null;
            var isLeader = false;
            lock (this)
            {
                if (_initializeAsyncGate == null)
                {
                    _initializeAsyncGate = new TaskCompletionSource<bool>();
                    isLeader = true;
                }
                else
                {
                    inFlight = _initializeAsyncGate.Task;
                }
            }

            if (!isLeader)
            {
                DebugAds.Log("[JisAds] InitializeAsync already in progress — joining existing startup.");
                await inFlight;
                await WaitForCoreReadyAsync();
                TryCompleteStartupAfterCoreReady();
                return;
            }

            try
            {
                await RunInitializeAsyncCore(fetchRemoteConfig);
                _initializeAsyncGate.TrySetResult(true);
            }
            catch (Exception ex)
            {
                _initializeAsyncGate.TrySetException(ex);
                lock (this)
                    _initializeAsyncGate = null;
                throw;
            }

            await WaitForCoreReadyAsync();
            LogInitializeAsyncComplete();
        }

        async Task RunInitializeAsyncCore(bool fetchRemoteConfig)
        {
            settings?.ApplyRuntimeDebugSettings();
            RefreshRemoveAdsFromPersistence();
            DebugAds.LogSdkInit("JisAds", "InitializeAsync", true, $"fetchRemoteConfig={fetchRemoteConfig}");
            WarmUpAdSdksEarly();
            await InitializeFirebaseAsync(fetchRemoteConfig);
            ApplyRemoteAdInventoryFromConfig();
            InitializeCoreFlow();
        }

        async Task WaitForCoreReadyAsync()
        {
            if (_core != null && _core.IsInitialized)
            {
                TryCompleteStartupAfterCoreReady();
                return;
            }

            var waited = 0f;
            while ((_core == null || !_core.IsInitialized) && waited < CoreInitWaitTimeoutSec)
            {
                waited += Time.unscaledDeltaTime;
                await Task.Yield();
            }

            TryCompleteStartupAfterCoreReady();
        }

        void LogInitializeAsyncComplete()
        {
            var coreReadyNow = _core != null && _core.IsInitialized;
            DebugAds.LogSdkInit(
                "JisAds",
                "InitializeAsync complete",
                coreReadyNow,
                coreReadyNow
                    ? null
                    : "Core AdManager not initialized within wait — startup preload will resume when Core becomes ready.");
        }

        /// <summary>
        /// Completes legacy bridge notification, app-open bootstrap, and standard-format preloads once
        /// <see cref="AdManager"/> is initialized. Safe to call from <see cref="InitializeAsync"/> and from
        /// the Core init success callback (handles slow AdMob/MAX init racing Remote Config).
        /// </summary>
        void TryCompleteStartupAfterCoreReady()
        {
            if (_core == null || !_core.IsInitialized)
                return;

            if (!_isReady)
            {
                _isReady = true;
                ApplyBannerRemoteConfig();
                DebugAds.LogSdkInit("JisAds", "Core AdManager ready", true);
            }

            if (!_legacyBridgeNotified)
            {
                _legacyBridgeNotified = true;
                AdsManager.Instance?.NotifyJisAdsCoreReady();
            }

            if (!_appOpenResumeInitStarted)
                EnsureAppOpenAndResumeBootstrapped();

            if (!ShouldPreloadAdsOnGameStart())
            {
                _pendingRecoverFullscreenPreloadsAfterCoreReady = false;
                _pendingImmediatePreloadAfterCoreReady = false;
                _pendingFullscreenPreloadAfterRemoteConfig = false;
                return;
            }

            if (_pendingRecoverFullscreenPreloadsAfterCoreReady)
            {
                TryPreloadImmediateFormatsOnCoreReady();
                RunDeferredFullscreenPreloadRecovery();
            }
            else
            {
                TryPreloadImmediateFormatsOnCoreReady();
                TryPreloadFullscreenFormatsAfterRemoteConfig();
            }

            TryFulfillQueuedBannerShow();
        }

        void EnsureAppOpenAndResumeBootstrapped()
        {
            if (_appOpenResumeInitStarted)
                return;

            _appOpenResumeInitStarted = true;
            resumeCoordinator?.Bind(this, _appOpen);

            if (ShouldPreloadAdsOnGameStart() && IsAppOpenEnabledInSetup())
                _appOpen?.BeginAfterCoreReady();

            if (IsRemoteConfigReady())
                resumeCoordinator?.ApplyRemoteConfig();
        }

        void TryPreloadImmediateFormatsOnCoreReady()
        {
            if (Application.isEditor)
                return;

            if (!ShouldPreloadAdsOnGameStart())
            {
                _immediateFormatsPreloadedOnCoreReady = true;
                _pendingImmediatePreloadAfterCoreReady = false;
                return;
            }

            if (_core == null || !_core.IsInitialized)
            {
                _pendingImmediatePreloadAfterCoreReady = true;
                DebugAds.Log("[JisAds] Deferring banner/app-open preload — Core AdManager not ready.");
                return;
            }

            if (_immediateFormatsPreloadedOnCoreReady)
                return;

            var bannerDone = !IsBannerEnabledInSetup() || _bannerStartupPreloadStarted;
            var appOpenDone = !IsAppOpenEnabledInSetup() || _appOpenStartupPreloadStarted;
            if (bannerDone && appOpenDone)
            {
                _immediateFormatsPreloadedOnCoreReady = true;
                return;
            }

            _pendingImmediatePreloadAfterCoreReady = false;
            _immediateFormatsPreloadedOnCoreReady = true;

            if (IsBannerEnabledInSetup() && !_bannerStartupPreloadStarted)
            {
                DebugAds.Log("[JisAds] Preloading banner — Core ready (no Remote Config wait).");
                _bannerStartupPreloadStarted = true;
                PreloadBannerAd(isStartup: true);
            }

            if (IsAppOpenEnabledInSetup() && _appOpen != null && !_appOpenStartupPreloadStarted)
            {
                DebugAds.Log("[JisAds] Preloading app open — Core ready (no Remote Config wait).");
                _appOpenStartupPreloadStarted = true;
                _appOpen.Preload();
            }
        }

        void TryPreloadFullscreenFormatsAfterRemoteConfig()
        {
            if (Application.isEditor)
                return;

            if (!ShouldPreloadAdsOnGameStart())
            {
                _fullscreenFormatsPreloadedAfterRemoteConfig = true;
                _pendingFullscreenPreloadAfterRemoteConfig = false;
                return;
            }

            if (_core == null)
            {
                _pendingFullscreenPreloadAfterRemoteConfig = true;
                DebugAds.Log("[JisAds] Deferring interstitial/rewarded preload — Core AdManager not ready.");
                return;
            }

            if (!_core.IsInitialized)
            {
                _pendingFullscreenPreloadAfterRemoteConfig = true;
                DebugAds.Log("[JisAds] Deferring interstitial/rewarded preload — Core AdManager not ready.");
                TryPreloadFullscreenForAllReadyProviders();
                return;
            }

            if (!IsRemoteConfigReady())
            {
                _pendingFullscreenPreloadAfterRemoteConfig = true;
                DebugAds.Log("[JisAds] Deferring interstitial/rewarded preload — Remote Config not ready.");
                return;
            }

            TryPreloadFullscreenForAllReadyProviders();

            if (_fullscreenFormatsPreloadedAfterRemoteConfig)
                return;

            _pendingFullscreenPreloadAfterRemoteConfig = false;
            _fullscreenFormatsPreloadedAfterRemoteConfig = true;

            if (IsInterstitialEnabledInSetup())
                StartCoroutine(CoDeferredInterstitialPreload());
        }

        static bool IsRemoteConfigReady() =>
            FirebaseManager.Instance != null && FirebaseManager.Instance.IsRemoteConfigReady;

        bool IsBannerEnabledInSetup() =>
            settings?.GetActiveProfile()?.sdkSetup?.IsActiveAdsType(AdsType.BANNER) == true;

        bool IsAppOpenEnabledInSetup() =>
            settings?.GetActiveProfile()?.sdkSetup?.IsActiveAdsType(AdsType.APP_OPEN) == true;

        bool IsInterstitialEnabledInSetup() =>
            settings?.GetActiveProfile()?.sdkSetup?.IsActiveAdsType(AdsType.INTERSTITIAL) == true;

        bool IsRewardedEnabledInSetup() =>
            settings?.GetActiveProfile()?.sdkSetup?.IsActiveAdsType(AdsType.REWARDED) == true;

        bool CanOperateBanner() =>
            useCoreForStandardFormats
            && _core != null
            && CanShowAds()
            && IsBannerProviderOperational();

        bool IsBannerProviderOperational()
        {
            if (_core == null)
                return false;

            var providerId = _core.GetProviderIdForFormat(AdFormat.Banner);
            return providerId != AdProviderId.None
                   && (_core.IsInitialized || _core.IsProviderInitialized(providerId));
        }

        bool CanOperateFullscreen() =>
            useCoreForStandardFormats
            && _core != null
            && CanShowAds()
            && (_core.IsInitialized || HasAnyOperationalFullscreenProvider());

        bool HasAnyOperationalFullscreenProvider()
        {
            if (_core == null)
                return false;

            foreach (var providerId in GetFullscreenPreloadProviderIds(AdFormat.Rewarded))
            {
                if (_core.IsProviderInitialized(providerId))
                    return true;
            }

            foreach (var providerId in GetFullscreenPreloadProviderIds(AdFormat.Interstitial))
            {
                if (_core.IsProviderInitialized(providerId))
                    return true;
            }

            return false;
        }

        void HandleProviderInitialized(string providerId)
        {
            if (!useCoreForStandardFormats || _core == null)
                return;

            var parsedId = ParseProviderId(providerId);
            if (parsedId == AdProviderId.None)
                return;

            OnProviderBecameReady(parsedId);
        }

        static AdProviderId ParseProviderId(string providerId) => providerId switch
        {
            "AdMob" => AdProviderId.AdMob,
            "Max" => AdProviderId.Max,
            _ => AdProviderId.None
        };

        void OnProviderBecameReady(AdProviderId providerId)
        {
            if (!ShouldPreloadAdsOnGameStart() || !CanShowAds())
                return;

            TryPreloadBannerForProvider(providerId);
            TryPreloadAppOpenForProvider(providerId);
            TryPreloadFullscreenForProvider(providerId);

            if (_bannerWantsVisible)
                TryFulfillQueuedBannerShow();
        }

        void TryPreloadBannerForProvider(AdProviderId providerId)
        {
            if (_bannerStartupPreloadStarted || !IsBannerEnabledInSetup())
                return;

            if (_core == null || _core.GetProviderIdForFormat(AdFormat.Banner) != providerId)
                return;

            if (!_core.IsProviderInitialized(providerId))
                return;

            _bannerStartupPreloadStarted = true;
            DebugAds.Log($"[JisAds] Preloading banner — {providerId} provider ready.");
            PreloadBannerAd(isStartup: true);
        }

        void TryPreloadAppOpenForProvider(AdProviderId providerId)
        {
            if (_appOpenStartupPreloadStarted || !IsAppOpenEnabledInSetup() || _appOpen == null)
                return;

            if (_core == null || _core.GetProviderIdForFormat(AdFormat.AppOpen) != providerId)
                return;

            if (!_core.IsProviderInitialized(providerId))
                return;

            _appOpenStartupPreloadStarted = true;
            DebugAds.Log($"[JisAds] Preloading app open — {providerId} provider ready.");
            _appOpen.Preload();
        }

        void TryPreloadFullscreenForProvider(AdProviderId providerId)
        {
            if (!IsRemoteConfigReady() || _core == null || !_core.IsProviderInitialized(providerId))
                return;

            if (IsRewardedEnabledInSetup() && ProviderHandlesFormat(providerId, AdFormat.Rewarded))
                TryPreloadRewardedForProvider(providerId);

            if (IsInterstitialEnabledInSetup() && ProviderHandlesFormat(providerId, AdFormat.Interstitial))
                TryPreloadInterstitialForProvider(providerId);
        }

        void TryPreloadFullscreenForAllReadyProviders()
        {
            if (!IsRemoteConfigReady() || _core == null)
                return;

            foreach (var providerId in GetFullscreenPreloadProviderIds(AdFormat.Rewarded))
                TryPreloadFullscreenForProvider(providerId);

            foreach (var providerId in GetFullscreenPreloadProviderIds(AdFormat.Interstitial))
                TryPreloadFullscreenForProvider(providerId);
        }

        bool ProviderHandlesFormat(AdProviderId providerId, AdFormat format)
        {
            foreach (var id in GetFullscreenPreloadProviderIds(format))
            {
                if (id == providerId)
                    return true;
            }

            return false;
        }

        void TryPreloadRewardedForProvider(AdProviderId providerId)
        {
            var provider = _core?.GetProvider(providerId);
            if (!CanOperateFullscreen() || provider?.Rewarded == null)
                return;

            if (provider.Rewarded.IsLoaded)
            {
                DebugAds.Log($"[JisAds][Rewarded][preload_skip] mediation={providerId} reason=already_loaded");
                return;
            }

            DebugAds.Log($"[JisAds][Rewarded][preload_start] mediation={providerId}");
            provider.Rewarded.Load(
                onLoaded: () =>
                {
                    OnPreloadSucceeded(StandardAdPreloadFormat.Rewarded);
                    DebugAds.Log($"[JisAds][Rewarded][preload_success] mediation={providerId}");
                },
                onFailed: err =>
                {
                    DebugAds.LogWarning($"[JisAds][Rewarded][preload_fail] mediation={providerId} error={err}");
                    HandlePreloadFailedIfNoFormatReady(StandardAdPreloadFormat.Rewarded);
                });
        }

        void TryPreloadInterstitialForProvider(AdProviderId providerId)
        {
            var provider = _core?.GetProvider(providerId);
            if (!CanOperateFullscreen() || provider?.Interstitial == null)
                return;

            if (provider.Interstitial.IsLoaded)
            {
                DebugAds.Log($"[JisAds][Interstitial][preload_skip] mediation={providerId} reason=already_loaded");
                return;
            }

            DebugAds.Log($"[JisAds][Interstitial][preload_start] mediation={providerId}");
            provider.Interstitial.Load(
                onLoaded: () =>
                {
                    OnPreloadSucceeded(StandardAdPreloadFormat.Interstitial);
                    DebugAds.Log($"[JisAds][Interstitial][preload_success] mediation={providerId}");
                },
                onFailed: err =>
                {
                    DebugAds.LogWarning($"[JisAds][Interstitial][preload_fail] mediation={providerId} error={err}");
                    HandlePreloadFailedIfNoFormatReady(StandardAdPreloadFormat.Interstitial);
                });
        }

        void TryFulfillQueuedBannerShow()
        {
            if (!_bannerWantsVisible || !CanShowAds() || !CanOperateBanner())
                return;

            DebugAds.Log("[JisAds] Fulfilling queued banner show after Core ready.");
            _core.ShowBanner(
                onShown: () => DebugAds.Log("[JisAds] Banner shown"),
                onFailed: err => Debug.LogWarning($"[JisAds] Banner show failed: {err}"));
            RestartBannerAutoRefresh();
        }

        void RunDeferredFullscreenPreloadRecovery()
        {
            _pendingRecoverFullscreenPreloadsAfterCoreReady = false;
            _pendingFullscreenPreloadAfterRemoteConfig = false;

            if (!CanOperateFullscreen() || !CanShowAds())
                return;

            _preloadFailCounts[PreloadFormatIndex(StandardAdPreloadFormat.Interstitial)] = 0;
            _preloadFailCounts[PreloadFormatIndex(StandardAdPreloadFormat.Rewarded)] = 0;

            if (!_fullscreenFormatsPreloadedAfterRemoteConfig)
            {
                TryPreloadFullscreenFormatsAfterRemoteConfig();
                return;
            }

            RequestInterstitialLoadIfNeeded();
            RequestRewardedLoadIfNeeded();
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

            if (!ShouldPreloadAdsOnGameStart() || !CanOperateFullscreen())
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
            if (!CanOperateBanner())
                return;

            var provider = _core.GetProvider(_core.GetProviderIdForFormat(AdFormat.Banner));
            if (provider?.Banner == null)
                return;

            var preserveVisible = _bannerWantsVisible;
            provider.Banner.Load(
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
            if (!CanOperateFullscreen() || _core == null)
                return;

            var providerIds = GetFullscreenPreloadProviderIds(AdFormat.Interstitial);
            LogFullscreenLoadPlan(AdFormat.Interstitial, providerIds);

            foreach (var providerId in providerIds)
            {
                if (!_core.IsProviderInitialized(providerId))
                {
                    DebugAds.Log($"[JisAds][Interstitial][preload_skip] mediation={providerId} reason=provider_not_initialized");
                    continue;
                }

                var provider = _core.GetProvider(providerId);
                if (provider?.Interstitial == null)
                {
                    DebugAds.LogWarning($"[JisAds][Interstitial][preload_skip] mediation={providerId} reason=provider_unavailable");
                    continue;
                }

                if (provider.Interstitial.IsLoaded)
                {
                    DebugAds.Log($"[JisAds][Interstitial][preload_skip] mediation={providerId} reason=already_loaded");
                    continue;
                }

                DebugAds.Log($"[JisAds][Interstitial][preload_start] mediation={providerId}");
                provider.Interstitial.Load(
                    onLoaded: () =>
                    {
                        OnPreloadSucceeded(StandardAdPreloadFormat.Interstitial);
                        DebugAds.Log($"[JisAds][Interstitial][preload_success] mediation={providerId}");
                    },
                    onFailed: err =>
                    {
                        DebugAds.LogWarning($"[JisAds][Interstitial][preload_fail] mediation={providerId} error={err}");
                        HandlePreloadFailedIfNoFormatReady(StandardAdPreloadFormat.Interstitial);
                    });
            }
        }

        void PreloadRewardedAd()
        {
            if (!CanOperateFullscreen() || _core == null)
                return;

            var providerIds = GetFullscreenPreloadProviderIds(AdFormat.Rewarded);
            LogFullscreenLoadPlan(AdFormat.Rewarded, providerIds);

            foreach (var providerId in providerIds)
            {
                if (!_core.IsProviderInitialized(providerId))
                {
                    DebugAds.Log($"[JisAds][Rewarded][preload_skip] mediation={providerId} reason=provider_not_initialized");
                    continue;
                }

                var provider = _core.GetProvider(providerId);
                if (provider?.Rewarded == null)
                {
                    DebugAds.LogWarning($"[JisAds][Rewarded][preload_skip] mediation={providerId} reason=provider_unavailable");
                    continue;
                }

                if (provider.Rewarded.IsLoaded)
                {
                    DebugAds.Log($"[JisAds][Rewarded][preload_skip] mediation={providerId} reason=already_loaded");
                    continue;
                }

                DebugAds.Log($"[JisAds][Rewarded][preload_start] mediation={providerId}");
                provider.Rewarded.Load(
                    onLoaded: () =>
                    {
                        OnPreloadSucceeded(StandardAdPreloadFormat.Rewarded);
                        DebugAds.Log($"[JisAds][Rewarded][preload_success] mediation={providerId}");
                    },
                    onFailed: err =>
                    {
                        DebugAds.LogWarning($"[JisAds][Rewarded][preload_fail] mediation={providerId} error={err}");
                        HandlePreloadFailedIfNoFormatReady(StandardAdPreloadFormat.Rewarded);
                    });
            }
        }

        void HandlePreloadFailedIfNoFormatReady(StandardAdPreloadFormat format)
        {
            if (format == StandardAdPreloadFormat.Interstitial && IsInterstitialAdLoaded())
            {
                OnPreloadSucceeded(format);
                return;
            }

            if (format == StandardAdPreloadFormat.Rewarded && IsRewardedVideoLoaded())
            {
                OnPreloadSucceeded(format);
                return;
            }

            HandlePreloadFailed(format);
        }

        /*
         * Kept in this region because preload retries call the method by format.
         */
        void PreloadSingleProviderInterstitial(AdProviderId providerId)
        {
            var provider = _core?.GetProvider(providerId);
            if (!CanOperateFullscreen() || provider?.Interstitial == null || _core == null || !_core.IsProviderInitialized(providerId))
                return;

            if (provider.Interstitial.IsLoaded)
            {
                DebugAds.Log($"[JisAds][Interstitial][preload_skip] mediation={providerId} reason=already_loaded");
                return;
            }

            DebugAds.Log($"[JisAds][Interstitial][preload_start] mediation={providerId}");
            provider.Interstitial.Load(
                onLoaded: () =>
                {
                    OnPreloadSucceeded(StandardAdPreloadFormat.Interstitial);
                    DebugAds.Log($"[JisAds][Interstitial][preload_success] mediation={providerId}");
                },
                onFailed: err =>
                {
                    DebugAds.LogWarning($"[JisAds][Interstitial][preload_fail] mediation={providerId} error={err}");
                    HandlePreloadFailed(StandardAdPreloadFormat.Interstitial);
                });
        }

        void PreloadSingleProviderRewarded(AdProviderId providerId)
        {
            var provider = _core?.GetProvider(providerId);
            if (!CanOperateFullscreen() || provider?.Rewarded == null || _core == null || !_core.IsProviderInitialized(providerId))
                return;

            if (provider.Rewarded.IsLoaded)
            {
                DebugAds.Log($"[JisAds][Rewarded][preload_skip] mediation={providerId} reason=already_loaded");
                return;
            }

            DebugAds.Log($"[JisAds][Rewarded][preload_start] mediation={providerId}");
            provider.Rewarded.Load(
                onLoaded: () =>
                {
                    OnPreloadSucceeded(StandardAdPreloadFormat.Rewarded);
                    DebugAds.Log($"[JisAds][Rewarded][preload_success] mediation={providerId}");
                },
                onFailed: err =>
                {
                    DebugAds.LogWarning($"[JisAds][Rewarded][preload_fail] mediation={providerId} error={err}");
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
            var provider = _core?.GetProviderForFormat(AdFormat.Banner);
            if (!UseCoreForStandardFormats || provider?.Banner == null)
                return;

            DebugAds.Log("[JisAds] Banner auto-refresh reload");
            provider.Banner.Load(
                onLoaded: () =>
                {
                    if (_bannerWantsVisible)
                        _core.ShowBanner(
                            onShown: () => DebugAds.Log("[JisAds] Banner auto-refresh shown"),
                            onFailed: err => DebugAds.LogWarning($"[JisAds] Banner auto-refresh show failed: {err}"));
                },
                onFailed: err => DebugAds.LogWarning($"[JisAds] Banner auto-refresh load failed: {err}"));
        }
        public void RefreshAppOpenAndResumeRemoteConfig()
        {
            _appOpen?.ApplyRemoteConfig();
            resumeCoordinator?.ApplyRemoteConfig();
            ApplyBannerRemoteConfig();
            RecoverFullscreenPreloadsAfterRemoteConfigRefresh();
        }

        /// <summary>
        /// Call after Firebase Remote Config is ready (fetch success or defaults-only).
        /// Applies inventory, legacy managers, app-open/resume policies, and arms fullscreen preloads.
        /// </summary>
        public void OnRemoteConfigFetched()
        {
            if (_isApplyingRemoteConfig)
                return;

            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
            {
                DebugAds.LogWarning("[JisAds] OnRemoteConfigFetched skipped — Remote Config not ready.");
                return;
            }

            _isApplyingRemoteConfig = true;
            try
            {
                AdsManager.Instance?.RefreshRemoteConfigDrivenSettings(notifyLegacySubscribers: false);
                RefreshAppOpenAndResumeRemoteConfig();
                DebugAds.Log("[JisAds] Remote Config applied to ads — preloads armed.");
            }
            finally
            {
                _isApplyingRemoteConfig = false;
            }
        }

        /// <summary>
        /// Background-friendly RC pipeline: fetch (or defaults) then apply to ads and preload.
        /// Does not block game loading when started fire-and-forget from the boot scene.
        /// </summary>
        /// <returns><c>true</c> when server fetch + activate succeeded.</returns>
        public async Task<bool> FetchAndApplyRemoteConfigAsync()
        {
            if (FirebaseManager.Instance == null)
            {
                var go = new GameObject("JisSDKAds_FirebaseManager");
                go.AddComponent<FirebaseManager>();
            }

            var fetchSucceeded = await FirebaseManager.Instance.FetchRemoteConfigAsync();
            OnRemoteConfigFetched();
            return fetchSucceeded;
        }

        /// <summary>
        /// When Remote Config refreshes (e.g. sequential-tier ad unit IDs arrive late), re-apply the
        /// resolved IDs and re-arm interstitial/rewarded preloads that may have permanently given up
        /// earlier (the tiered preload retry stops after a few "no ad unit configured" failures).
        /// </summary>
        void RecoverFullscreenPreloadsAfterRemoteConfigRefresh()
        {
            // Always apply inventory when RC arrives — even if Core is still initializing.
            ApplyRemoteAdInventoryFromConfig();

            if (!useCoreForStandardFormats || !CanShowAds())
                return;

            if (_core == null || !_core.IsInitialized)
            {
                _pendingRecoverFullscreenPreloadsAfterCoreReady = true;
                _pendingFullscreenPreloadAfterRemoteConfig = false;
                DebugAds.Log("[JisAds] Deferring fullscreen preload recovery — Core AdManager not ready.");
                TryPreloadFullscreenForAllReadyProviders();
                return;
            }

            if (!_immediateFormatsPreloadedOnCoreReady)
                TryPreloadImmediateFormatsOnCoreReady();

            RunDeferredFullscreenPreloadRecovery();
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
            OnRemoteConfigFetched();
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

            AdMediationModeRemoteConfigResolver.ApplyMediationModesFromRemoteConfig(settings);
            AdInventoryRemoteConfigResolver.ApplyInventoryModesFromRemoteConfig(profile.sdkSetup, profile, settings);
            SequentialTierRemoteConfigResolver.ApplyResolvedIdsToAllMediationSetups(profile);
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
            var primaryProviderId = ToProviderId(profile.mediation);
            var usesFullscreenMultipleMediation = settings != null && settings.UsesAnyFullscreenMultipleMediation();
            _core.ConfigureSingleMediation(primaryProviderId, !usesFullscreenMultipleMediation);

            var fallbackProviderId = usesFullscreenMultipleMediation ? GetOppositeProvider(primaryProviderId) : AdProviderId.None;
            if (fallbackProviderId != AdProviderId.None)
                _core.SetProviderPriority(primaryProviderId, fallbackProviderId);

            var providerMediations = CollectCoreProviderMediations(profile);
            var registeredAny = false;
            foreach (var mediation in providerMediations)
            {
                var providerConfig = ProviderConfigFactory.CreateFromSdkSetup(profile, mediation);
                if (providerConfig == null)
                {
                    Debug.LogWarning($"[JisAds] No Core provider for {mediation}.");
                    continue;
                }

                var provider = providerConfig.CreateProvider();
                provider = DecorateSequentialAdsIfEnabled(provider, profile, mediation);
                _core.RegisterProvider(providerConfig.ProviderId, provider);
                registeredAny = true;
            }

            if (!registeredAny)
            {
                useCoreForStandardFormats = false;
                return;
            }

            ConfigureCoreFormatRoutes(profile);
            _core.Initialize(
                onSuccess: () =>
                {
                    DebugAds.LogSdkInit("JisAds", "Core AdManager", true);
                    TryCompleteStartupAfterCoreReady();
                },
                onFailure: err =>
                {
                    DebugAds.LogSdkInit("JisAds", "Core AdManager", false, err);
                    useCoreForStandardFormats = false;
                    _pendingRecoverFullscreenPreloadsAfterCoreReady = false;
                    _pendingImmediatePreloadAfterCoreReady = false;
                    _pendingFullscreenPreloadAfterRemoteConfig = false;
                });
        }

        HashSet<AdsMediationType> CollectCoreProviderMediations(PlatformAdsProfile profile)
        {
            var providers = new HashSet<AdsMediationType>();
            if (profile == null)
                return providers;

            AddProviderMediation(providers, profile.mediation);
            var setup = profile.sdkSetup;
            if (setup != null)
            {
                AddProviderMediation(providers, setup.GetAdsMediationType(AdsType.BANNER));
                AddProviderMediation(providers, setup.GetAdsMediationType(AdsType.INTERSTITIAL));
                AddProviderMediation(providers, setup.GetAdsMediationType(AdsType.REWARDED));
                AddProviderMediation(providers, setup.GetAdsMediationType(AdsType.APP_OPEN));
            }

            foreach (var mediation in settings.GetFullscreenAutoShowPriority(AdsType.INTERSTITIAL))
                AddProviderMediation(providers, mediation);

            foreach (var mediation in settings.GetFullscreenAutoShowPriority(AdsType.REWARDED))
                AddProviderMediation(providers, mediation);

            return providers;
        }

        static void AddProviderMediation(HashSet<AdsMediationType> providers, AdsMediationType mediation)
        {
            if (mediation == AdsMediationType.MAX || mediation == AdsMediationType.ADMOB)
                providers.Add(mediation);
        }

        void ConfigureCoreFormatRoutes(PlatformAdsProfile profile)
        {
            var setup = profile?.sdkSetup;
            if (setup == null || _core == null)
                return;

            _core.SetFormatProvider(AdFormat.Banner, ToProviderId(setup.GetAdsMediationType(AdsType.BANNER)));
            _core.SetFormatProvider(AdFormat.Interstitial, ToProviderId(setup.GetAdsMediationType(AdsType.INTERSTITIAL)));
            _core.SetFormatProvider(AdFormat.Rewarded, ToProviderId(setup.GetAdsMediationType(AdsType.REWARDED)));
            _core.SetFormatProvider(AdFormat.AppOpen, ToProviderId(setup.GetAdsMediationType(AdsType.APP_OPEN)));
        }

        static AdProviderId ToProviderId(AdsMediationType mediation) => mediation switch
        {
            AdsMediationType.MAX => AdProviderId.Max,
            AdsMediationType.ADMOB => AdProviderId.AdMob,
            _ => AdProviderId.None
        };

        static AdsMediationType ToMediation(AdProviderId provider) => provider switch
        {
            AdProviderId.Max => AdsMediationType.MAX,
            AdProviderId.AdMob => AdsMediationType.ADMOB,
            _ => AdsMediationType.NONE
        };

        static AdProviderId GetOppositeProvider(AdProviderId provider) => provider switch
        {
            AdProviderId.Max => AdProviderId.AdMob,
            AdProviderId.AdMob => AdProviderId.Max,
            _ => AdProviderId.None
        };

        IAdService DecorateSequentialAdsIfEnabled(IAdService provider, PlatformAdsProfile profile, AdsMediationType mediation)
        {
            switch (mediation)
            {
                case AdsMediationType.ADMOB:
                    return DecorateAdMobSequentialAdsIfEnabled(provider, profile);
                case AdsMediationType.MAX:
                    return DecorateMaxSequentialAdsIfEnabled(provider, profile);
                default:
                    return provider;
            }
        }

        IAdService DecorateAdMobSequentialAdsIfEnabled(IAdService provider, PlatformAdsProfile profile)
        {
#if UNITY_AD_ADMOB
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

        IAdService DecorateMaxSequentialAdsIfEnabled(IAdService provider, PlatformAdsProfile profile)
        {
#if UNITY_AD_MAX
            var max = profile.sdkSetup?.maxAdsSetup;
            var interstitialConfig = max?.InterstitialTierConfig;
            var rewardedConfig = max?.RewardedTierConfig;

            var decorated = MaxSequentialTierReflection.TryDecorate(
                provider,
                this,
                interstitialConfig,
                rewardedConfig);

            if (!ReferenceEquals(decorated, provider))
            {
                if (interstitialConfig != null && interstitialConfig.enableSequentialLadder)
                    DebugAds.Log("[JisAds] MAX interstitial uses SequentialTier ladder via Core.");
                if (rewardedConfig != null && rewardedConfig.enableSequentialLadder)
                    DebugAds.Log("[JisAds] MAX rewarded uses SequentialTier ladder via Core.");
            }

            return decorated;
#else
            return provider;
#endif
        }

        List<AdProviderId> GetFullscreenShowProviderIds(AdFormat format)
        {
            var list = new List<AdProviderId>(2);
            if (settings != null)
            {
                foreach (var mediation in settings.GetFullscreenAutoShowPriority(ToAdsType(format)))
                    AddProviderIfValid(list, ToProviderId(mediation));
            }

            AddProviderIfValid(list, _core != null ? _core.GetProviderIdForFormat(format) : AdProviderId.None);
            return list;
        }

        /// <summary>
        /// Preload order for multi-mediation: warm fallback mediation first so a show-ready ad exists
        /// while the primary provider (often AdMob tiered ladder) is still loading.
        /// </summary>
        List<AdProviderId> GetFullscreenPreloadProviderIds(AdFormat format)
        {
            var providerIds = GetFullscreenShowProviderIds(format);
            if (providerIds.Count <= 1 || settings == null || !settings.IsMultipleMediationEnabled(ToAdsType(format)))
                return providerIds;

            var primary = _core != null ? _core.GetProviderIdForFormat(format) : AdProviderId.None;
            if (primary == AdProviderId.None)
                return providerIds;

            var fallbackFirst = new List<AdProviderId>(providerIds.Count);
            foreach (var providerId in providerIds)
            {
                if (providerId != primary)
                    fallbackFirst.Add(providerId);
            }

            if (fallbackFirst.Count == 0)
                return providerIds;

            fallbackFirst.Add(primary);
            return fallbackFirst;
        }

        void LogFullscreenLoadPlan(AdFormat format, List<AdProviderId> providerIds)
        {
            if (providerIds == null || providerIds.Count == 0)
            {
                DebugAds.LogWarning($"[JisAds][{format}][preload_plan] no mediation provider configured");
                return;
            }

            DebugAds.Log($"[JisAds][{format}][preload_plan] mediations={string.Join(">", providerIds)}");
        }

        List<AdProviderId> BuildFullscreenShowProviderOrder(AdsMediationType requestedMediation, AdFormat format)
        {
            var order = new List<AdProviderId>(2);
            AddProviderIfValid(order, ToProviderId(requestedMediation));

            var isMultipleForFormat = settings != null && settings.IsMultipleMediationEnabled(ToAdsType(format));
            if (settings != null && isMultipleForFormat)
            {
                foreach (var mediation in settings.GetFullscreenAutoShowPriority(ToAdsType(format)))
                    AddProviderIfValid(order, ToProviderId(mediation));
            }
            else if (requestedMediation == AdsMediationType.NONE)
            {
                AddProviderIfValid(order, _core != null ? _core.GetProviderIdForFormat(format) : AdProviderId.None);
            }

            if (isMultipleForFormat && requestedMediation == AdsMediationType.ADMOB)
                AddProviderIfValid(order, AdProviderId.Max);
            else if (isMultipleForFormat && requestedMediation == AdsMediationType.MAX)
                AddProviderIfValid(order, AdProviderId.AdMob);

            return order;
        }

        static AdsType ToAdsType(AdFormat format) => format switch
        {
            AdFormat.Interstitial => AdsType.INTERSTITIAL,
            AdFormat.Rewarded => AdsType.REWARDED,
            AdFormat.Banner => AdsType.BANNER,
            AdFormat.AppOpen => AdsType.APP_OPEN,
            _ => AdsType.BANNER
        };

        static void AddProviderIfValid(List<AdProviderId> order, AdProviderId provider)
        {
            if (provider == AdProviderId.None || order.Contains(provider))
                return;
            order.Add(provider);
        }

        AdProviderId SelectProviderForShow(List<AdProviderId> order, AdFormat format)
        {
            if (_core == null || order == null)
                return AdProviderId.None;

            foreach (var provider in order)
            {
                if (!_core.HasProvider(provider) || !_core.IsProviderInitialized(provider))
                    continue;
                if (IsProviderLoaded(provider, format))
                    return provider;
            }

            foreach (var provider in order)
            {
                if (_core.HasProvider(provider) && _core.IsProviderInitialized(provider))
                    return provider;
            }

            return AdProviderId.None;
        }

        AdProviderId SelectFallbackProvider(List<AdProviderId> order, AdProviderId primary)
        {
            if (_core == null || order == null)
                return AdProviderId.None;

            foreach (var provider in order)
            {
                if (provider != primary && _core.HasProvider(provider) && _core.IsProviderInitialized(provider))
                    return provider;
            }

            return AdProviderId.None;
        }

        bool IsProviderLoaded(AdProviderId provider, AdFormat format)
        {
            if (_core == null)
                return false;

            return format switch
            {
                AdFormat.Interstitial => _core.IsInterstitialLoaded(provider),
                AdFormat.Rewarded => _core.IsRewardedLoaded(provider),
                _ => false
            };
        }

        bool IsInterstitialAdLoaded(List<AdProviderId> order)
        {
            if (!CanOperateFullscreen() || order == null)
                return false;

            foreach (var provider in order)
            {
                if (!_core.IsProviderInitialized(provider))
                    continue;

                if (_core.IsInterstitialLoaded(provider))
                    return true;
            }

            return false;
        }

        bool IsRewardedVideoLoaded(List<AdProviderId> order)
        {
            if (!CanOperateFullscreen() || order == null)
                return false;

            foreach (var provider in order)
            {
                if (!_core.IsProviderInitialized(provider))
                    continue;

                if (_core.IsRewardedLoaded(provider))
                    return true;
            }

            return false;
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
            _immediateFormatsPreloadedOnCoreReady = true;
            _fullscreenFormatsPreloadedAfterRemoteConfig = true;
            _bannerStartupPreloadStarted = true;
            _appOpenStartupPreloadStarted = true;
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
            ShowInterstitial("", AdsMediationType.NONE, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);

        public void ShowInterstitial(
            AdsMediationType mediation,
            UnityAction closedCallback = null,
            UnityAction showSuccessCallback = null,
            UnityAction showFailCallback = null,
            bool isTracking = true,
            bool isSkipCapping = false) =>
            ShowInterstitial("", mediation, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);

        public void ShowInterstitialAuto(
            UnityAction closedCallback = null,
            UnityAction showSuccessCallback = null,
            UnityAction showFailCallback = null,
            bool isTracking = true,
            bool isSkipCapping = false) =>
            ShowInterstitial("", AdsMediationType.NONE, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);

        public void ShowInterstitial(
            string interstitialPlacement,
            UnityAction closedCallback = null,
            UnityAction showSuccessCallback = null,
            UnityAction showFailCallback = null,
            bool isTracking = true,
            bool isSkipCapping = false) =>
            ShowInterstitial(interstitialPlacement, AdsMediationType.NONE, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);

        public void ShowInterstitial(
            string interstitialPlacement,
            AdsMediationType mediation,
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
            var providerOrder = BuildFullscreenShowProviderOrder(mediation, AdFormat.Interstitial);
            _pendingInterstitialHadLoadedAdAtShowRequest = IsInterstitialAdLoaded(providerOrder);
            _pendingInterstitialIsTracking = isTracking;
            _pendingInterstitialPlacement = interstitialPlacement;
            _pendingInterstitialClosedCallback = closedCallback;
            _pendingInterstitialShowSuccessCallback = showSuccessCallback;
            _pendingInterstitialShowFailCallback = showFailCallback;

            TrackPendingInterstitialClick();

            if (useCoreForStandardFormats)
            {
                if (!CanOperateFullscreen())
                {
                    DebugAds.LogWarning("[JisAds] Interstitial show skipped — Core not ready. Warm-loading.");
                    RequestInterstitialLoadIfNeeded();
                    TrackPendingInterstitialShowFailure(isTracking);
                    ConsumePendingInterstitialCallbacksOnFail();
                    return;
                }

                if (!_pendingInterstitialHadLoadedAdAtShowRequest)
                {
                    DebugAds.LogWarning("[JisAds] Interstitial show skipped: no mediation has a loaded ad. Warm-loading for next request.");
                    RequestInterstitialLoadIfNeeded();
                    TrackPendingInterstitialShowFailure(isTracking);
                    ConsumePendingInterstitialCallbacksOnFail();
                    return;
                }

                SetAdsShowingState(true);
                HideBannerForFullscreenAd("interstitial");
                var provider = SelectProviderForShow(providerOrder, AdFormat.Interstitial);
                var fallback = SelectFallbackProvider(providerOrder, provider);
                if (provider == AdProviderId.None)
                {
                    Debug.LogWarning("[JisAds] Core interstitial failed: no mediation provider registered.");
                    TrackPendingInterstitialShowFailure(isTracking);
                    ConsumePendingInterstitialCallbacksOnFail();
                    return;
                }

                _core.ShowInterstitial(
                    provider,
                    fallback,
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
            AdEvents.OnInterstitialLoaded += OnCoreInterstitialLoaded;
            AdEvents.OnRewardedLoaded += OnCoreRewardedLoaded;
        }

        void UnbindCoreCappingEvents()
        {
            AdEvents.OnInterstitialShown -= OnCoreInterstitialShown;
            AdEvents.OnRewardEarned -= OnCoreRewardEarned;
            AdEvents.OnInterstitialLoaded -= OnCoreInterstitialLoaded;
            AdEvents.OnRewardedLoaded -= OnCoreRewardedLoaded;
        }

        void OnCoreInterstitialLoaded(AdFormat format, string providerId)
        {
            DebugAds.Log($"[JisAds][{format}][load_success] mediation={providerId}");
        }

        void OnCoreRewardedLoaded(AdFormat format, string providerId)
        {
            DebugAds.Log($"[JisAds][{format}][load_success] mediation={providerId}");
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
            UnityAction failedCallback = null) =>
            ShowRewardVideo(rewardedPlacement, AdsMediationType.NONE, successCallback, closedCallback, failedCallback);

        public void ShowRewardVideoAuto(
            string rewardedPlacement,
            UnityAction successCallback,
            UnityAction<bool> closedCallback = null,
            UnityAction failedCallback = null) =>
            ShowRewardVideo(rewardedPlacement, AdsMediationType.NONE, successCallback, closedCallback, failedCallback);

        public void ShowRewardVideo(
            string rewardedPlacement,
            AdsMediationType mediation,
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
            var providerOrder = BuildFullscreenShowProviderOrder(mediation, AdFormat.Rewarded);
            _pendingRewardedHadLoadedAdAtShowRequest = IsRewardedVideoLoaded(providerOrder);
            _pendingRewardedPlacement = rewardedPlacement;
            _pendingRewardedRewardCallback = successCallback;
            _pendingRewardedClosedCallback = closedCallback;
            _pendingRewardedFailCallback = failedCallback;
            SetAdsShowingState(true);

            TrackRewardedClick();

            if (useCoreForStandardFormats)
            {
                if (!CanOperateFullscreen())
                {
                    DebugAds.LogWarning("[JisAds] Rewarded show skipped — Core not ready. Warm-loading.");
                    RequestRewardedLoadIfNeeded();
                    TrackPendingRewardedShowFailure();
                    ConsumePendingRewardedCallbacksOnFail();
                    return;
                }

                if (!_pendingRewardedHadLoadedAdAtShowRequest)
                {
                    AdLoadCoordinator.Instance.PrepareUrgentRewarded();
                    DebugAds.LogWarning("[JisAds] Rewarded show skipped: no mediation has a loaded ad. Warm-loading for next request.");
                    RequestRewardedLoadIfNeeded();
                    TrackPendingRewardedShowFailure();
                    ConsumePendingRewardedCallbacksOnFail();
                    return;
                }

                HideBannerForFullscreenAd("rewarded");
                var provider = SelectProviderForShow(providerOrder, AdFormat.Rewarded);
                var fallback = SelectFallbackProvider(providerOrder, provider);
                if (provider == AdProviderId.None)
                {
                    Debug.LogWarning("[JisAds] Core rewarded failed: no mediation provider registered.");
                    TrackPendingRewardedShowFailure();
                    ConsumePendingRewardedCallbacksOnFail();
                    return;
                }

                _core.ShowRewarded(
                    provider,
                    fallback,
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

            if (!useCoreForStandardFormats)
            {
                Debug.LogWarning("[JisAds] Banner unavailable — Core AdManager not enabled.");
                return;
            }

            _bannerWantsVisible = true;

            if (!CanOperateBanner())
            {
                DebugAds.Log("[JisAds] Banner show queued — Core AdManager not ready yet.");
                return;
            }

            _core.ShowBanner(
                onShown: () => DebugAds.Log("[JisAds] Banner shown"),
                onFailed: err => Debug.LogWarning($"[JisAds] Banner show failed: {err}"));
            RestartBannerAutoRefresh();
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
                var bannerProvider = _core.GetProviderForFormat(AdFormat.Banner);
                if (bannerProvider?.Banner == null)
                {
                    _bannerRestoreCoroutine = null;
                    yield break;
                }

                bannerProvider.Banner.Load(
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
            if (!CanOperateFullscreen())
                return;
            if (IsInterstitialAdLoaded())
                return;
            PreloadInterstitialAd();
        }

        /// <summary>Warm-load rewarded through the global load pipeline (serialized with interstitial).</summary>
        public void RequestRewardedLoadIfNeeded()
        {
            if (!CanOperateFullscreen())
                return;
            if (IsRewardedVideoLoaded())
                return;
            PreloadRewardedAd();
        }

        public bool IsInterstitialAdLoaded() =>
            IsInterstitialAdLoaded(BuildFullscreenShowProviderOrder(AdsMediationType.NONE, AdFormat.Interstitial));

        public bool CanShowInterstitialAd() => IsInterstitialAdLoaded();

        public bool IsRewardedVideoLoaded() =>
            IsRewardedVideoLoaded(BuildFullscreenShowProviderOrder(AdsMediationType.NONE, AdFormat.Rewarded));
        public bool CanShowRewardedVideo() => IsRewardedVideoLoaded();

        public bool IsBannerAdLoaded()
        {
            if (!CanOperateBanner())
                return false;

            var providerId = _core.GetProviderIdForFormat(AdFormat.Banner);
            if (!_core.IsProviderInitialized(providerId))
                return false;

            return _core.GetProvider(providerId)?.Banner?.IsLoaded ?? false;
        }

        public bool CanShowBannerAd() => IsBannerAdLoaded();
        #endregion
        #region App Open
        public void ShowAppOpenAd() => _appOpen?.Show();
        public bool IsAppOpenAdLoaded() => _appOpen != null && _appOpen.IsLoaded();
        public void PreloadAppOpenAd() => _appOpen?.Preload();
        #endregion
    }
}
