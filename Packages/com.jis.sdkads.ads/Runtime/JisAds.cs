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
            EnsureResumeCoordinator();
            _appOpen = new AppOpenAdService(this, this);
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
        }
        private void Start()
        {
            if (autoInitializeOnStart)
                _ = InitializeAsync();
        }
        public async Task InitializeAsync(bool fetchRemoteConfig = true)
        {
            settings?.ApplyRuntimeDebugSettings();
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
                PreloadStandardFormatsAfterRemoteConfig();
                TryShowBannerOnStartIfConfigured();
                StartCoroutine(CoInitializeAppOpenAndResume());
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

            // Only preload when Remote Config has been fetched+activated.
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
                return;

            if (_core == null || !_core.IsInitialized)
                return;

            var provider = _core.PrimaryProvider;
            if (provider == null)
                return;

            _standardFormatsPreloadedAfterRemoteConfig = true;

            // Banner + Interstitial are typically disabled by "remove ads".
            if (!_isRemoveAds)
            {
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
            }

            // Rewarded should remain available even if remove-ads is enabled.
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
        public void SetRemoveAds(bool isRemove) => _isRemoveAds = isRemove;
        public bool IsRemoveAds => _isRemoveAds;
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
            if (UseTieredInterstitial)
            {
                if (!CanShowAds())
                {
                    showFailCallback?.Invoke();
                    return;
                }
                _tiered.Manager.ShowInterstitial(
                    onClosed: () =>
                    {
                        showSuccessCallback?.Invoke();
                        closedCallback?.Invoke();
                    },
                    onFailed: err =>
                    {
                        Debug.LogWarning($"[JisAds] Tiered interstitial failed: {err}");
                        showFailCallback?.Invoke();
                    });
                return;
            }
            if (UseCoreForStandardFormats)
            {
                _core.ShowInterstitial(
                    onClosed: () => closedCallback?.Invoke(),
                    onFailed: err =>
                    {
                        Debug.LogWarning($"[JisAds] Core interstitial failed: {err}");
                        showFailCallback?.Invoke();
                    });
                return;
            }
            Debug.LogWarning("[JisAds] Legacy interstitial is removed. Enable Core or Tiered inventory.");
            showFailCallback?.Invoke();
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
            if (UseTieredRewarded)
            {
                if (!CanShowAds())
                {
                    failedCallback?.Invoke();
                    return;
                }
                _tiered.Manager.ShowRewarded(
                    onRewardEarned: () => successCallback?.Invoke(),
                    onClosed: () => closedCallback?.Invoke(true),
                    onFailed: err =>
                    {
                        Debug.LogWarning($"[JisAds] Tiered rewarded failed: {err}");
                        failedCallback?.Invoke();
                    });
                return;
            }
            if (UseCoreForStandardFormats)
            {
                _core.ShowRewarded(
                    onRewardEarned: () => successCallback?.Invoke(),
                    onClosed: () => closedCallback?.Invoke(true),
                    onFailed: err =>
                    {
                        Debug.LogWarning($"[JisAds] Core rewarded failed: {err}");
                        failedCallback?.Invoke();
                    });
                return;
            }
            Debug.LogWarning("[JisAds] Legacy rewarded is removed. Enable Core or Tiered inventory.");
            failedCallback?.Invoke();
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
