using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using JisSDKAds.Ads.Integration;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Ads.Tracking;
using JisSDKAds.Ads.UnitAdManagers;
using JisSDKAds.Common;
using JisSDKAds.Firebase;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JisSDKAds.Ads
{
    [ScriptOrder(-99)]
    public partial class AdsManager : MonoBehaviour
    {
        public enum AdsInitializationMode
        {
            AutoOnStart = 0,
            Manual = 1
        }

        #region Fields

        public bool isCheatAds;
        /// <summary>Legacy singleton. Prefer <see cref="JisAds.Instance"/> in game code.</summary>
        public static AdsManager Instance { get; private set; }

        [field: SerializeField]
        public JisSDKAdsSettings SdkSettings { get; set; }

        [field: SerializeField]
        public SDKSetup AndroidSdkSetup { get; set; }
        
        [field: SerializeField]
        public SDKSetup IOSSdkSetup { get; set; }

        [field: SerializeField]
        public AdsStateMachine AdsStateMachine { get; set; }

        [field: SerializeField]
        private bool IsUpdateRemoteConfigSuccess { get; set; }

        [field: SerializeField]
        [Tooltip("When true, fetch Firebase Remote Config before mediation init and any ad requests.")]
        private bool fetchRemoteConfigBeforeAds = true;

        bool _adsBootstrapInProgress;
        bool _adsBootstrapCompleted;
        bool _remoteConfigPrefetchedForAdsFlow;

        [field: SerializeField]
        public bool IsRemoveAds { get; set; }

        [field: SerializeField]
        public bool IsFirstOpen { get; set; }

        [field: SerializeField]
        public bool IsReady { get; set; }

        [field: SerializeField]
        public AdsInitializationMode InitializationMode { get; set; } = AdsInitializationMode.AutoOnStart;

        [field: SerializeField] 
        public AdsMediationType MainAdsMediationType { get; set; } = AdsMediationType.MAX;
        
        [field: SerializeField] 
        public List<AdsConfig> AdsConfigs { get; set; } = new();
        
        [field: SerializeField] 
        public List<AdsMediationController> AdsMediationControllers { get; set; } = new();

        [field: SerializeField]
        [Tooltip("Nếu bật, hệ thống sẽ init/load quảng cáo theo hàng đợi: AppOpen -> Banner -> Interstitial -> Rewarded")]
        public bool PrioritizeAppOpenAndThrottleLoads { get; set; } = true;

        [field: SerializeField]
        [Tooltip("Độ trễ giữa các bước init/load (giúp tránh spike tài nguyên)")]
        public float DelayBetweenAdInits { get; set; } = 0.75f;

        [SerializeField, Tooltip("Debounce lifecycle events (ms) để chống gọi trùng khi app vào nền/ra tiền cảnh")]
        private float lifecycleDebounceMs = 250f;

        private bool lastPaused;
        private bool lastFocused = true;
        private bool isForeground = true;
        private Coroutine lifecycleCo;

        private readonly UnityEvent onRemoveAdsEvent = new();

        private const string key_local_remove_ads = "key_local_remove_ads";
        private const string key_first_open = "first_open";
        private const float initialization_timeout = 8f;
        private const float showing_ads_done_cooldown = 2f;

        #endregion

        #region Properties

        private SDKSetup CurrentSDKSetup
        {
            get => GetSetupForCurrentPlatform();
            set => SetSetupForCurrentPlatform(value);
        }

        private SDKSetup GetSetupForCurrentPlatform()
        {
            if (SdkSettings != null)
            {
                var fromSettings = SdkSettings.GetActiveSdkSetup();
                if (fromSettings != null)
                    return fromSettings;
            }

#if UNITY_EDITOR
            return EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ? AndroidSdkSetup : IOSSdkSetup;
#else
            return Application.platform == RuntimePlatform.Android ? AndroidSdkSetup : IOSSdkSetup;
#endif
        }

        private void SetSetupForCurrentPlatform(SDKSetup value)
        {
#if UNITY_EDITOR
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                AndroidSdkSetup = value;
            else
                IOSSdkSetup = value;
#else
            if (Application.platform == RuntimePlatform.Android)
                AndroidSdkSetup = value;
            else
                IOSSdkSetup = value;
#endif
        }

        public bool IsActiveAdImpressionTracking => 
            CurrentSDKSetup != null && CurrentSDKSetup.IsActiveAdImpressionTracking;

        public bool IsActiveCustomAdImpressionEvent =>
            CurrentSDKSetup != null && 
            CurrentSDKSetup.IsActiveCustomAdImpressionTracking &&
            !string.IsNullOrEmpty(AdsImpressionEventName);

        public string AdsImpressionEventName => 
            CurrentSDKSetup != null ? CurrentSDKSetup.CustomAdImpressionEventName : "";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (!InitializeSingleton()) return;

            SdkSettings?.ApplyRuntimeDebugSettings();

            EventManager.StartListening("UpdateRemoteConfigs", UpdateRemoteConfigs);

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                ValidateSetupMatchesEditorTargetOrLogError();
            }
#endif

            LoadRemoveAds();
            InitializeFirstOpenFlag();
        }

        private void Start()
        {
            if (InitializationMode != AdsInitializationMode.AutoOnStart)
                return;

            // JisAds owns Firebase RC + ads bootstrap when present (avoid double init).
            if (JisAds.Instance != null)
                return;

            Init();
        }

        private void OnApplicationPause(bool paused)
        {
            DebugAds.Log($"[Lifecycle] OnApplicationPause: {paused}");
            lastPaused = paused;
            ScheduleLifecycleEvaluation();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            DebugAds.Log($"[Lifecycle] OnApplicationFocus: {hasFocus}");
            lastFocused = hasFocus;
            ScheduleLifecycleEvaluation();
        }

        #endregion

        #region Initialization

        private bool InitializeSingleton()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return false;
            }

            Instance = this;
            JisSDKPersistence.DontDestroyUnlessUnderPersistentRoot(gameObject);
            return true;
        }

        private void InitializeFirstOpenFlag()
        {
            IsFirstOpen = PlayerPrefs.GetInt(key_first_open, 0) == 0;
            DebugAds.Log("Is First Open " + IsFirstOpen);
            PlayerPrefs.SetInt(key_first_open, 1);
        }

        private void Init()
        {
            AdsStateMachine = new AdsStateMachine();
            AdsStateMachine.ChangeState(AdsStateMachine.AdsState.Initializing);
            StartCoroutine(CoBootstrapAdsWithRemoteConfig());
        }

        /// <summary>
        /// Full init: Firebase (+ Remote Config when enabled) then ads mediation and unit managers.
        /// </summary>
        public async Task InitializeAllAsync(bool fetchRemoteConfig = true)
        {
            await InitializeFirebaseAsync(fetchRemoteConfig);
            InitializeAdsFlow();
            await WaitUntilReadyAsync();
        }

        /// <summary>Waits until <see cref="IsReady"/> or timeout (mediation + unit init can take 30–90s).</summary>
        public async Task WaitUntilReadyAsync(float timeoutSeconds = 120f)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!IsReady && Time.realtimeSinceStartup < deadline)
                await Task.Yield();
        }

        public void LogInitDiagnosticsIfNotReady()
        {
            if (IsReady) return;

            Debug.LogWarning(
                $"[AdsManager] Not ready — bootstrapInProgress={_adsBootstrapInProgress}, " +
                $"bootstrapCompleted={_adsBootstrapCompleted}, state={AdsStateMachine?.GetCurrentState()}, " +
                $"mainMediation={MainAdsMediationType}, activeSetup={CurrentSDKSetup?.name ?? "null"}");

            if (AdsMediationControllers == null || AdsMediationControllers.Count == 0)
            {
                Debug.LogWarning("[AdsManager] No AdsMediationControllers assigned — check scene / Apply to Scene.");
                return;
            }

            foreach (var controller in AdsMediationControllers)
            {
                if (controller == null) continue;
                Debug.LogWarning(
                    $"[AdsManager] Mediation {controller.GetAdsMediationType()}: " +
                    $"status={controller.Status}, active={controller.IsActive}");
            }
        }

        public async Task InitializeFirebaseAsync(bool fetchRemoteConfig = true)
        {
            await AdsRemoteConfigBootstrap.EnsureFetchedAsync(fetchRemoteConfig && fetchRemoteConfigBeforeAds);
            ApplyRemoteConfigBeforeAdsInit();
            _remoteConfigPrefetchedForAdsFlow = true;
        }

        /// <summary>
        /// Starts ads setup. When RC gate is on, use after <see cref="InitializeFirebaseAsync"/> or auto via <see cref="Init"/>.
        /// </summary>
        public void InitializeAdsFlow()
        {
            if (_adsBootstrapInProgress)
                return;

            if (_remoteConfigPrefetchedForAdsFlow)
            {
                _remoteConfigPrefetchedForAdsFlow = false;
                StartAdsBootstrapCoroutine();
                return;
            }

            if (fetchRemoteConfigBeforeAds && FirebaseManager.Instance != null && !_adsBootstrapCompleted)
            {
                StartCoroutine(CoBootstrapAdsWithRemoteConfig());
                return;
            }

            StartAdsBootstrapCoroutine();
        }

        void StartAdsBootstrapCoroutine()
        {
            if (_adsBootstrapInProgress)
                return;

            _adsBootstrapInProgress = true;
            StartCoroutine(CoRunAdsFlowAndReleaseBootstrap());
        }

        IEnumerator CoRunAdsFlowAndReleaseBootstrap()
        {
            yield return CoRunAdsFlow();
            _adsBootstrapInProgress = false;
        }

        IEnumerator CoBootstrapAdsWithRemoteConfig()
        {
            if (_adsBootstrapInProgress)
                yield break;

            _adsBootstrapInProgress = true;

            if (AdsStateMachine == null)
            {
                AdsStateMachine = new AdsStateMachine();
                AdsStateMachine.ChangeState(AdsStateMachine.AdsState.Initializing);
            }

            yield return AdsRemoteConfigBootstrap.EnsureFetchedCoroutine(fetchRemoteConfigBeforeAds);
            ApplyRemoteConfigBeforeAdsInit();
            yield return CoRunAdsFlow();

            _adsBootstrapInProgress = false;
            _adsBootstrapCompleted = true;
        }

        IEnumerator CoRunAdsFlow()
        {
            if (AdsStateMachine == null)
            {
                AdsStateMachine = new AdsStateMachine();
            }

            AdsStateMachine.ChangeState(AdsStateMachine.AdsState.Initializing);
            InitConfig();
            SetupUnitAdManager();

            foreach (var adsType in BuildMediationInitOrder())
            {
                if (!IsAdTypeEnabled(adsType)) continue;
                InitializeMediationIfNeeded(adsType);
                var mediation = GetSelectedMediation(adsType);
                yield return CoWaitForMediationReady(mediation, adsType);
            }

            if (!PrioritizeAppOpenAndThrottleLoads)
                DelayBetweenAdInits = 0;

            IsReady = false;
            foreach (var adsType in BuildAdInitializationOrder())
            {
                if (!IsAdTypeEnabled(adsType)) continue;
                yield return CoInitingAdType(adsType);
                if (DelayBetweenAdInits > 0f)
                    yield return new WaitForSecondsRealtime(DelayBetweenAdInits);
            }

            IsReady = true;
            IsUpdateRemoteConfigSuccess = AdsRemoteConfigBootstrap.IsRemoteConfigReadyForAds;
            _adsBootstrapCompleted = true;
        }

        private void InitConfig()
        {
            if (SdkSettings != null)
                MainAdsMediationType = SdkSettings.GetActiveMediation();

            EnsureMediationControllersWired();
            ConfigureMainAdsMediation();
            InitializeAdsConfigs();
        }

        private void ConfigureMainAdsMediation()
        {
            var mainAdsMediation = GetAdsMediationController(MainAdsMediationType);
            if (mainAdsMediation != null)
            {
#if !UNITY_EDITOR
                mainAdsMediation.IsActiveConsent = true;
#endif
            }
        }

        private void InitializeAdsConfigs()
        {
            if (CurrentSDKSetup == null || AdsConfigs == null) return;
            foreach (var adsConfig in AdsConfigs)
            {
                var adsMediationType = CurrentSDKSetup.GetAdsMediationType(adsConfig.adsType);
                var controller = ResolveMediationController(adsMediationType);
                adsConfig.Init(controller, OnAdRevenuePaidEvent);
            }
        }

        void EnsureMediationControllersWired()
        {
            var fromChildren = GetComponentsInChildren<AdsMediationController>(true);
            if (fromChildren == null || fromChildren.Length == 0)
                return;

            AdsMediationControllers ??= new List<AdsMediationController>();
            foreach (var controller in fromChildren)
            {
                if (controller != null && !AdsMediationControllers.Contains(controller))
                    AdsMediationControllers.Add(controller);
            }
        }

        AdsMediationController ResolveMediationController(AdsMediationType adsMediationType)
        {
            if (adsMediationType == AdsMediationType.NONE)
                return null;

            var controller = GetAdsMediationController(adsMediationType);
            if (controller != null)
                return controller;

            EnsureMediationControllersWired();
            controller = GetAdsMediationController(adsMediationType);
            if (controller != null)
                return controller;

            foreach (var candidate in GetComponentsInChildren<AdsMediationController>(true))
            {
                if (candidate != null && candidate.GetAdsMediationType() == adsMediationType)
                {
                    if (!AdsMediationControllers.Contains(candidate))
                        AdsMediationControllers.Add(candidate);
                    return candidate;
                }
            }

            DebugAds.LogSdkInit("AdsManager", $"No mediation controller for {adsMediationType}", false,
                "Add AdmobMediation/MaxMediation under Ads_Runtime/Mediation.");
            return null;
        }

        void RefreshUnitManagerMediationBindings()
        {
            RefreshUnitManagerMediation(BannerAdManager, AdsType.BANNER);
            RefreshUnitManagerMediation(InterstitialAdManager, AdsType.INTERSTITIAL);
            RefreshUnitManagerMediation(RewardAdManager, AdsType.REWARDED);
        }

        void RefreshUnitManagerMediation(UnitAdManager unit, AdsType adsType)
        {
            if (unit == null || CurrentSDKSetup == null)
                return;

            var cfg = GetAdsConfig(adsType);
            if (cfg == null)
                return;

            var mediationType = CurrentSDKSetup.GetAdsMediationType(adsType);
            var controller = cfg.GetAdsMediation() ?? ResolveMediationController(mediationType);

            unit.AdsConfig = cfg;
            unit.SDKSetup = CurrentSDKSetup;
            unit.AdsMediationType = mediationType;
            unit.MediationController = controller;
            unit.IsActive = cfg.isActive && controller != null;
        }

        private void InitializeMediationIfNeeded(AdsType adsType)
        {
            var mediationController = GetSelectedMediation(adsType);
            mediationController?.Init();
        }

        private List<AdsType> BuildMediationInitOrder()
        {
            var order = new List<AdsType>();
            
            var rest = new[] 
            { 
                AdsType.BANNER, 
                AdsType.INTERSTITIAL, 
                AdsType.REWARDED
            };
            
            foreach (var type in rest)
            {
                if (IsAdTypeEnabled(type) && !order.Contains(type))
                    order.Add(type);
            }

            return order;
        }

        public void SetupUnitAdManager()
        {
            DebugAds.Log("Init Ads Type");
            SetupInterstitialAds();
            SetupRewardVideo();
            SetupBannerAds();
            RefreshUnitManagerMediationBindings();
        }

        /// <summary>
        /// Binds each <see cref="UnitAdManager"/> to its <see cref="AdsMediationController"/> from current SDKSetup.
        /// Call after <see cref="UpdateAdsMediationConfig"/> or during ads init.
        /// </summary>
        public void RebindUnitManagersToMediation()
        {
            if (CurrentSDKSetup == null)
                return;

            EnsureMediationControllersWired();
            InitializeAdsConfigs();
            SetupUnitAdManager();
        }

        private List<AdsType> BuildAdInitializationOrder()
        {
            var order = new List<AdsType>();
            
            order.Add(AdsType.BANNER);
            order.Add(AdsType.INTERSTITIAL);
            order.Add(AdsType.REWARDED);
            return order;
        }

        private IEnumerator CoInitingAdType(AdsType adsType)
        {
            var adManager = GetUnitAdManager(adsType);
            var mediationType = adManager.AdsMediationType;
            var mediationController = GetAdsMediationController(mediationType);

            yield return StartCoroutine(CoWaitForMediationReady(mediationController, adsType));

            if (mediationController == null || mediationController.Status != AdsMediationController.MediationStatus.Inited)
            {
                var status = mediationController == null ? "controller=null" : mediationController.Status.ToString();
                DebugAds.LogSdkInit("AdsManager", $"{adsType} unit init skipped", false,
                    $"mediation={mediationType} status={status} after {initialization_timeout}s");
                yield break;
            }

            adManager.Init();
            adManager.ApplyRemoteConfigNow();
            DebugAds.LogSdkInit("AdsManager", $"{adsType} unit ready", true, $"mediation={mediationType}");
        }

        private IEnumerator CoWaitForMediationReady(AdsMediationController mediationController, AdsType adsType)
        {
            var waited = 0f;
            
            while ((mediationController == null || mediationController.Status != AdsMediationController.MediationStatus.Inited) 
                   && waited < initialization_timeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        #endregion

        #region Remote Config

        private void UpdateRemoteConfigs()
        {
            IsUpdateRemoteConfigSuccess = true;
            StartCoroutine(CoUpdateRemoteConfigs());
        }

        private IEnumerator CoUpdateRemoteConfigs()
        {
            UpdateAllAdManagerConfigs();
            
            yield return StartCoroutine(CoWaitForInterstitialLoad());
            
            if (DelayBetweenAdInits > 0f)
            {
                yield return new WaitForSecondsRealtime(DelayBetweenAdInits);
            }

            RewardAdManager.UpdateRemoteConfig();
        }

        private void UpdateAllAdManagerConfigs()
        {
            ApplyRemoteConfigBeforeAdsInit();
            ApplyAllUnitAdManagerRemoteConfigs();
            BannerAdManager.UpdateRemoteConfig();
            InterstitialAdManager.UpdateRemoteConfig();
            JisAds.Instance?.RefreshAppOpenAndResumeRemoteConfig();
        }

        private IEnumerator CoWaitForInterstitialLoad()
        {
            var elapsedTime = 0f;
            
            while (elapsedTime < initialization_timeout && !InterstitialAdManager.IsLoaded())
            {
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        public void RefreshRemoteConfigDrivenSettings()
        {
            ApplyRemoteConfigBeforeAdsInit();
            ApplyAllUnitAdManagerRemoteConfigs();
            NotifyRemoteConfigUpdated();
        }

        /// <summary>Inventory mode + tier unit IDs from Firebase (call before mediation/ad loads).</summary>
        public void ApplyRemoteConfigBeforeAdsInit()
        {
            var setup = CurrentSDKSetup;
            if (setup == null) return;

            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
            {
                DebugAds.LogWarning(
                    "[AdsManager] Remote Config not ready — using local inventory mode and unit IDs.");
                return;
            }

            AdMobMediationReflection.ApplyRemoteAdInventorySettings(this, setup);
        }

        void ApplyAllUnitAdManagerRemoteConfigs()
        {
            BannerAdManager?.ApplyRemoteConfigNow();
            InterstitialAdManager?.ApplyRemoteConfigNow();
            RewardAdManager?.ApplyRemoteConfigNow();
            JisAds.Instance?.RefreshAppOpenAndResumeRemoteConfig();
        }

        static void NotifyRemoteConfigUpdated() => EventManager.Trigger("UpdateRemoteConfigs");

        #endregion

        #region Commands

        public void SetRemoveAds(bool isRemove)
        {
            IsRemoveAds = isRemove;
            PlayerPrefs.SetInt(key_local_remove_ads, isRemove ? 1 : 0);
            
            if (IsRemoveAds)
            {
                onRemoveAdsEvent.Invoke();
            }
        }

        private void LoadRemoveAds()
        {
            SetRemoveAds(PlayerPrefs.GetInt(key_local_remove_ads, 0) == 1);
        }

        public void SetAdsShowingState(bool isShowing) => MarkShowingAds(isShowing);

        private void MarkShowingAds(bool isShowing)
        {
            if (isShowing)
            {
                AdsStateMachine.ChangeState(AdsStateMachine.AdsState.ShowingAds);
            }
            else
            {
                EventManager.InvokeNextFrame(() => { StartCoroutine(CoWaitingMarkShowingAdsDone()); });
            }
        }

        private IEnumerator CoWaitingMarkShowingAdsDone()
        {
            yield return new WaitForSeconds(showing_ads_done_cooldown);
            AdsStateMachine.ChangeState(AdsStateMachine.AdsState.Ready);
        }

        private void OnAdRevenuePaidEvent(ImpressionData impressionData)
        {
            DebugAds.Log("Paid Ad Revenue - Ads Type = " + impressionData.ad_format);
            TrackAdImpressionToAllPlatforms(impressionData);
        }

        private void TrackAdImpressionToAllPlatforms(ImpressionData impressionData)
        {
            AdsTracker.TrackAdImpression(
                impressionData,
                CurrentSDKSetup.IsActiveAdImpressionTracking,
                CurrentSDKSetup.IsActiveCustomAdImpressionTracking,
                CurrentSDKSetup.CustomAdImpressionEventName);

#if UNITY_APPSFLYER
            AppsflyerManager.TrackAppsflyerAdRevenue(impressionData);
#endif

            Integration.AdsAnalyticsBridge.PublishAdImpression(impressionData);
        }

        private async Task ShowLoadingPanel()
        {
            DebugAds.Log("Show Loading Panel");
            await Task.Delay(1000);
        }

        private void CloseLoadingPanel()
        {
            DebugAds.Log("Close Loading Panel");
        }

        public void ShowConsentForm()
        {
            DebugAds.Log("Show Consent Form");
#if UNITY_AD_ADMOB
            if (GetAdsMediationController(AdsMediationType.ADMOB) is IAdMobConsentHost consentHost)
                consentHost.ShowConsentFormAgain();
#endif
        }

        #endregion
        
        #region Helpers

        private AdsConfig GetAdsConfig(AdsType adsType)
        {
            return AdsConfigs.Find(x => x.adsType == adsType);
        }

        private AdsMediationController GetSelectedMediation(AdsType adsType)
        {
            return adsType switch
            {
                AdsType.BANNER => BannerAdsConfig.GetAdsMediation(),
                AdsType.INTERSTITIAL => InterstitialAdsConfig.GetAdsMediation(),
                AdsType.REWARDED => RewardVideoAdsConfig.GetAdsMediation(),
                AdsType.APP_OPEN => GetAdsConfig(AdsType.APP_OPEN)?.GetAdsMediation(),
                _ => null
            };
        }

        public AdsMediationController GetAdsMediationController(AdsMediationType adsMediationType)
        {
            if (AdsMediationControllers == null) return null;
            return AdsMediationControllers.Find(x =>
                x != null && x.GetAdsMediationType() == adsMediationType);
        }

        public bool IsShowingAds()
        {
            return AdsStateMachine.GetCurrentState() == AdsStateMachine.AdsState.ShowingAds;
        }

        public bool IsAdTypeEnabled(AdsType adsType)
        {
            var cfg = GetAdsConfig(adsType);
            if (cfg == null) return false;
            
            var mediation = GetSelectedMediation(adsType);
            return mediation != null;
        }

        private UnitAdManager GetUnitAdManager(AdsType adsType)
        {
            return adsType switch
            {
                AdsType.BANNER => BannerAdManager,
                AdsType.INTERSTITIAL => InterstitialAdManager,
                AdsType.REWARDED => RewardAdManager,
                _ => null
            };
        }
        
        #endregion

        #region Lifecycle Management
        
        private void ScheduleLifecycleEvaluation()
        {
            if (lifecycleCo != null)
            {
                StopCoroutine(lifecycleCo);
                lifecycleCo = null;
            }
            lifecycleCo = StartCoroutine(CoEvaluateLifecycle());
        }

        private IEnumerator CoEvaluateLifecycle()
        {
            if (lifecycleDebounceMs > 0f)
                yield return new WaitForSecondsRealtime(lifecycleDebounceMs / 1000f);
            else
                yield return null;

            var shouldBeForeground = lastFocused && !lastPaused;
            if (shouldBeForeground == isForeground)
                yield break;

            isForeground = shouldBeForeground;
            
            if (isForeground)
                OnEnterForeground();
            else
                OnEnterBackground();
        }

        private void OnEnterBackground()
        {
            DebugAds.Log("[Lifecycle] Enter Background");
            NotifyAllAdManagersPause(true);
        }

        private void OnEnterForeground()
        {
            DebugAds.Log("[Lifecycle] Enter Foreground");
            NotifyAllAdManagersPause(false);
        }

        private void NotifyAllAdManagersPause(bool isPaused)
        {
            InterstitialAdManager.OnPause(isPaused);
            RewardAdManager.OnPause(isPaused);
            BannerAdManager.OnPause(isPaused);
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Validation

        private void ValidateSetupMatchesEditorTargetOrLogError()
        {
            var expectedSetup = GetExpectedSetupForEditorBuildTarget();
            if (expectedSetup == null)
            {
                LogEditorError_MissingSDKSetup();
                return;
            }

            if (!ReferenceEquals(GetSetupByBuildTarget(EditorUserBuildSettings.activeBuildTarget), expectedSetup))
            {
                LogEditorError_SDKSetupMismatch(expectedSetup);
            }

            ValidateAdsConfigs(expectedSetup);
        }

        private void LogEditorError_MissingSDKSetup()
        {
            Debug.LogError(
                $"[AdsManager][EditorCheck] Missing SDKSetup for active build target: {EditorUserBuildSettings.activeBuildTarget}. " +
                $"Please assign AndroidSdkSetup/IOSSdkSetup correctly (or apply via Container).",
                this);
        }

        private void LogEditorError_SDKSetupMismatch(SDKSetup expectedSetup)
        {
            Debug.LogError(
                $"[AdsManager][EditorCheck] SDKSetup reference mismatch for {EditorUserBuildSettings.activeBuildTarget}. " +
                $"Expected: '{expectedSetup.name}', but AdsManager is pointing to a different asset. " +
                $"Hint: Use Container auto-apply or re-assign the correct SDKSetup.",
                this);
        }

        private void ValidateAdsConfigs(SDKSetup expectedSetup)
        {
            if (AdsConfigs == null || AdsConfigs.Count == 0)
            {
                Debug.LogError(
                    "[AdsManager][EditorCheck] AdsConfigs is empty. Cannot validate mediation mapping. " +
                    "Did you forget to setup AdsManager configs?",
                    this);
                return;
            }

            var mismatchCount = 0;
            
            foreach (var cfg in AdsConfigs)
            {
                if (cfg == null) continue;

                var expectedMediation = expectedSetup.GetAdsMediationType(cfg.adsType);
                if (cfg.adsMediationType != expectedMediation)
                {
                    mismatchCount++;
                    LogEditorError_AdsConfigMismatch(cfg, expectedMediation, expectedSetup);
                }
            }

            if (mismatchCount == 0)
            {
                DebugAds.Log($"[AdsManager][EditorCheck] OK - AdsManager matches SDKSetup '{expectedSetup.name}' for {EditorUserBuildSettings.activeBuildTarget}");
            }
        }

        private void LogEditorError_AdsConfigMismatch(AdsConfig cfg, AdsMediationType expectedMediation, SDKSetup expectedSetup)
        {
            Debug.LogError(
                $"[AdsManager][EditorCheck] AdsConfig mismatch: adsType={cfg.adsType} " +
                $"AdsManager.adsMediationType={cfg.adsMediationType} but SDKSetup expects {expectedMediation}. " +
                $"(SDKSetup: '{expectedSetup.name}', Target: {EditorUserBuildSettings.activeBuildTarget})",
                this);
        }

        private SDKSetup GetExpectedSetupForEditorBuildTarget()
        {
            return GetSetupByBuildTarget(EditorUserBuildSettings.activeBuildTarget);
        }

        private SDKSetup GetSetupByBuildTarget(BuildTarget target)
        {
            return target switch
            {
                BuildTarget.Android => AndroidSdkSetup,
                BuildTarget.iOS => IOSSdkSetup,
                _ => null
            };
        }

        #endregion
#endif
    }
}