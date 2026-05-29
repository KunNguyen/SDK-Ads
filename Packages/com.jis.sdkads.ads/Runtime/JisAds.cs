using System.Collections;
using System.Threading.Tasks;
using JisSDKAds.Ads.AppOpen;
using JisSDKAds.Ads.Integration;
using JisSDKAds.Ads.Resume;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Common;
using JisSDKAds.Ads.Tiered;
using JisSDKAds.Core;
using JisSDKAds.Core.Interfaces;
using JisSDKAds.Core.Events;
using JisSDKAds.Core.Models;
using JisSDKAds.Core.Tiered;
using JisSDKAds.Core.Tiered.Ads;
using JisSDKAds.Core.Tiered.Config;
using JisSDKAds.Core.Tiered.Models;
using JisSDKAds.Firebase;
using UnityEngine;
using UnityEngine.Events;
namespace JisSDKAds.Ads
{
    /// <summary>
    /// Unified ads entry point: Core <see cref="AdManager"/> + tiered inventory;
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
        private AdManager _core;
        private TieredAdsExtension _tiered;
        private AppOpenAdService _appOpen;
        private bool _isReady;
        private bool _isShowingAd;
        private bool _isRemoveAds;
        private float _appOpenUnscaledTime;
        private float _interstitialNextAllowedUnscaledTime;
        private bool _pendingInterstitialWasShown;
        private UnityAction _pendingInterstitialClosedCallback;
        private UnityAction _pendingInterstitialShowSuccessCallback;
        private UnityAction _pendingInterstitialShowFailCallback;
        private bool _interstitialCallbacksInFlight;
        private int _interstitialShowAttemptId;

