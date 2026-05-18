using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Ads.Tracking;
using JisSDKAds.Ads.UnitAdManagers;
using JisSDKAds.Common;
using JisSDKAds.Firebase;
using Sirenix.OdinInspector;
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

        [field: SerializeField, PropertyOrder(-1)]
        public JisSDKAdsSettings SdkSettings { get; set; }

        [field: SerializeField, PropertyOrder(-1)]
        public SDKSetup AndroidSdkSetup { get; set; }
        
        [field: SerializeField, PropertyOrder(-1)]
        public SDKSetup IOSSdkSetup { get; set; }

        [field: SerializeField, PropertyOrder(-1)]
        public AdsStateMachine AdsStateMachine { get; set; }

        [field: SerializeField, ReadOnly, PropertyOrder(-1)]
        private bool IsUpdateRemoteConfigSuccess { get; set; }

        [field: SerializeField, PropertyOrder(-1)]
        public bool IsRemoveAds { get; set; }

        [field: SerializeField, PropertyOrder(-1)]
        public bool IsFirstOpen { get; set; }

        [field: SerializeField, PropertyOrder(-1)]
        public bool IsReady { get; set; }

        [field: SerializeField, PropertyOrder(-1)]
        public AdsInitializationMode InitializationMode { get; set; } = AdsInitializationMode.AutoOnStart;

        [field: SerializeField] 
        public AdsMediationType MainAdsMediationType { get; set; } = AdsMediationType.MAX;
        
        [field: SerializeField] 
        public List<AdsConfig> AdsConfigs { get; set; } = new();
        
        [field: SerializeField] 
        public List<AdsMediationController> AdsMediationControllers { get; set; } = new();

        [field: SerializeField, PropertyOrder(-1)]
        [Tooltip("Nếu bật, hệ thống sẽ init/load quảng cáo theo hàng đợi: AppOpen -> Banner -> Interstitial -> Rewarded -> MRec -> Collapsible")]
        public bool PrioritizeAppOpenAndThrottleLoads { get; set; } = true;

        [field: SerializeField, PropertyOrder(-1)]
        [MinValue(0f)]
        [Tooltip("Độ trễ giữa các bước init/load (giúp tránh spike tài nguyên)")]
        public float DelayBetweenAdInits { get; set; } = 0.75f;

        [SerializeField, Tooltip("Debounce lifecycle events (ms) để chống gọi trùng khi app vào nền/ra tiền cảnh")]
        private float lifecycleDebounceMs = 250f;

        private bool lastPaused;
        private bool lastFocused = true;
        private bool isForeground = true;
        private Coroutine lifecycleCo;

        private readonly Queue<IEnumerator> adInitQueue = new();
        private bool isProcessingAdInitQueue;

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

        [ShowInInspector, ReadOnly, PropertyOrder(-1)]
        public bool IsActiveAdImpressionTracking => 
            CurrentSDKSetup != null && CurrentSDKSetup.IsActiveAdImpressionTracking;

        [ShowInInspector, ReadOnly, PropertyOrder(-1)]
        public bool IsActiveCustomAdImpressionEvent =>
            CurrentSDKSetup != null && 
            CurrentSDKSetup.IsActiveCustomAdImpressionTracking &&
            !string.IsNullOrEmpty(AdsImpressionEventName);

        [ShowInInspector, ReadOnly, PropertyOrder(-1), ShowIf("@IsActiveCustomAdImpressionEvent == true")]
        public string AdsImpressionEventName => 
            CurrentSDKSetup != null ? CurrentSDKSetup.CustomAdImpressionEventName : "";

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (!InitializeSingleton()) return;

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
            if (InitializationMode == AdsInitializationMode.AutoOnStart)
            {
                Init();
            }
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
            DontDestroyOnLoad(gameObject);
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
            StartCoroutine(CoWaitForFirebaseInitialization());
        }

        /// <summary>
        /// Option 2: game code can call async init in loading flow.
        /// </summary>
        public async Task InitializeAllAsync(bool fetchRemoteConfig = false)
        {
            await InitializeFirebaseAsync(fetchRemoteConfig);
            InitializeAdsFlow();
        }

        public async Task InitializeFirebaseAsync(bool fetchRemoteConfig = false)
        {
            if (FirebaseManager.Instance == null)
            {
                DebugAds.LogError("[AdsManager] FirebaseManager instance is missing in scene.");
                return;
            }

            await FirebaseManager.Instance.InitAsync();
            if (fetchRemoteConfig)
            {
                await FirebaseManager.Instance.FetchRemoteConfigAsync();
            }
        }

        public void InitializeAdsFlow()
        {
            if (AdsStateMachine == null)
            {
                AdsStateMachine = new AdsStateMachine();
            }
            AdsStateMachine.ChangeState(AdsStateMachine.AdsState.Initializing);
            InitConfig();
            SetupUnitAdManager();
            InitAdsMediation();
            InitAds();
        }

        private IEnumerator CoWaitForFirebaseInitialization()
        {
            if (FirebaseManager.Instance == null)
            {
                DebugAds.LogError("[AdsManager] FirebaseManager instance is missing in scene.");
                yield break;
            }

            if (FirebaseManager.Instance != null && !FirebaseManager.Instance.IsFirebaseReady)
            {
                var initTask = FirebaseManager.Instance.InitAsync();
                while (!initTask.IsCompleted)
                {
                    yield return null;
                }
            }

            while (!FirebaseManager.Instance.IsFirebaseReady)
            {
                yield return new WaitForEndOfFrame();
            }

            InitializeAdsFlow();
        }

        private void InitConfig()
        {
            if (SdkSettings != null)
                MainAdsMediationType = SdkSettings.GetActiveMediation();

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
                var controller = GetAdsMediationController(adsMediationType);
                adsConfig.Init(controller, OnAdRevenuePaidEvent);
            }
        }

        private void InitAdsMediation()
        {
            DebugAds.Log("Init Ads Mediation (throttled)");
            StartCoroutine(CoInitAdsMediationThrottled(BuildMediationInitOrder()));
        }

        private IEnumerator CoInitAdsMediationThrottled(List<AdsType> order)
        {
            foreach (var adsType in order)
            {
                InitializeMediationIfNeeded(adsType);

                if (DelayBetweenAdInits > 0f)
                    yield return new WaitForSecondsRealtime(DelayBetweenAdInits);
                else
                    yield return null;
            }
        }

        private void InitializeMediationIfNeeded(AdsType adsType)
        {
            var mediationController = GetSelectedMediation(adsType);
            mediationController?.Init();
        }

        private List<AdsType> BuildMediationInitOrder()
        {
            var order = new List<AdsType>();
            
            if (PrioritizeAppOpenAndThrottleLoads && IsAdTypeEnabled(AdsType.APP_OPEN))
                order.Add(AdsType.APP_OPEN);

            var rest = new[] 
            { 
                AdsType.BANNER, 
                AdsType.INTERSTITIAL, 
                AdsType.REWARDED, 
                AdsType.MREC, 
                AdsType.COLLAPSIBLE_BANNER 
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
            SetupCollapsibleBannerAds();
            SetupMRecAds();
            SetupAppOpenAds();
        }

        public void InitAds()
        {
            if (!PrioritizeAppOpenAndThrottleLoads)
            {
                DelayBetweenAdInits = 0;
            }

            IsReady = false;

            var order = BuildAdInitializationOrder();
            foreach (var type in order)
            {
                if (IsAdTypeEnabled(type))
                {
                    EnqueueAdInit(type);
                }
            }

            InitResumeAdManager();
        }

        private List<AdsType> BuildAdInitializationOrder()
        {
            var order = new List<AdsType>();
            
            if (IsAdTypeEnabled(AdsType.APP_OPEN))
                order.Add(AdsType.APP_OPEN);

            order.Add(AdsType.BANNER);
            order.Add(AdsType.INTERSTITIAL);
            order.Add(AdsType.REWARDED);
            order.Add(AdsType.MREC);
            order.Add(AdsType.COLLAPSIBLE_BANNER);

            return order;
        }

        private void EnqueueAdInit(AdsType type)
        {
            adInitQueue.Enqueue(CoInitingAdType(type));
            if (!isProcessingAdInitQueue)
            {
                StartCoroutine(CoProcessAdInitQueue());
            }
        }

        private IEnumerator CoProcessAdInitQueue()
        {
            isProcessingAdInitQueue = true;
            
            while (adInitQueue.Count > 0)
            {
                var routine = adInitQueue.Dequeue();
                yield return StartCoroutine(routine);

                if (DelayBetweenAdInits > 0f)
                    yield return new WaitForSecondsRealtime(DelayBetweenAdInits);
                else
                    yield return null;
            }

            isProcessingAdInitQueue = false;
            IsReady = true;
        }

        private IEnumerator CoInitingAdType(AdsType adsType)
        {
            var adManager = GetUnitAdManager(adsType);
            var mediationType = adManager.AdsMediationType;
            var mediationController = GetAdsMediationController(mediationType);

            yield return StartCoroutine(CoWaitForMediationReady(mediationController, adsType));

            if (mediationController == null || mediationController.Status != AdsMediationController.MediationStatus.Inited)
            {
                DebugAds.LogWarning($"Mediation controller for {adsType} not ready after {initialization_timeout}s. Skipping init this round.");
                yield break;
            }

            adManager.Init();
            DebugAds.Log($"Initialized {adsType} Ads with Mediation: {mediationType}");
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
            BannerAdManager.UpdateRemoteConfig();
            InterstitialAdManager.UpdateRemoteConfig();
            CollapsibleBannerAdManager.UpdateRemoteConfig();
            MRecAdManager.UpdateRemoteConfig();
            AppOpenAdManager.UpdateRemoteConfig();
            ResumeAdManager.UpdateRemoteConfig();
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
            
#if UNITY_SOLAR_ENGINE
            SolarEngineManager.Instance.TrackAdImpression(impressionData);
#endif
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
            var controller = GetAdsMediationController(AdsMediationType.ADMOB);
            if (controller is AdmobMediationController admobMediationController)
            {
                admobMediationController.ShowConsentFormAgain();
            }
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
                AdsType.COLLAPSIBLE_BANNER => CollapsibleBannerAdsConfig.GetAdsMediation(),
                AdsType.INTERSTITIAL => InterstitialAdsConfig.GetAdsMediation(),
                AdsType.REWARDED => RewardVideoAdsConfig.GetAdsMediation(),
                AdsType.MREC => MRecAdsConfig.GetAdsMediation(),
                AdsType.APP_OPEN => AppOpenAdsConfig.GetAdsMediation(),
                _ => null
            };
        }

        private AdsMediationController GetAdsMediationController(AdsMediationType adsMediationType)
        {
            if (AdsMediationControllers == null) return null;
            return AdsMediationControllers.Find(x => x != null && x.AdsMediationType == adsMediationType);
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
                AdsType.COLLAPSIBLE_BANNER => CollapsibleBannerAdManager,
                AdsType.INTERSTITIAL => InterstitialAdManager,
                AdsType.REWARDED => RewardAdManager,
                AdsType.MREC => MRecAdManager,
                AdsType.APP_OPEN => AppOpenAdManager,
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
            CollapsibleBannerAdManager.OnPause(isPaused);
            MRecAdManager.OnPause(isPaused);
            AppOpenAdManager.OnPause(isPaused);
            ResumeAdManager.OnPause(isPaused);
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