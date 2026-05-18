using System.Threading.Tasks;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Core;
using JisSDKAds.Core.Interfaces;
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
        private bool _isReady;

        public bool IsReady => _isReady;
        public bool UseCoreForStandardFormats => useCoreForStandardFormats && _core != null && _core.IsInitialized;

        public bool UseCoreForAppOpen =>
            useCoreForStandardFormats &&
            _core != null &&
            _core.IsInitialized &&
            _core.PrimaryProvider?.AppOpen is not NullAppOpenAd;
        public JisSDKAdsSettings Settings => settings;
        public AdsManager Legacy => _legacy;
        public AdManager Core => _core;

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
            DontDestroyOnLoad(gameObject);

            if (settings == null)
            {
                Debug.LogError("[JisAds] JisSDKAdsSettings is not assigned.");
                return;
            }

            ResolveLegacy();
            if (_legacy != null)
                _legacy.InitializationMode = AdsManager.AdsInitializationMode.Manual;
            settings.ApplyToAdsManager(_legacy);
        }

        private void Start()
        {
            if (autoInitializeOnStart)
                _ = InitializeAsync();
        }

        public async Task InitializeAsync(bool fetchRemoteConfig = false)
        {
            await InitializeFirebaseAsync(fetchRemoteConfig);
            InitializeLegacyFlow();
            if (useCoreForStandardFormats)
                InitializeCoreFlow();

            await WaitUntilLegacyReadyAsync();
            _isReady = _legacy != null && _legacy.IsReady;
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

            Debug.LogWarning("[JisAds] Legacy AdsManager did not become ready in time.");
        }

        public async Task InitializeFirebaseAsync(bool fetchRemoteConfig = false)
        {
            if (FirebaseManager.Instance == null)
            {
                Debug.LogError("[JisAds] FirebaseManager not found in scene.");
                return;
            }

            await FirebaseManager.Instance.InitAsync();
            if (fetchRemoteConfig)
                await FirebaseManager.Instance.FetchRemoteConfigAsync();
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
                Debug.LogWarning($"[JisAds] No Core provider for {profile.mediation}. Standard formats will use legacy AdsManager.");
                useCoreForStandardFormats = false;
                return;
            }

            _core.RegisterProvider(providerConfig.ProviderId, providerConfig.CreateProvider());
            _core.Initialize(
                onSuccess: () => Debug.Log("[JisAds] Core AdManager ready."),
                onFailure: err =>
                {
                    Debug.LogWarning($"[JisAds] Core init failed, falling back to legacy: {err}");
                    useCoreForStandardFormats = false;
                });
        }

        #region Standard formats (Core or legacy)

        public void ShowInterstitial(
            UnityAction closedCallback = null,
            UnityAction showSuccessCallback = null,
            UnityAction showFailCallback = null,
            bool isTracking = true,
            bool isSkipCapping = false)
        {
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

            _legacy?.ShowInterstitial(closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
        }

        public void ShowRewardVideo(
            string rewardedPlacement,
            UnityAction successCallback,
            UnityAction<bool> closedCallback = null,
            UnityAction failedCallback = null)
        {
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

        public bool IsInterstitialAdLoaded() =>
            UseCoreForStandardFormats
                ? _core.PrimaryProvider?.Interstitial.IsLoaded ?? false
                : _legacy != null && _legacy.IsInterstitialAdLoaded();

        public bool CanShowInterstitialAd() =>
            UseCoreForStandardFormats
                ? _core.PrimaryProvider?.Interstitial.IsLoaded ?? false
                : _legacy != null && _legacy.CanShowInterstitialAd();

        public bool IsRewardedVideoLoaded() =>
            UseCoreForStandardFormats
                ? _core.PrimaryProvider?.Rewarded.IsLoaded ?? false
                : _legacy != null && _legacy.IsRewardedVideoLoaded();

        public bool CanShowRewardedVideo() =>
            UseCoreForStandardFormats
                ? _core.PrimaryProvider?.Rewarded.IsLoaded ?? false
                : _legacy != null && _legacy.CanShowRewardedVideo();

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