        private bool _pendingRewardedWasShown;
        private bool _pendingRewardedRewardGranted;
        private UnityAction<bool> _pendingRewardedClosedCallback;
        private UnityAction _pendingRewardedRewardCallback;
        private UnityAction _pendingRewardedFailCallback;
        private bool _rewardedCallbacksInFlight;
        private int _rewardedShowAttemptId;
        private bool _standardFormatsPreloadedAfterRemoteConfig;
        private int _preloadRetryAttempt;
        private const int PreloadMaxRetries = 3;
        private bool _preloadRetryScheduled;
        public bool IsReady => _isReady;
        public bool UseCoreForStandardFormats => useCoreForStandardFormats && _core != null && _core.IsInitialized;
        public bool UseTieredInterstitial =>
            _tiered != null && _tiered.IsTieredForInterstitial;
        public bool UseTieredRewarded =>
            _tiered != null && _tiered.IsTieredForRewarded;
        public bool HasAppOpenSupport =>
            _core != null
            && _core.IsInitialized
            && _core.PrimaryProvider?.AppOpen is not NullAppOpenAd;
        public JisSDKAdsSettings Settings => settings;
        public AdManager Core => _core;
        public TieredAdsExtension Tiered => _tiered;
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
            BindCoreCappingEvents();
            _appOpen.ConfigureColdStart(
                showAppOpenOnColdStart,
                appOpenFirstShowDelayMs,
                appOpenFirstShowWaitLoadTimeoutSec,
                appOpenMinIntervalBetweenShowsSec);
        }
        private void OnApplicationPause(bool pauseStatus)
        {
            _tiered?.OnApplicationPause(pauseStatus);
            resumeCoordinator?.OnApplicationPause(pauseStatus);
        }
        private void OnDestroy()
        {
            if (Instance == this)
                _tiered?.Shutdown();
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
            ApplyRemoteTierInventoryFromSettings();
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
                if (ShouldPreloadAdsOnGameStart())
                {
                    PreloadStandardFormatsAfterRemoteConfig();
                    TryShowBannerOnStartIfConfigured();
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

            // Only preload when Remote Config has been fetched+activated.
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
                return;

            if (_core == null || !_core.IsInitialized)
                return;

            var provider = _core.PrimaryProvider;
            if (provider == null)
                return;

            _standardFormatsPreloadedAfterRemoteConfig = true;

            provider.Banner?.Load(
                onLoaded: () =>
                {
                    DebugAds.Log("[JisAds] Preload Banner: loaded");
                    TryShowBannerOnStartIfConfigured();
                },
                onFailed: err =>
                {
                    DebugAds.LogWarning($"[JisAds] Preload Banner failed: {err}");
                    SchedulePreloadRetry();
                });

            provider.Interstitial?.Load(
                onLoaded: () => DebugAds.Log("[JisAds] Preload Interstitial: loaded"),
                onFailed: err =>
                {
                    DebugAds.LogWarning($"[JisAds] Preload Interstitial failed: {err}");
                    SchedulePreloadRetry();
                });

            provider.Rewarded?.Load(
                onLoaded: () => DebugAds.Log("[JisAds] Preload Rewarded: loaded"),
                onFailed: err =>
                {
                    DebugAds.LogWarning($"[JisAds] Preload Rewarded failed: {err}");
                    SchedulePreloadRetry();
                });
        }

        void SchedulePreloadRetry()
        {
            if (!ShouldPreloadAdsOnGameStart())
                return;

            if (_preloadRetryScheduled)
                return;

            if (_preloadRetryAttempt >= PreloadMaxRetries)
                return;

            _preloadRetryScheduled = true;
            StartCoroutine(CoRetryPreloadStandardFormats());
        }

        IEnumerator CoRetryPreloadStandardFormats()
        {
            // simple backoff: 2s, 5s, 10s
            _preloadRetryAttempt++;
            var delay = _preloadRetryAttempt switch
            {
                1 => 2f,
                2 => 5f,
                _ => 10f
            };

            yield return new WaitForSecondsRealtime(delay);
            _preloadRetryScheduled = false;

            if (!ShouldPreloadAdsOnGameStart())
                yield break;

            // Allow re-attempts by resetting the one-shot flag.
            _standardFormatsPreloadedAfterRemoteConfig = false;
            PreloadStandardFormatsAfterRemoteConfig();
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
        void ApplyRemoteTierInventoryFromSettings()
        {
            if (settings == null) return;
            var profile = settings.GetActiveProfile();
            if (profile == null) return;

            SequentialTierRemoteConfigResolver.ApplyResolvedIdsToAdmobSetup(profile);

            var tieredConfig = ResolveTieredConfig(profile);
            if (tieredConfig != null && tieredConfig.EnableTieredInventory)
                TieredAdsConfigFactory.ApplyRemoteTierIdsWithFallback(profile, tieredConfig);
        }

        void InitializeCoreFlow()
        {
            var profile = settings.GetActiveProfile();
            if (profile == null) return;
            var tieredConfig = ResolveTieredConfig(profile);
            TryInitializeTieredExtension(profile, tieredConfig);
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
                if (_tiered == null)
                {
                    Debug.LogWarning($"[JisAds] No Core provider for {profile.mediation}.");
                    useCoreForStandardFormats = false;
                }
                return;
            }
            var provider = providerConfig.CreateProvider();
            provider = DecorateSequentialAdsIfEnabled(provider, profile);
            if (_tiered != null && tieredConfig != null && tieredConfig.EnableTieredInventory)
                provider = new TieredAdServiceWrapper(provider, tieredConfig, _tiered.Manager);
            _core.RegisterProvider(providerConfig.ProviderId, provider);
            _core.Initialize(
                onSuccess: () => DebugAds.LogSdkInit("JisAds", "Core AdManager", true),
                onFailure: err =>
                {
                    DebugAds.LogSdkInit("JisAds", "Core AdManager", false, err);
                    if (_tiered == null)
                        useCoreForStandardFormats = false;
                });
        }
        void TryInitializeTieredExtension(PlatformAdsProfile profile, TieredAdsConfig tieredConfig)
        {
            if (tieredConfig == null || !tieredConfig.EnableTieredInventory)
                return;
            var backend = TieredAdBackendFactory.Create(profile);
            if (backend == null)
            {
                Debug.LogWarning("[JisAds] Tiered inventory enabled but no ITieredAdBackend for current mediation.");
                return;
            }
            TieredAdsConfigFactory.ApplyLegacyFallbackFromSdkSetup(profile, tieredConfig);
            if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsRemoteConfigReady)
                TieredAdsConfigFactory.ApplyRemoteTierIdsWithFallback(profile, tieredConfig);
            _tiered = TieredAdsBootstrap.CreateExtension(tieredConfig, backend, transform);
            if (_tiered != null)
                Debug.Log("[JisAds] Tiered inventory extension ready.");
        }
        static TieredAdsConfig ResolveTieredConfig(PlatformAdsProfile profile) =>
            profile?.tieredAdsConfig;

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
        void InitializeTieredOnlyFlow()
        {
            var profile = settings.GetActiveProfile();
            if (profile == null) return;
            var tieredConfig = ResolveTieredConfig(profile);
            TryInitializeTieredExtension(profile, tieredConfig);
            if (_tiered == null)
                return;
            var providerConfig = ProviderConfigFactory.CreateFromSdkSetup(profile);
            if (providerConfig == null)
            {
                Debug.LogWarning("[JisAds] Tiered inventory requires a provider config.");
                return;
            }
            var provider = providerConfig.CreateProvider();
            provider.Initialize(
                onSuccess: () =>
                {
                    _tiered.Manager.SetProviderReady(true);
                    Debug.Log("[JisAds] Tiered provider SDK ready.");
                },
                onFailure: err => Debug.LogWarning($"[JisAds] Tiered provider init failed: {err}"));
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
            _preloadRetryScheduled = false;
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
            _pendingInterstitialWasShown = false;
            _pendingInterstitialClosedCallback = closedCallback;
            _pendingInterstitialShowSuccessCallback = showSuccessCallback;
            _pendingInterstitialShowFailCallback = showFailCallback;

            if (UseTieredInterstitial)
            {
                if (!CanShowAds())
                {
                    ConsumePendingInterstitialCallbacksOnFail();
                    return;
                }
                _tiered.Manager.ShowInterstitial(
                    onClosed: () =>
                    {
                        ConsumePendingInterstitialCallbacksOnClose();
                    },
                    onFailed: err =>
                    {
                        Debug.LogWarning($"[JisAds] Tiered interstitial failed: {err}");
                        ConsumePendingInterstitialCallbacksOnFail();
                    });
                return;
            }
            if (UseCoreForStandardFormats)
            {
                _core.ShowInterstitial(
                    onClosed: () =>
                    {
                        ConsumePendingInterstitialCallbacksOnClose();
                    },
                    onFailed: err =>
                    {
                        Debug.LogWarning($"[JisAds] Core interstitial failed: {err}");
                        ConsumePendingInterstitialCallbacksOnFail();
                    });
                return;
            }
            Debug.LogWarning("[JisAds] Legacy interstitial is removed. Enable Core or Tiered inventory.");
            ConsumePendingInterstitialCallbacksOnFail();
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
        }

        void ConsumePendingInterstitialCallbacksOnFail()
        {
            if (!_interstitialCallbacksInFlight)
                return;

            var onFail = _pendingInterstitialShowFailCallback;
            ClearPendingInterstitialCallbacks();
            onFail?.Invoke();
        }

        void ClearPendingInterstitialCallbacks()
        {
            _pendingInterstitialWasShown = false;
            _pendingInterstitialClosedCallback = null;
            _pendingInterstitialShowSuccessCallback = null;
            _pendingInterstitialShowFailCallback = null;
            _interstitialCallbacksInFlight = false;
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
                _pendingInterstitialWasShown = true;
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
            if (UseTieredInterstitial)
            {
                if (!CanShowAds())
                {
                    onFailed?.Invoke("Ads blocked");
                    return;
                }
                _tiered.Manager.ShowInterstitial(onClosed, onFailed);
                return;
            }
            if (UseCoreForStandardFormats)
            {
                _core.ShowInterstitial(onClosed, onFailed);
                return;
            }
            onFailed?.Invoke("Legacy resume interstitial removed. Enable Core or Tiered inventory.");
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
            _pendingRewardedWasShown = false;
            _pendingRewardedRewardGranted = false;
            _pendingRewardedRewardCallback = successCallback;
            _pendingRewardedClosedCallback = closedCallback;
            _pendingRewardedFailCallback = failedCallback;

            if (UseTieredRewarded)
            {
                _tiered.Manager.ShowRewarded(
                    onRewardEarned: ConsumePendingRewardedCallbacksOnRewardGranted,
                    onClosed: () => ConsumePendingRewardedCallbacksOnClose(true),
                    onFailed: err =>
                    {
                        Debug.LogWarning($"[JisAds] Tiered rewarded failed: {err}");
                        ConsumePendingRewardedCallbacksOnFail();
                    });
                return;
            }
            if (UseCoreForStandardFormats)
            {
                _core.ShowRewarded(
                    onRewardEarned: ConsumePendingRewardedCallbacksOnRewardGranted,
                    onClosed: () => ConsumePendingRewardedCallbacksOnClose(true),
                    onFailed: err =>
                    {
                        Debug.LogWarning($"[JisAds] Core rewarded failed: {err}");
                        ConsumePendingRewardedCallbacksOnFail();
                    });
                return;
            }
            Debug.LogWarning("[JisAds] Legacy rewarded is removed. Enable Core or Tiered inventory.");
            ConsumePendingRewardedCallbacksOnFail();
        }

        void ConsumePendingRewardedCallbacksOnRewardGranted()
        {
            if (!_rewardedCallbacksInFlight || _pendingRewardedRewardGranted)
                return;
            _pendingRewardedRewardGranted = true;
            _pendingRewardedRewardCallback?.Invoke();
        }

        void ConsumePendingRewardedCallbacksOnClose(bool closedOk)
        {
            if (!_rewardedCallbacksInFlight)
                return;

            var onClosed = _pendingRewardedClosedCallback;
            ClearPendingRewardedCallbacks();
            onClosed?.Invoke(closedOk);
        }

        void ConsumePendingRewardedCallbacksOnFail()
        {
            if (!_rewardedCallbacksInFlight)
                return;

            var onFail = _pendingRewardedFailCallback;
            ClearPendingRewardedCallbacks();
            onFail?.Invoke();
        }

        void ClearPendingRewardedCallbacks()
        {
            _pendingRewardedWasShown = false;
            _pendingRewardedRewardGranted = false;
            _pendingRewardedClosedCallback = null;
            _pendingRewardedRewardCallback = null;
            _pendingRewardedFailCallback = null;
            _rewardedCallbacksInFlight = false;
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
                _core.ShowBanner(
                    onShown: () => DebugAds.Log("[JisAds] Banner shown"),
                    onFailed: err => Debug.LogWarning($"[JisAds] Banner show failed: {err}"));
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
            if (UseCoreForStandardFormats)
            {
                _core.HideBanner();
                return;
            }
        }
        public bool IsInterstitialAdLoaded()
        {
            if (UseTieredInterstitial)
                return _tiered.Manager.IsAnyLoaded(AdsFormatType.Interstitial);
            return UseCoreForStandardFormats
                ? _core.PrimaryProvider?.Interstitial.IsLoaded ?? false
                : false;
        }
        public bool CanShowInterstitialAd() => IsInterstitialAdLoaded();
        public bool IsRewardedVideoLoaded()
        {
            if (UseTieredRewarded)
                return _tiered.Manager.IsAnyLoaded(AdsFormatType.Rewarded);
            return UseCoreForStandardFormats
                ? _core.PrimaryProvider?.Rewarded.IsLoaded ?? false
                : false;
        }
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
