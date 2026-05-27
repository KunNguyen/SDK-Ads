using System.Threading.Tasks;
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
    /// Unified ads entry point (Phase 4). Standard formats can use Core <see cref="AdManager"/>;
    /// App Open, MREC, Collapsible, Resume, and RC-driven rules stay on legacy <see cref="AdsManager"/>.
    /// </summary>
    [DefaultExecutionOrder(-110)]
    public class JisAds : MonoBehaviour
    {
        public static JisAds Instance { get; private set; }

        [SerializeField] private JisSDKAdsSettings settings;
        [SerializeField] private bool useCoreForStandardFormats = true;
        [SerializeField] private bool autoInitializeOnStart = true;
        [SerializeField] private AdsManager.AdsInitializationMode legacyInitMode =
            AdsManager.AdsInitializationMode.AutoOnStart;

        private AdsManager _legacy;
        private AdManager _core;
        private TieredAdsExtension _tiered;
        private bool _isReady;

        public bool IsReady => _isReady;
        public bool UseCoreForStandardFormats => useCoreForStandardFormats && _core != null && _core.IsInitialized;

        public bool UseTieredInterstitial =>
            _tiered != null && _tiered.IsTieredForInterstitial;

        public bool UseTieredRewarded =>
            _tiered != null && _tiered.IsTieredForRewarded;

        public bool UseCoreForAppOpen =>
            useCoreForStandardFormats &&
            _core != null &&
            _core.IsInitialized &&
            _core.PrimaryProvider?.AppOpen is not NullAppOpenAd;
        public JisSDKAdsSettings Settings => settings;
        public AdsManager Legacy => _legacy;
        public AdManager Core => _core;
        public TieredAdsExtension Tiered => _tiered;

        /// <summary>Called from <see cref="Settings.SdkAdsBootstrap"/> or tests.</summary>
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
            ResolveLegacy();
            if (_legacy != null)
                _legacy.InitializationMode = AdsManager.AdsInitializationMode.Manual;
            settings.ApplyToAdsManager(_legacy);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            _tiered?.OnApplicationPause(pauseStatus);
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

            await InitializeFirebaseAsync(fetchRemoteConfig);
            InitializeLegacyFlow();
            if (useCoreForStandardFormats)
                InitializeCoreFlow();
            else
                InitializeTieredOnlyFlow();

            await WaitUntilLegacyReadyAsync();
            _isReady = _legacy != null && _legacy.IsReady;
            DebugAds.LogSdkInit("JisAds", "InitializeAsync complete", _isReady,
                _isReady ? null : "AdsManager not ready — check mediation init logs above.");
        }

        async Task WaitUntilLegacyReadyAsync()
        {
            const int maxFrames = 600;
            for (var i = 0; i < maxFrames; i++)
            {
                if (_legacy != null && _legacy.IsReady)
                    return;
                await Task.Yield();
            }

            DebugAds.LogSdkInit("JisAds", "Legacy AdsManager ready wait", false, "timeout");
        }

        public async Task InitializeFirebaseAsync(bool fetchRemoteConfig = true)
        {
            _legacy ??= FindFirstObjectByType<AdsManager>();
            if (_legacy == null)
            {
                Debug.LogError("[JisAds] AdsManager not found in scene.");
                return;
            }

            await _legacy.InitializeFirebaseAsync(fetchRemoteConfig);
        }

        void ResolveLegacy()
        {
            _legacy = FindFirstObjectByType<AdsManager>();
            if (_legacy == null)
                Debug.LogError("[JisAds] AdsManager not found. Add AdsManager to the scene.");
        }

        void InitializeLegacyFlow()
        {
            if (_legacy == null) return;

            _legacy.InitializationMode = AdsManager.AdsInitializationMode.Manual;
            _legacy.InitializeAdsFlow();
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
                    Debug.LogWarning($"[JisAds] No Core provider for {profile.mediation}. Standard formats will use legacy AdsManager.");
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
            _tiered = TieredAdsBootstrap.CreateExtension(tieredConfig, backend, transform);
            if (_tiered != null)
                Debug.Log("[JisAds] Tiered inventory extension ready.");
        }

        static TieredAdsConfig ResolveTieredConfig(PlatformAdsProfile profile)
        {
            return profile?.tieredAdsConfig;
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
                Debug.LogWarning("[JisAds] Tiered inventory requires a provider config to initialize the ad SDK.");
                return;
            }

            var provider = providerConfig.CreateProvider();
            provider.Initialize(
                onSuccess: () =>
                {
                    _tiered.Manager.SetProviderReady(true);
                    Debug.Log("[JisAds] Tiered provider SDK ready (legacy path).");
                },
                onFailure: err => Debug.LogWarning($"[JisAds] Tiered provider init failed: {err}"));
        }

        #region Standard formats (Core or legacy)

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
            if (UseTieredInterstitial && !UseCoreForStandardFormats)
            {
                if (_legacy != null && (_legacy.IsRemoveAds || _legacy.isCheatAds))
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

            _legacy?.ShowInterstitial(
                interstitialPlacement,
                closedCallback,
                showSuccessCallback,
                showFailCallback,
                isTracking,
                isSkipCapping);
        }

        public void ShowRewardVideo(
            string rewardedPlacement,
            UnityAction successCallback,
            UnityAction<bool> closedCallback = null,
            UnityAction failedCallback = null)
        {
            if (UseTieredRewarded && !UseCoreForStandardFormats)
            {
                if (_legacy != null && (_legacy.IsRemoveAds || _legacy.isCheatAds))
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

            _legacy?.ShowRewardVideo(rewardedPlacement, successCallback, closedCallback, failedCallback);
        }

        public void ShowBannerAds()
        {
            if (UseCoreForStandardFormats)
            {
                _core.ShowBanner();
                return;
            }

            _legacy?.ShowBannerAds();
        }

        public void HideBannerAds()
        {
            if (UseCoreForStandardFormats)
            {
                _core.HideBanner();
                return;
            }

            _legacy?.HideBannerAds();
        }

        public bool IsInterstitialAdLoaded()
        {
            if (UseTieredInterstitial)
                return _tiered.Manager.IsAnyLoaded(AdsFormatType.Interstitial);
            return UseCoreForStandardFormats
                ? _core.PrimaryProvider?.Interstitial.IsLoaded ?? false
                : _legacy != null && _legacy.IsInterstitialAdLoaded();
        }

        public bool CanShowInterstitialAd()
        {
            if (UseTieredInterstitial)
                return IsInterstitialAdLoaded();
            return UseCoreForStandardFormats
                ? _core.PrimaryProvider?.Interstitial.IsLoaded ?? false
                : _legacy != null && _legacy.CanShowInterstitialAd();
        }

        public bool IsRewardedVideoLoaded()
        {
            if (UseTieredRewarded)
                return _tiered.Manager.IsAnyLoaded(AdsFormatType.Rewarded);
            return UseCoreForStandardFormats
                ? _core.PrimaryProvider?.Rewarded.IsLoaded ?? false
                : _legacy != null && _legacy.IsRewardedVideoLoaded();
        }

        public bool CanShowRewardedVideo()
        {
            if (UseTieredRewarded)
                return IsRewardedVideoLoaded();
            return UseCoreForStandardFormats
                ? _core.PrimaryProvider?.Rewarded.IsLoaded ?? false
                : _legacy != null && _legacy.CanShowRewardedVideo();
        }

        #endregion

        #region Legacy-only (delegate)

        public void ShowAppOpenAd()
        {
            if (UseCoreForAppOpen)
            {
                _core.ShowAppOpen(
                    onClosed: null,
                    onFailed: err => Debug.LogWarning($"[JisAds] Core app open failed, try legacy: {err}"));
                return;
            }

            _legacy?.ShowAppOpenAd();
        }

        public bool IsAppOpenAdLoaded() =>
            UseCoreForAppOpen
                ? _core.IsAppOpenLoaded()
                : _legacy != null && _legacy.IsAppOpenAdLoaded();
        public void ShowMRecAds() => _legacy?.ShowMRecAds();
        public void HideMRecAds() => _legacy?.HideMRecAds();
        public void ShowCollapsibleBannerAds(UnityAction closeCallback = null) =>
            _legacy?.ShowCollapsibleBannerAds(closeCallback);
        public void HideCollapsibleBannerAds() => _legacy?.HideCollapsibleBannerAds();
        public void InitResumeAdManager() => _legacy?.InitResumeAdManager();
        public void SetRemoveAds(bool isRemove) => _legacy?.SetRemoveAds(isRemove);
        public bool IsRemoveAds => _legacy != null && _legacy.IsRemoveAds;

        #endregion
    }
}
