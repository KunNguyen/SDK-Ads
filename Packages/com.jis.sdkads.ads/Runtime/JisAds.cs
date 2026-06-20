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
        [Header("Network recovery")]
        [SerializeField] private bool enableNetworkReachabilityPolling = true;
        [SerializeField] private float networkReachabilityPollIntervalSec = 5f;
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
        private bool _bannerWantsVisible;
        private bool _bannerPreloadInFlight;
        private bool _bannerShowInFlight;
        private bool _bannerAutoRefreshEnabled;
        private float _bannerAutoRefreshIntervalSec = BannerRefreshSettings.DefaultIntervalSeconds;
        private Coroutine _bannerAutoRefreshCoroutine;
        private Coroutine _bannerRestoreCoroutine;
        private Coroutine _bannerPauseRestoreCoroutine;
        private int _bannerRestoreGeneration;
        const string BannerRestoreLogPrefix = "[JisAds][BannerRestore]";
        const float FullscreenPreloadRemoteConfigWaitTimeoutSec = 10f;
        const float LoadedAdMaxAgeSec = 50f * 60f;
        const int MaxProviderInitRetries = 5;
        const int MaxCoreInitRetries = 3;
        private bool _interstitialLoadInFlight;
        private int _interstitialLoadsPending;
        private bool _rewardedLoadInFlight;
        private int _rewardedLoadsPending;
        private bool _forceFullscreenPreloadWithoutRemoteConfig;
        private bool _fullscreenPreloadRemoteConfigWaitStarted;
        private Coroutine _fullscreenPreloadRemoteConfigWaitCoroutine;
        private NetworkReachability _lastNetworkReachability;
        private readonly Dictionary<AdProviderId, float> _interstitialLoadedAtUnscaled = new Dictionary<AdProviderId, float>();
        private readonly Dictionary<AdProviderId, float> _rewardedLoadedAtUnscaled = new Dictionary<AdProviderId, float>();
        private List<AdProviderId> _cachedInterstitialProviderOrder;
        private List<AdProviderId> _cachedRewardedProviderOrder;
        private bool _providerOrderCacheDirty = true;
        private readonly Dictionary<AdProviderId, int> _providerInitFailCounts = new Dictionary<AdProviderId, int>();
        private readonly Dictionary<AdProviderId, Coroutine> _providerInitRetryCoroutines = new Dictionary<AdProviderId, Coroutine>();
        private int _coreInitFailCount;
        private Coroutine _coreInitRetryCoroutine;
        private Coroutine _networkReachabilityPollCoroutine;
        private bool _networkRecoveryRemoteConfigFetchInFlight;
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
            _lastNetworkReachability = Application.internetReachability;
            AdLoadCoordinator.Instance.Configure(this);
            BindCoreCappingEvents();
            BindProviderEvents();
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
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                HandleNetworkReachabilityChanged();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            resumeCoordinator?.OnApplicationPause(pauseStatus);

            if (!pauseStatus)
            {
                HandleNetworkReachabilityChanged();
                if (_bannerWantsVisible && !IsShowingAnyAd())
                    ScheduleBannerRestoreOnAppResume();
            }
        }
        private void OnDestroy()
        {
            StopAllPreloadRetries();
            StopAllProviderInitRetries();
            StopCoreInitRetry();
            StopFullscreenPreloadRemoteConfigWait();
            StopNetworkReachabilityPolling();
            StopBannerAutoRefresh();
            CancelPendingBannerRestore();
            CancelPendingBannerPauseRestore();
            UnbindCoreCappingEvents();
            UnbindProviderEvents();
            AdEvents.OnProviderInitialized -= HandleProviderInitialized;
        }
        private void Start()
        {
            StartNetworkReachabilityPolling();
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

            if (!IsRemoteConfigReady() && !_forceFullscreenPreloadWithoutRemoteConfig)
            {
                _pendingFullscreenPreloadAfterRemoteConfig = true;
                DebugAds.Log("[JisAds] Deferring interstitial/rewarded preload — Remote Config not ready.");
                EnsureFullscreenPreloadRemoteConfigWaitStarted();
                return;
            }

            ProceedWithFullscreenPreloadAfterRemoteConfig();
        }

        void EnsureFullscreenPreloadRemoteConfigWaitStarted()
        {
            if (_fullscreenPreloadRemoteConfigWaitStarted)
                return;

            _fullscreenPreloadRemoteConfigWaitStarted = true;
            _fullscreenPreloadRemoteConfigWaitCoroutine = StartCoroutine(CoWaitRemoteConfigOrTimeoutPreloadFullscreen());
        }

        void StopFullscreenPreloadRemoteConfigWait()
        {
            if (_fullscreenPreloadRemoteConfigWaitCoroutine == null)
                return;

            StopCoroutine(_fullscreenPreloadRemoteConfigWaitCoroutine);
            _fullscreenPreloadRemoteConfigWaitCoroutine = null;
            _fullscreenPreloadRemoteConfigWaitStarted = false;
        }

        IEnumerator CoWaitRemoteConfigOrTimeoutPreloadFullscreen()
        {
            var startedAt = Time.unscaledTime;
            while (!IsRemoteConfigReady() && Time.unscaledTime - startedAt < FullscreenPreloadRemoteConfigWaitTimeoutSec)
                yield return null;

            _fullscreenPreloadRemoteConfigWaitCoroutine = null;
            _fullscreenPreloadRemoteConfigWaitStarted = false;

            if (IsRemoteConfigReady())
            {
                DebugAds.Log("[JisAds] Remote Config ready — proceeding with fullscreen preload.");
            }
            else
            {
                _forceFullscreenPreloadWithoutRemoteConfig = true;
                DebugAds.LogWarning(
                    $"[JisAds] Remote Config wait timeout ({FullscreenPreloadRemoteConfigWaitTimeoutSec:0.#}s) — fullscreen preload with default inventory.");
            }

            _pendingFullscreenPreloadAfterRemoteConfig = false;
            ProceedWithFullscreenPreloadAfterRemoteConfig();
        }

        void ProceedWithFullscreenPreloadAfterRemoteConfig()
        {
            if (!ShouldPreloadAdsOnGameStart())
                return;

            if (_core == null || !_core.IsInitialized)
                return;

            TryPreloadFullscreenForAllReadyProviders();

            if (_fullscreenFormatsPreloadedAfterRemoteConfig)
                return;

            _pendingFullscreenPreloadAfterRemoteConfig = false;
            _fullscreenFormatsPreloadedAfterRemoteConfig = true;

            if (IsInterstitialEnabledInSetup())
                StartCoroutine(CoDeferredInterstitialPreload());
        }

        bool CanPreloadFullscreenForProvider() =>
            IsRemoteConfigReady() || _forceFullscreenPreloadWithoutRemoteConfig;

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
            _providerInitFailCounts[providerId] = 0;
            if (_providerInitRetryCoroutines.TryGetValue(providerId, out var retry) && retry != null)
            {
                StopCoroutine(retry);
                _providerInitRetryCoroutines[providerId] = null;
            }

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
            if (!CanPreloadFullscreenForProvider() || _core == null || !_core.IsProviderInitialized(providerId))
                return;

            if (IsRewardedEnabledInSetup() && ProviderHandlesFormat(providerId, AdFormat.Rewarded))
                TryPreloadRewardedForProvider(providerId);

            if (IsInterstitialEnabledInSetup() && ProviderHandlesFormat(providerId, AdFormat.Interstitial))
                TryPreloadInterstitialForProvider(providerId);
        }

        void TryPreloadFullscreenForAllReadyProviders()
        {
            if (!CanPreloadFullscreenForProvider() || _core == null)
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
            if (!CanOperateFullscreen() || provider?.Rewarded == null || _rewardedLoadInFlight)
                return;

            if (IsProviderRewardedLoadedFresh(providerId))
            {
                DebugAds.Log($"[JisAds][Rewarded][preload_skip] mediation={providerId} reason=already_loaded");
                return;
            }

            _rewardedLoadInFlight = true;
            DebugAds.Log($"[JisAds][Rewarded][preload_start] mediation={providerId}");
            provider.Rewarded.Load(
                onLoaded: () =>
                {
                    _rewardedLoadInFlight = false;
                    RecordRewardedLoaded(providerId);
                    OnPreloadSucceeded(StandardAdPreloadFormat.Rewarded);
                    DebugAds.Log($"[JisAds][Rewarded][preload_success] mediation={providerId}");
                },
                onFailed: err =>
                {
                    _rewardedLoadInFlight = false;
                    DebugAds.LogWarning($"[JisAds][Rewarded][preload_fail] mediation={providerId} error={err}");
                    HandlePreloadFailedIfNoFormatReady(StandardAdPreloadFormat.Rewarded);
                });
        }

        void TryPreloadInterstitialForProvider(AdProviderId providerId)
        {
            var provider = _core?.GetProvider(providerId);
            if (!CanOperateFullscreen() || provider?.Interstitial == null || _interstitialLoadInFlight)
                return;

            if (IsProviderInterstitialLoadedFresh(providerId))
            {
                DebugAds.Log($"[JisAds][Interstitial][preload_skip] mediation={providerId} reason=already_loaded");
                return;
            }

            _interstitialLoadInFlight = true;
            DebugAds.Log($"[JisAds][Interstitial][preload_start] mediation={providerId}");
            var loadStartedAt = Time.unscaledTime;
            provider.Interstitial.Load(
                onLoaded: () =>
                {
                    _interstitialLoadInFlight = false;
                    RecordInterstitialLoaded(providerId);
                    OnPreloadSucceeded(StandardAdPreloadFormat.Interstitial);
                    TrackCoreInterstitialLoadSuccess(loadStartedAt);
                    DebugAds.Log($"[JisAds][Interstitial][preload_success] mediation={providerId}");
                },
                onFailed: err =>
                {
                    _interstitialLoadInFlight = false;
                    TrackCoreInterstitialLoadFail(err, loadStartedAt);
                    DebugAds.LogWarning($"[JisAds][Interstitial][preload_fail] mediation={providerId} error={err}");
                    HandlePreloadFailedIfNoFormatReady(StandardAdPreloadFormat.Interstitial);
                });
        }

        void TryFulfillQueuedBannerShow()
        {
            if (!_bannerWantsVisible || !CanShowAds() || !CanOperateBanner())
                return;

            if (_bannerShowInFlight)
                return;

            DebugAds.Log("[JisAds] Fulfilling queued banner show after Core ready.");
            _bannerShowInFlight = true;
            _core.ShowBanner(
                onShown: () =>
                {
                    _bannerShowInFlight = false;
                    DebugAds.Log("[JisAds] Banner shown");
                },
                onFailed: err =>
                {
                    _bannerShowInFlight = false;
                    Debug.LogWarning($"[JisAds] Banner show failed: {err}");
                });
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

            if (_bannerPreloadInFlight)
            {
                DebugAds.Log("[JisAds] Preload Banner skipped: load already in-flight.");
                return;
            }

            var preserveVisible = _bannerWantsVisible;
            _bannerPreloadInFlight = true;
            provider.Banner.Load(
                onLoaded: () =>
                {
                    _bannerPreloadInFlight = false;
                    OnPreloadSucceeded(StandardAdPreloadFormat.Banner);
                    DebugAds.Log("[JisAds] Preload Banner: loaded");
                    if (isStartup)
                        TryShowBannerOnStartIfConfigured();
                    else if (preserveVisible)
                        ShowBannerAds();
                },
                onFailed: err =>
                {
                    _bannerPreloadInFlight = false;
                    DebugAds.LogWarning($"[JisAds] Preload Banner failed: {err}");
                    HandlePreloadFailed(StandardAdPreloadFormat.Banner);
                });
        }

        void PreloadInterstitialAd(bool forceReload = false)
        {
            if (!CanOperateFullscreen() || _core == null)
                return;

            if (!TryBeginInterstitialLoadBatch(forceReload))
            {
                DebugAds.Log("[JisAds][Interstitial][preload_skip] reason=load_in_flight");
                return;
            }

            var providerIds = GetFullscreenPreloadProviderIds(AdFormat.Interstitial);
            LogFullscreenLoadPlan(AdFormat.Interstitial, providerIds);

            var startedAny = false;
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

                if (!forceReload && IsProviderInterstitialLoadedFresh(providerId))
                {
                    DebugAds.Log($"[JisAds][Interstitial][preload_skip] mediation={providerId} reason=already_loaded");
                    continue;
                }

                startedAny = true;
                BeginInterstitialLoadSlot();
                DebugAds.Log($"[JisAds][Interstitial][preload_start] mediation={providerId}");
                var loadStartedAt = Time.unscaledTime;
                provider.Interstitial.Load(
                    onLoaded: () =>
                    {
                        RecordInterstitialLoaded(providerId);
                        OnPreloadSucceeded(StandardAdPreloadFormat.Interstitial);
                        TrackCoreInterstitialLoadSuccess(loadStartedAt);
                        DebugAds.Log($"[JisAds][Interstitial][preload_success] mediation={providerId}");
                        EndInterstitialLoadSlot();
                    },
                    onFailed: err =>
                    {
                        TrackCoreInterstitialLoadFail(err, loadStartedAt);
                        DebugAds.LogWarning($"[JisAds][Interstitial][preload_fail] mediation={providerId} error={err}");
                        HandlePreloadFailedIfNoFormatReady(StandardAdPreloadFormat.Interstitial);
                        EndInterstitialLoadSlot();
                    });
            }

            if (!startedAny)
            {
                _interstitialLoadInFlight = false;
                _interstitialLoadsPending = 0;
            }
        }

        void PreloadRewardedAd(bool forceReload = false)
        {
            if (!CanOperateFullscreen() || _core == null)
                return;

            if (!TryBeginRewardedLoadBatch(forceReload))
            {
                DebugAds.Log("[JisAds][Rewarded][preload_skip] reason=load_in_flight");
                return;
            }

            var providerIds = GetFullscreenPreloadProviderIds(AdFormat.Rewarded);
            LogFullscreenLoadPlan(AdFormat.Rewarded, providerIds);

            var startedAny = false;
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

                if (!forceReload && IsProviderRewardedLoadedFresh(providerId))
                {
                    DebugAds.Log($"[JisAds][Rewarded][preload_skip] mediation={providerId} reason=already_loaded");
                    continue;
                }

                startedAny = true;
                BeginRewardedLoadSlot();
                DebugAds.Log($"[JisAds][Rewarded][preload_start] mediation={providerId}");
                provider.Rewarded.Load(
                    onLoaded: () =>
                    {
                        RecordRewardedLoaded(providerId);
                        OnPreloadSucceeded(StandardAdPreloadFormat.Rewarded);
                        DebugAds.Log($"[JisAds][Rewarded][preload_success] mediation={providerId}");
                        EndRewardedLoadSlot();
                    },
                    onFailed: err =>
                    {
                        DebugAds.LogWarning($"[JisAds][Rewarded][preload_fail] mediation={providerId} error={err}");
                        HandlePreloadFailedIfNoFormatReady(StandardAdPreloadFormat.Rewarded);
                        EndRewardedLoadSlot();
                    });
            }

            if (!startedAny)
            {
                _rewardedLoadInFlight = false;
                _rewardedLoadsPending = 0;
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

            if (_interstitialLoadInFlight)
                return;

            if (IsProviderInterstitialLoadedFresh(providerId))
            {
                DebugAds.Log($"[JisAds][Interstitial][preload_skip] mediation={providerId} reason=already_loaded");
                return;
            }

            _interstitialLoadInFlight = true;
            DebugAds.Log($"[JisAds][Interstitial][preload_start] mediation={providerId}");
            var loadStartedAt = Time.unscaledTime;
            provider.Interstitial.Load(
                onLoaded: () =>
                {
                    _interstitialLoadInFlight = false;
                    RecordInterstitialLoaded(providerId);
                    OnPreloadSucceeded(StandardAdPreloadFormat.Interstitial);
                    TrackCoreInterstitialLoadSuccess(loadStartedAt);
                    DebugAds.Log($"[JisAds][Interstitial][preload_success] mediation={providerId}");
                },
                onFailed: err =>
                {
                    _interstitialLoadInFlight = false;
                    TrackCoreInterstitialLoadFail(err, loadStartedAt);
                    DebugAds.LogWarning($"[JisAds][Interstitial][preload_fail] mediation={providerId} error={err}");
                    HandlePreloadFailed(StandardAdPreloadFormat.Interstitial);
                });
        }

        void PreloadSingleProviderRewarded(AdProviderId providerId)
        {
            var provider = _core?.GetProvider(providerId);
            if (!CanOperateFullscreen() || provider?.Rewarded == null || _core == null || !_core.IsProviderInitialized(providerId))
                return;

            if (_rewardedLoadInFlight)
                return;

            if (IsProviderRewardedLoadedFresh(providerId))
            {
                DebugAds.Log($"[JisAds][Rewarded][preload_skip] mediation={providerId} reason=already_loaded");
                return;
            }

            _rewardedLoadInFlight = true;
            DebugAds.Log($"[JisAds][Rewarded][preload_start] mediation={providerId}");
            provider.Rewarded.Load(
                onLoaded: () =>
                {
                    _rewardedLoadInFlight = false;
                    RecordRewardedLoaded(providerId);
                    OnPreloadSucceeded(StandardAdPreloadFormat.Rewarded);
                    DebugAds.Log($"[JisAds][Rewarded][preload_success] mediation={providerId}");
                },
                onFailed: err =>
                {
                    _rewardedLoadInFlight = false;
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

        static float GetPreloadRetryDelaySeconds(bool singleInventory, int failCount) =>
            RetryBackoff.GetDelaySeconds(failCount);

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
                _forceFullscreenPreloadWithoutRemoteConfig = false;
                StopFullscreenPreloadRemoteConfigWait();
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
            InvalidateProviderOrderCache();
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
            InvalidateProviderOrderCache();
            _core.Initialize(
                onSuccess: () =>
                {
                    DebugAds.LogSdkInit("JisAds", "Core AdManager", true);
                    _coreInitFailCount = 0;
                    StopCoreInitRetry();
                    TryCompleteStartupAfterCoreReady();
                },
                onFailure: err =>
                {
                    DebugAds.LogSdkInit("JisAds", "Core AdManager", false, err);
                    ScheduleCoreInitRetry(err);
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
            InvalidateProviderOrderCache();
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
                AdFormat.Interstitial => IsProviderInterstitialLoadedFresh(provider),
                AdFormat.Rewarded => IsProviderRewardedLoadedFresh(provider),
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

                if (IsProviderInterstitialLoadedFresh(provider))
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

                if (IsProviderRewardedLoadedFresh(provider))
                    return true;
            }

            return false;
        }
        #region Fullscreen load resilience
        void InvalidateProviderOrderCache()
        {
            _providerOrderCacheDirty = true;
            _cachedInterstitialProviderOrder = null;
            _cachedRewardedProviderOrder = null;
        }

        List<AdProviderId> GetCachedFullscreenShowProviderOrder(AdFormat format, AdsMediationType mediation = AdsMediationType.NONE)
        {
            if (mediation != AdsMediationType.NONE)
                return BuildFullscreenShowProviderOrder(mediation, format);

            if (!_providerOrderCacheDirty)
            {
                if (format == AdFormat.Interstitial && _cachedInterstitialProviderOrder != null)
                    return _cachedInterstitialProviderOrder;
                if (format == AdFormat.Rewarded && _cachedRewardedProviderOrder != null)
                    return _cachedRewardedProviderOrder;
            }

            var order = BuildFullscreenShowProviderOrder(AdsMediationType.NONE, format);
            if (format == AdFormat.Interstitial)
                _cachedInterstitialProviderOrder = order;
            else if (format == AdFormat.Rewarded)
                _cachedRewardedProviderOrder = order;
            _providerOrderCacheDirty = false;
            return order;
        }

        void RecordInterstitialLoaded(AdProviderId providerId)
        {
            if (providerId == AdProviderId.None)
                return;
            _interstitialLoadedAtUnscaled[providerId] = Time.unscaledTime;
        }

        void RecordRewardedLoaded(AdProviderId providerId)
        {
            if (providerId == AdProviderId.None)
                return;
            _rewardedLoadedAtUnscaled[providerId] = Time.unscaledTime;
        }

        bool IsProviderInterstitialLoadedFresh(AdProviderId providerId)
        {
            if (_core == null || !_core.IsInterstitialLoaded(providerId))
                return false;

            if (!_interstitialLoadedAtUnscaled.TryGetValue(providerId, out var loadedAt))
            {
                RecordInterstitialLoaded(providerId);
                return true;
            }

            if (Time.unscaledTime - loadedAt <= LoadedAdMaxAgeSec)
                return true;

            DebugAds.Log($"[JisAds][Interstitial][stale] mediation={providerId} age={(Time.unscaledTime - loadedAt):0.#}s");
            _interstitialLoadedAtUnscaled.Remove(providerId);
            if (!_interstitialLoadInFlight)
                PreloadSingleProviderInterstitial(providerId);
            return false;
        }

        bool IsProviderRewardedLoadedFresh(AdProviderId providerId)
        {
            if (_core == null || !_core.IsRewardedLoaded(providerId))
                return false;

            if (!_rewardedLoadedAtUnscaled.TryGetValue(providerId, out var loadedAt))
            {
                RecordRewardedLoaded(providerId);
                return true;
            }

            if (Time.unscaledTime - loadedAt <= LoadedAdMaxAgeSec)
                return true;

            DebugAds.Log($"[JisAds][Rewarded][stale] mediation={providerId} age={(Time.unscaledTime - loadedAt):0.#}s");
            _rewardedLoadedAtUnscaled.Remove(providerId);
            if (!_rewardedLoadInFlight)
                PreloadSingleProviderRewarded(providerId);
            return false;
        }

        bool TryBeginInterstitialLoadBatch(bool forceReload = false)
        {
            if (_interstitialLoadInFlight && !forceReload)
                return false;
            _interstitialLoadInFlight = true;
            _interstitialLoadsPending = 0;
            return true;
        }

        void BeginInterstitialLoadSlot()
        {
            _interstitialLoadsPending++;
        }

        void EndInterstitialLoadSlot()
        {
            _interstitialLoadsPending = Mathf.Max(0, _interstitialLoadsPending - 1);
            if (_interstitialLoadsPending <= 0)
            {
                _interstitialLoadsPending = 0;
                _interstitialLoadInFlight = false;
            }
        }

        bool TryBeginRewardedLoadBatch(bool forceReload = false)
        {
            if (_rewardedLoadInFlight && !forceReload)
                return false;
            _rewardedLoadInFlight = true;
            _rewardedLoadsPending = 0;
            return true;
        }

        void BeginRewardedLoadSlot()
        {
            _rewardedLoadsPending++;
        }

        void EndRewardedLoadSlot()
        {
            _rewardedLoadsPending = Mathf.Max(0, _rewardedLoadsPending - 1);
            if (_rewardedLoadsPending <= 0)
            {
                _rewardedLoadsPending = 0;
                _rewardedLoadInFlight = false;
            }
        }

        void HandleNetworkReachabilityChanged()
        {
            var current = Application.internetReachability;
            if (current == _lastNetworkReachability)
                return;

            var wasOffline = _lastNetworkReachability == NetworkReachability.NotReachable;
            _lastNetworkReachability = current;

            if (current == NetworkReachability.NotReachable || !wasOffline)
                return;

            OnNetworkBecameReachable();
        }

        void OnNetworkBecameReachable()
        {
            DebugAds.Log("[JisAds] Network reachable — resetting preload backoff and re-fetching Remote Config.");
            _preloadFailCounts[PreloadFormatIndex(StandardAdPreloadFormat.Interstitial)] = 0;
            _preloadFailCounts[PreloadFormatIndex(StandardAdPreloadFormat.Rewarded)] = 0;
            StopPreloadRetryCoroutine(StandardAdPreloadFormat.Interstitial);
            StopPreloadRetryCoroutine(StandardAdPreloadFormat.Rewarded);
            _interstitialLoadedAtUnscaled.Clear();
            _rewardedLoadedAtUnscaled.Clear();
            RequestInterstitialLoadIfNeeded();
            RequestRewardedLoadIfNeeded();
            TriggerRemoteConfigFetchAfterNetworkRecovery();
        }

        void StartNetworkReachabilityPolling()
        {
            if (!enableNetworkReachabilityPolling || settings == null)
                return;

            StopNetworkReachabilityPolling();
            if (networkReachabilityPollIntervalSec <= 0f)
                return;

            _networkReachabilityPollCoroutine = StartCoroutine(CoPollNetworkReachability());
        }

        void StopNetworkReachabilityPolling()
        {
            if (_networkReachabilityPollCoroutine == null)
                return;

            StopCoroutine(_networkReachabilityPollCoroutine);
            _networkReachabilityPollCoroutine = null;
        }

        IEnumerator CoPollNetworkReachability()
        {
            var interval = Mathf.Max(1f, networkReachabilityPollIntervalSec);
            var wait = new WaitForSecondsRealtime(interval);
            while (true)
            {
                yield return wait;
                HandleNetworkReachabilityChanged();
            }
        }

        void TriggerRemoteConfigFetchAfterNetworkRecovery()
        {
            if (_isRemoveAds || _networkRecoveryRemoteConfigFetchInFlight || _isApplyingRemoteConfig)
                return;

            _networkRecoveryRemoteConfigFetchInFlight = true;
            _ = FetchRemoteConfigAfterNetworkRecoveryAsync();
        }

        async Task FetchRemoteConfigAfterNetworkRecoveryAsync()
        {
            try
            {
                var fetchSucceeded = await FetchAndApplyRemoteConfigAsync();
                DebugAds.Log(
                    fetchSucceeded
                        ? "[JisAds] Network recovery Remote Config fetch succeeded."
                        : "[JisAds] Network recovery Remote Config fetch finished with defaults/failure — preloads re-armed.");
            }
            catch (Exception ex)
            {
                DebugAds.LogWarning($"[JisAds] Network recovery Remote Config fetch error: {ex.Message}");
            }
            finally
            {
                _networkRecoveryRemoteConfigFetchInFlight = false;
            }
        }

        void BindProviderEvents() => AdEvents.OnProviderFailed += HandleProviderFailed;

        void UnbindProviderEvents() => AdEvents.OnProviderFailed -= HandleProviderFailed;

        void HandleProviderFailed(AdFailureInfo failure)
        {
            if (!useCoreForStandardFormats || _core == null)
                return;

            var providerId = ParseProviderId(failure.ProviderId);
            if (providerId == AdProviderId.None)
                return;

            ScheduleProviderInitRetry(providerId);
        }

        void ScheduleProviderInitRetry(AdProviderId providerId)
        {
            if (_providerInitRetryCoroutines.TryGetValue(providerId, out var existing) && existing != null)
                return;

            _providerInitFailCounts.TryGetValue(providerId, out var count);
            count++;
            _providerInitFailCounts[providerId] = count;
            if (count > MaxProviderInitRetries)
            {
                DebugAds.LogWarning($"[JisAds] Provider init retry exhausted mediation={providerId}");
                return;
            }

            var delay = RetryBackoff.GetDelaySeconds(count);
            DebugAds.Log($"[JisAds] Provider init retry #{count} mediation={providerId} in {delay:0.#}s");
            _providerInitRetryCoroutines[providerId] = StartCoroutine(CoRetryProviderInit(providerId, delay));
        }

        IEnumerator CoRetryProviderInit(AdProviderId providerId, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _providerInitRetryCoroutines[providerId] = null;

            if (!useCoreForStandardFormats || _core == null)
                yield break;

            _core.RetryInitializeProvider(
                providerId,
                onSuccess: () =>
                {
                    _providerInitFailCounts[providerId] = 0;
                    OnProviderBecameReady(providerId);
                },
                onFailure: err =>
                    DebugAds.LogWarning($"[JisAds] Provider init retry failed mediation={providerId} error={err}"));
        }

        void StopAllProviderInitRetries()
        {
            foreach (var kvp in _providerInitRetryCoroutines)
            {
                if (kvp.Value != null)
                    StopCoroutine(kvp.Value);
            }
            _providerInitRetryCoroutines.Clear();
        }

        void ScheduleCoreInitRetry(string error)
        {
            if (_coreInitRetryCoroutine != null)
                return;

            _coreInitFailCount++;
            if (_coreInitFailCount > MaxCoreInitRetries)
            {
                DebugAds.LogSdkInit("JisAds", "Core AdManager", false, $"{error} (retries exhausted)");
                useCoreForStandardFormats = false;
                _pendingRecoverFullscreenPreloadsAfterCoreReady = false;
                _pendingImmediatePreloadAfterCoreReady = false;
                _pendingFullscreenPreloadAfterRemoteConfig = false;
                return;
            }

            var delay = RetryBackoff.GetDelaySeconds(_coreInitFailCount);
            DebugAds.LogWarning($"[JisAds] Core init retry #{_coreInitFailCount} in {delay:0.#}s error={error}");
            _coreInitRetryCoroutine = StartCoroutine(CoRetryCoreInit(delay));
        }

        IEnumerator CoRetryCoreInit(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _coreInitRetryCoroutine = null;

            if (_core == null || settings == null)
                yield break;

            _core.Initialize(
                onSuccess: () =>
                {
                    DebugAds.LogSdkInit("JisAds", "Core AdManager", true);
                    _coreInitFailCount = 0;
                    TryCompleteStartupAfterCoreReady();
                },
                onFailure: ScheduleCoreInitRetry);
        }

        void StopCoreInitRetry()
        {
            if (_coreInitRetryCoroutine == null)
                return;
            StopCoroutine(_coreInitRetryCoroutine);
            _coreInitRetryCoroutine = null;
        }

        /// <summary>
        /// Re-arms interstitial preload after tier ladder give-up or prolonged NoFill backoff.
        /// Call before important gameplay moments (e.g. entering a level).
        /// </summary>
        public void ForceRearmInterstitialLoad()
        {
            if (!CanShowAds())
                return;

            _preloadFailCounts[PreloadFormatIndex(StandardAdPreloadFormat.Interstitial)] = 0;
            StopPreloadRetryCoroutine(StandardAdPreloadFormat.Interstitial);
            _interstitialLoadInFlight = false;
            _interstitialLoadsPending = 0;
            _interstitialLoadedAtUnscaled.Clear();

            if (!CanOperateFullscreen())
            {
                DebugAds.LogWarning("[JisAds] ForceRearmInterstitialLoad skipped — Core not ready.");
                return;
            }

            DebugAds.Log("[JisAds] ForceRearmInterstitialLoad — warm-loading interstitial.");
            PreloadInterstitialAd(forceReload: true);
        }

        /// <summary>
        /// Re-arms rewarded preload after tier ladder give-up or prolonged NoFill backoff.
        /// </summary>
        public void ForceRearmRewardedLoad()
        {
            if (!CanShowAds())
                return;

            _preloadFailCounts[PreloadFormatIndex(StandardAdPreloadFormat.Rewarded)] = 0;
            StopPreloadRetryCoroutine(StandardAdPreloadFormat.Rewarded);
            _rewardedLoadInFlight = false;
            _rewardedLoadsPending = 0;
            _rewardedLoadedAtUnscaled.Clear();

            if (!CanOperateFullscreen())
            {
                DebugAds.LogWarning("[JisAds] ForceRearmRewardedLoad skipped — Core not ready.");
                return;
            }

            DebugAds.Log("[JisAds] ForceRearmRewardedLoad — warm-loading rewarded.");
            PreloadRewardedAd(forceReload: true);
        }
        #endregion
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
            else
                RestoreAdsAfterRemoveAdsRevoked();

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

        void RestoreAdsAfterRemoveAdsRevoked()
        {
            _immediateFormatsPreloadedOnCoreReady = false;
            _fullscreenFormatsPreloadedAfterRemoteConfig = false;
            _bannerStartupPreloadStarted = false;
            _appOpenStartupPreloadStarted = false;
            _pendingRecoverFullscreenPreloadsAfterCoreReady = false;
            DebugAds.Log("[JisAds] Remove Ads revoked — re-arming startup preloads.");
            TryCompleteStartupAfterCoreReady();
        }
        #endregion
        #region Standard formats
        public void ShowInterstitial(
            UnityAction closedCallback = null,
            UnityAction showSuccessCallback = null,
            UnityAction showFailCallback = null,
            bool isTracking = true,
            bool isSkipCapping = false) =>
            ShowInterstitial("default", AdsMediationType.NONE, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);

        public void ShowInterstitial(
            AdsMediationType mediation,
            UnityAction closedCallback = null,
            UnityAction showSuccessCallback = null,
            UnityAction showFailCallback = null,
            bool isTracking = true,
            bool isSkipCapping = false) =>
            ShowInterstitial("default", mediation, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);

        public void ShowInterstitialAuto(
            UnityAction closedCallback = null,
            UnityAction showSuccessCallback = null,
            UnityAction showFailCallback = null,
            bool isTracking = true,
            bool isSkipCapping = false) =>
            ShowInterstitial("auto", AdsMediationType.NONE, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);

        public void ShowInterstitialAuto(
            string placement,
            UnityAction closedCallback = null,
            UnityAction showSuccessCallback = null,
            UnityAction showFailCallback = null,
            bool isTracking = true,
            bool isSkipCapping = false)
        {
            if (string.IsNullOrEmpty(placement))
                placement = "auto";

            ShowInterstitial(placement, AdsMediationType.NONE, closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
        }

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
            var providerOrder = GetCachedFullscreenShowProviderOrder(AdFormat.Interstitial, mediation);
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
                    },
                    showAttemptId: _interstitialShowAttemptId);
                return;
            }
            Debug.LogWarning("[JisAds] Legacy interstitial is removed. Enable Core AdManager.");
            TrackPendingInterstitialShowFailure(isTracking);
            ConsumePendingInterstitialCallbacksOnFail();
        }

        static float GetLoadRequestTimeSeconds(float loadStartedAtUnscaled)
        {
            if (loadStartedAtUnscaled <= 0f)
                return 0f;

            return Mathf.Round(Mathf.Max(0f, Time.unscaledTime - loadStartedAtUnscaled) * 2f) / 2f;
        }

        void TrackCoreInterstitialLoadSuccess(float loadStartedAtUnscaled)
        {
            var tracker = AdsTracker.Instance;
            if (tracker == null)
            {
                DebugAds.LogWarning("[JisAds] AdsTracker.Instance is null. Skipping interstitial load-success tracking.");
                return;
            }

            tracker.TrackAdsInterstitial_LoadedSuccess(GetLoadRequestTimeSeconds(loadStartedAtUnscaled));
        }

        void TrackCoreInterstitialLoadFail(string error, float loadStartedAtUnscaled)
        {
            var tracker = AdsTracker.Instance;
            if (tracker == null)
            {
                DebugAds.LogWarning("[JisAds] AdsTracker.Instance is null. Skipping interstitial load-fail tracking.");
                return;
            }

            tracker.TrackAdsInterstitial_LoadedFail(error ?? string.Empty, GetLoadRequestTimeSeconds(loadStartedAtUnscaled));
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
                tracker.TrackAdsInterstitial_ShowFailByLoad(_pendingInterstitialPlacement);
            else
                tracker.TrackAdsInterstitial_ShowFail(_pendingInterstitialPlacement);
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
            if (format == AdFormat.Interstitial)
            {
                var id = ParseProviderId(providerId);
                RecordInterstitialLoaded(id);
                TrackCoreInterstitialLoadSuccess(0f);
            }

            DebugAds.Log($"[JisAds][{format}][load_success] mediation={providerId}");
        }

        void OnCoreRewardedLoaded(AdFormat format, string providerId)
        {
            if (format == AdFormat.Rewarded)
            {
                var id = ParseProviderId(providerId);
                RecordRewardedLoaded(id);
            }

            DebugAds.Log($"[JisAds][{format}][load_success] mediation={providerId}");
        }

        void OnCoreInterstitialShown(AdFormat format, int showAttemptId)
        {
            if (format != AdFormat.Interstitial)
                return;

            if (!_interstitialCallbacksInFlight || showAttemptId != _interstitialShowAttemptId)
                return;

            _pendingInterstitialWasShown = true;
            TrackPendingInterstitialShowSuccess();
            ResetCoreInterstitialBetweenShowsCooldown();
        }

        void OnCoreRewardEarned(AdFormat format)
        {
            if (format != AdFormat.Rewarded)
                return;
            // Requirement: watching a rewarded ad resets type-2 timer as well.
            ResetCoreInterstitialBetweenShowsCooldown();
        }
        /// <summary>Interstitial for foreground resume — bypasses gameplay capping; uses shared in-flight guard.</summary>
        public void ShowInterstitialForResume(System.Action onClosed = null, System.Action<string> onFailed = null)
        {
            if (_interstitialCallbacksInFlight)
            {
                onFailed?.Invoke("Previous interstitial still in-flight.");
                return;
            }

            if (!UseCoreForStandardFormats)
            {
                onFailed?.Invoke("Resume interstitial unavailable — Core AdManager not initialized.");
                return;
            }

            if (!CanOperateFullscreen())
            {
                RequestInterstitialLoadIfNeeded();
                onFailed?.Invoke("Core not ready.");
                return;
            }

            var providerOrder = GetCachedFullscreenShowProviderOrder(AdFormat.Interstitial);
            if (!IsInterstitialAdLoaded(providerOrder))
            {
                RequestInterstitialLoadIfNeeded();
                onFailed?.Invoke("No interstitial loaded.");
                return;
            }

            _interstitialCallbacksInFlight = true;
            _interstitialShowAttemptId++;
            var attemptId = _interstitialShowAttemptId;
            StartInterstitialInFlightWatchdog(attemptId);
            _pendingInterstitialIsTracking = false;
            _pendingInterstitialPlacement = "resume";
            _pendingInterstitialWasShown = false;

            SetAdsShowingState(true);
            HideBannerForFullscreenAd("interstitial_resume");

            var provider = SelectProviderForShow(providerOrder, AdFormat.Interstitial);
            var fallback = SelectFallbackProvider(providerOrder, provider);
            if (provider == AdProviderId.None)
            {
                ConsumePendingInterstitialCallbacksOnFail();
                onFailed?.Invoke("No mediation provider registered.");
                return;
            }

            _core.ShowInterstitial(
                provider,
                fallback,
                onClosed: () =>
                {
                    var closed = onClosed;
                    ClearPendingInterstitialCallbacks();
                    closed?.Invoke();
                    ScheduleBannerRestoreAfterFullscreenAd("interstitial_resume");
                    RequestInterstitialLoadIfNeeded();
                },
                onFailed: err =>
                {
                    var failed = onFailed;
                    ClearPendingInterstitialCallbacks();
                    failed?.Invoke(err);
                    ScheduleBannerRestoreAfterFullscreenAd("interstitial_resume");
                    RequestInterstitialLoadIfNeeded();
                },
                showAttemptId: attemptId);
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
            var providerOrder = GetCachedFullscreenShowProviderOrder(AdFormat.Rewarded, mediation);
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

            if (_bannerShowInFlight)
                return;

            _bannerShowInFlight = true;
            _core.ShowBanner(
                onShown: () =>
                {
                    _bannerShowInFlight = false;
                    DebugAds.Log("[JisAds] Banner shown");
                },
                onFailed: err =>
                {
                    _bannerShowInFlight = false;
                    Debug.LogWarning($"[JisAds] Banner show failed: {err}");
                });
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
            _bannerShowInFlight = false;
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

            CancelPendingBannerRestore();
            CancelPendingBannerPauseRestore();
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

            // Fullscreen close is authoritative. Dismissing a native ad can also emit app-resume;
            // both paths restoring would reload the banner that the first path has just shown.
            StopBannerAutoRefresh();
            CancelPendingBannerPauseRestore();
            CancelPendingBannerRestore();
            var generation = _bannerRestoreGeneration;
            // Wait for the post-fullscreen orientation/safe-area values to settle before the one
            // intentional native reload. The app-resume fallback has already paid this debounce.
            var restoreDelay = reason == "app_resume"
                ? Mathf.Max(0f, bannerRestoreDelaySec)
                : Mathf.Max(bannerRestoreDelaySec, bannerRestoreDebounceSec);
            _bannerRestoreCoroutine = StartCoroutine(
                CoRestoreBannerAfterFullscreenAd(reason, generation, restoreDelay));
        }

        void ScheduleBannerRestoreOnAppResume()
        {
            if (!restoreBannerAfterFullscreenAds || !CanShowAds() || !UseCoreForStandardFormats)
                return;
            if (!_bannerWantsVisible || IsShowingAnyAd())
                return;

            // A fullscreen close callback already owns the pending reload. Do not create a second
            // app-resume timer for the same native transition.
            if (_bannerRestoreCoroutine != null)
                return;

            CancelPendingBannerPauseRestore();
            _bannerPauseRestoreCoroutine = StartCoroutine(CoDebouncedBannerRestoreOnAppResume());
        }

        void CancelPendingBannerRestore()
        {
            // Native load callbacks can still arrive after StopCoroutine.
            _bannerRestoreGeneration++;

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

        IEnumerator CoRestoreBannerAfterFullscreenAd(string reason, int generation, float restoreDelay)
        {
            if (restoreDelay > 0f)
                yield return new WaitForSecondsRealtime(restoreDelay);

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

                // Recreate the native view exactly once after orientation/safe-area has settled.
                // AdMob happened to destroy on Load already; MAX requires an explicit Destroy.
                bannerProvider.Banner.Destroy();
                bannerProvider.Banner.Load(
                    onLoaded: () =>
                    {
                        if (generation != _bannerRestoreGeneration ||
                            !BannerRestorePreconditionsMet() || IsShowingAnyAd())
                        {
                            done = true;
                            succeeded = true; // intent changed — stop retrying.
                            return;
                        }

                        _core.ShowBanner(
                            onShown: () =>
                            {
                                DebugAds.Log($"{BannerRestoreLogPrefix} restore shown reason={reason}");
                                RestartBannerAutoRefresh();
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
            if (BannerRestorePreconditionsMet())
                RestartBannerAutoRefresh();
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

            // The fullscreen close path may still be restoring, or may already have completed.
            // The app-resume fallback must not start a destructive second reload.
            if (_bannerRestoreCoroutine != null)
            {
                DebugAds.Log($"{BannerRestoreLogPrefix} app_resume skipped_restore_already_handled");
                yield break;
            }

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
            IsInterstitialAdLoaded(GetCachedFullscreenShowProviderOrder(AdFormat.Interstitial));

        public bool CanShowInterstitialAd() => IsInterstitialAdLoaded();

        public bool IsRewardedVideoLoaded() =>
            IsRewardedVideoLoaded(GetCachedFullscreenShowProviderOrder(AdFormat.Rewarded));
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
