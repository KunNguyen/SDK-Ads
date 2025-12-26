using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using ABIMaxSDKAds.Scripts;
using SDK.AdsManagers;
using UnityEngine.Events;
using Sirenix.OdinInspector;

namespace SDK
{
    [ScriptOrder(-99)]
    public partial class AdsManager : MonoBehaviour
    {
        #region Fields

        public bool IsCheatAds;
        public static AdsManager Instance { get; private set; }

        [field: SerializeField, PropertyOrder(-1)]
        public SDKSetup SDKSetup { get; set; }

        [field: SerializeField, PropertyOrder(-1)]
        public AdsStateMachine AdsStateMachine { get; set; }

        [field: SerializeField, ReadOnly, PropertyOrder(-1)]
        private bool IsUpdateRemoteConfigSuccess { get; set; } = false;

        [field: SerializeField, PropertyOrder(-1)]
        public bool IsRemoveAds { get; set; }

        [field: SerializeField, PropertyOrder(-1)]
        public bool IsFirstOpen { get; set; }

        [field: SerializeField, PropertyOrder(-1)]
        public bool IsReady { get; set; }

        [ShowInInspector, ReadOnly, PropertyOrder(-1)]
        public bool IsActiveAdImpressionTracking => SDKSetup != null && SDKSetup.IsActiveAdImpressionTracking;

        [ShowInInspector, ReadOnly, PropertyOrder(-1)]
        public bool IsActiveCustomAdImpressionEvent =>
            SDKSetup != null && SDKSetup.IsActiveCustomAdImpressionTracking &&
            !string.IsNullOrEmpty(AdsImpressionEventName);

        [ShowInInspector, ReadOnly, PropertyOrder(-1), ShowIf("@IsActiveCustomAdImpressionEvent == true")]
        public string AdsImpressionEventName => SDKSetup != null ? SDKSetup.CustomAdImpressionEventName : "";

        [field: SerializeField] public AdsMediationType MainAdsMediationType { get; set; } = AdsMediationType.MAX;
        [field: SerializeField] public List<AdsConfig> AdsConfigs { get; set; } = new();
        [field: SerializeField] public List<AdsMediationController> AdsMediationControllers { get; set; } = new();


        [field: SerializeField, PropertyOrder(-1)]
        [Tooltip(
            "Nếu bật, hệ thống sẽ init/load quảng cáo theo hàng đợi: AppOpen -> Banner -> Interstitial -> Rewarded -> MRec -> Collapsible")]
        public bool PrioritizeAppOpenAndThrottleLoads { get; set; } = true;

        [field: SerializeField, PropertyOrder(-1)]
        [MinValue(0f)]
        [Tooltip("Độ trễ giữa các bước init/load (giúp tránh spike tài nguyên)")]
        public float DelayBetweenAdInits { get; set; } = 0.75f;

        // Thêm vào vùng Fields
        [SerializeField, Tooltip("Debounce lifecycle events (ms) để chống gọi trùng khi app vào nền/ra tiền cảnh")]
        private float LifecycleDebounceMs = 250f;

        private bool _lastPaused = false;
        private bool _lastFocused = true;
        private bool _isForeground = true; // Trạng thái suy ra: foreground = focused && !paused
        private Coroutine _lifecycleCo = null;

        private readonly Queue<IEnumerator> AdInitQueue = new Queue<IEnumerator>();
        private bool IsProcessingAdInitQueue { get; set; } = false;

        private UnityEvent OnRemoveAdsEvent = new UnityEvent();

        private const string key_local_remove_ads = "key_local_remove_ads";
        private static readonly AdsType[] AdsTypesToInitialize =
        {
            AdsType.INTERSTITIAL,
            AdsType.REWARDED,
            AdsType.BANNER,
            AdsType.COLLAPSIBLE_BANNER,
            AdsType.MREC,
            AdsType.APP_OPEN
        };

        #endregion

        #region Init

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EventManager.StartListening("UpdateRemoteConfigs", UpdateRemoteConfigs);

            LoadRemoveAds();
            IsFirstOpen = PlayerPrefs.GetInt("first_open", 0) == 0;
            DebugAds.Log("Is First Open " + IsFirstOpen);
            PlayerPrefs.SetInt("first_open", 1);
        }

        private void Start()
        {
            Init();
        }

        private void UpdateRemoteConfigs()
        {
            IsUpdateRemoteConfigSuccess = true;
            StartCoroutine(CoUpdateRemoteConfigs());
        }

        private IEnumerator CoUpdateRemoteConfigs()
        {
            BannerAdManager.UpdateRemoteConfig();
            InterstitialAdManager.UpdateRemoteConfig();
            CollapsibleBannerAdManager.UpdateRemoteConfig();
            MRecAdManager.UpdateRemoteConfig();
            AppOpenAdManager.UpdateRemoteConfig();
            ResumeAdManager.UpdateRemoteConfig();
            
            var elapsedTime = 0f;
            var timeOut = 8f;
            while (elapsedTime <timeOut && !InterstitialAdManager.IsLoaded())
            {
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }
            
            if (DelayBetweenAdInits > 0f)
            {
                yield return new WaitForSecondsRealtime(DelayBetweenAdInits);
            }

            RewardAdManager.UpdateRemoteConfig();
        }

        private void Init()
        {
            AdsStateMachine = new AdsStateMachine();
            AdsStateMachine.ChangeState(AdsStateMachine.AdsState.Initializing);
            StartCoroutine(CoWaitForFirebaseInitialization());
        }
        private IEnumerator CoWaitForFirebaseInitialization()
        {
            while (!FirebaseManager.Instance.IsFirebaseReady)
            {
                yield return new WaitForEndOfFrame();
            }

            InitConfig();
            SetupUnitAdManager();
            InitAdsMediation();
            InitAds();
        }
        private void InitConfig()
        {
            var mainAdsMediation = GetAdsMediationController(MainAdsMediationType);
            if (mainAdsMediation != null)
            {
#if !UNITY_EDITOR
                mainAdsMediation.IsActiveConsent = true;
#endif
            }
            
            foreach (AdsConfig adsConfig in AdsConfigs)
            {
                AdsMediationType adsMediationType = SDKSetup.GetAdsMediationType(adsConfig.adsType);
                adsConfig.Init(GetAdsMediationController(adsMediationType), OnAdRevenuePaidEvent);
            }
        }
        private void InitializeMediationIfNeeded(AdsType adsType)
        {
            var mediationController = GetSelectedMediation(adsType);
            if (mediationController != null)
            {
                mediationController.Init();
            }
        }
        private List<AdsType> BuildMediationInitOrder()
        {
            var order = new List<AdsType>();
            if (PrioritizeAppOpenAndThrottleLoads && IsAdTypeEnabled(AdsType.APP_OPEN))
                order.Add(AdsType.APP_OPEN);

            var rest = new[] { AdsType.BANNER, AdsType.INTERSTITIAL, AdsType.REWARDED, AdsType.MREC, AdsType.COLLAPSIBLE_BANNER };
            foreach (var t in rest)
                if (IsAdTypeEnabled(t) && !order.Contains(t))
                    order.Add(t);

            return order;
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
                    yield return null; // yield 1 frame để tránh burst
            }
        }
        public void SetupUnitAdManager()
        {
            DebugAds.Log("Init Ads Type");
            //Setup Interstitial
            SetupInterstitialAds();

            //Setup Reward Video
            SetupRewardVideo();

            //Setup Banner
            SetupBannerAds();

            //Setup Collapsible Banner
            SetupCollapsibleBannerAds();

            //Setup RMecAds
            SetupMRecAds();

            //Setup AppOpenAds
            SetupAppOpenAds();
        }
        public void InitAds()
        {
            if (!PrioritizeAppOpenAndThrottleLoads)
            {
                DelayBetweenAdInits = 0;
            }

            IsReady = false;

            var order = new List<AdsType>();
            if (IsAdTypeEnabled(AdsType.APP_OPEN))
                order.Add(AdsType.APP_OPEN);

            order.Add(AdsType.BANNER);
            order.Add(AdsType.INTERSTITIAL);
            order.Add(AdsType.REWARDED);
            order.Add(AdsType.MREC);
            order.Add(AdsType.COLLAPSIBLE_BANNER);

            foreach (var type in order)
            {
                if (IsAdTypeEnabled(type))
                {
                    EnqueueAdInit(type);
                }
            }

            InitResumeAdManager();
        }
        private void EnqueueAdInit(AdsType type)
        {
            AdInitQueue.Enqueue(CoInitingAdType(type));
            if (!IsProcessingAdInitQueue)
            {
                StartCoroutine(CoProcessAdInitQueue());
            }
        }
        private IEnumerator CoProcessAdInitQueue()
        {
            IsProcessingAdInitQueue = true;
            while (AdInitQueue.Count > 0)
            {
                var routine = AdInitQueue.Dequeue();
                yield return StartCoroutine(routine);

                if (DelayBetweenAdInits > 0f)
                    yield return new WaitForSecondsRealtime(DelayBetweenAdInits);
                else
                    yield return null;
            }

            IsProcessingAdInitQueue = false;
            IsReady = true;
        }
        private IEnumerator CoInitingAdType(AdsType adsType)
        {
            UnitAdManager adManager = GetUnitAdManager(adsType);
            AdsMediationType mediationType = adManager.AdsMediationType;
            var mediationController = GetAdsMediationController(mediationType);

            float waited = 0f, timeout = 8f;
            while ((mediationController == null || mediationController.Status != AdsMediationController.MediationStatus.Inited) && waited < timeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (mediationController == null || mediationController.Status != AdsMediationController.MediationStatus.Inited)
            {
                DebugAds.LogWarning($"Mediation controller for {adsType} not ready after {timeout}s. Skipping init this round.");
                yield break;
            }

            adManager.Init();
            DebugAds.Log($"Initialized {adsType} Ads with Mediation: {mediationType}");
        }
        #endregion

        #region Commands
        public void SetRemoveAds(bool isRemove)
        {
            IsRemoveAds = isRemove;
            PlayerPrefs.SetInt(key_local_remove_ads, isRemove ? 1 : 0);
            if (IsRemoveAds)
            {
                OnRemoveAdsEvent.Invoke();
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
                EventManager.AddEventNextFrame(() => { StartCoroutine(CoWaitingMarkShowingAdsDone()); });
            }
        }
        private IEnumerator CoWaitingMarkShowingAdsDone()
        {
            yield return new WaitForSeconds(2f);
            AdsStateMachine.ChangeState(AdsStateMachine.AdsState.Ready);
        }   
        private void OnAdRevenuePaidEvent(ImpressionData impressionData)
        {
            DebugAds.Log("Paid Ad Revenue - Ads Type = " + impressionData.ad_format);
            AdsTracker.TrackAdImpression(impressionData,
                SDKSetup.IsActiveAdImpressionTracking,
                SDKSetup.IsActiveCustomAdImpressionTracking,
                SDKSetup.CustomAdImpressionEventName);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackAppsflyerAdRevenue(impressionData);
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
            AdsMediationController controller = GetAdsMediationController(AdsMediationType.ADMOB);
            if( controller != null && controller is AdmobMediationController admobMediationController)
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
            return AdsMediationControllers.Find(x => x.AdsMediationType == adsMediationType);
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


        

        #region Systems
        
        private void ScheduleLifecycleEvaluation()
        {
            if (_lifecycleCo != null)
            {
                StopCoroutine(_lifecycleCo);
                _lifecycleCo = null;
            }
            _lifecycleCo = StartCoroutine(CoEvaluateLifecycle());
        }

        private IEnumerator CoEvaluateLifecycle()
        {
            // Debounce theo realtime (không phụ thuộc Time.timeScale)
            if (LifecycleDebounceMs > 0f)
                yield return new WaitForSecondsRealtime(LifecycleDebounceMs / 1000f);
            else
                yield return null;

            bool shouldBeForeground = _lastFocused && !_lastPaused;
            if (shouldBeForeground == _isForeground)
                yield break; // Không đổi trạng thái -> không propagate

            _isForeground = shouldBeForeground;
            if (_isForeground)
                OnEnterForeground();
            else
                OnEnterBackground();
        }

        private void OnEnterBackground()
        {
            DebugAds.Log("[Lifecycle] Enter Background");
            InterstitialAdManager.OnPause(true);
            RewardAdManager.OnPause(true);
            BannerAdManager.OnPause(true);
            CollapsibleBannerAdManager.OnPause(true);
            MRecAdManager.OnPause(true);
            AppOpenAdManager.OnPause(true);
            ResumeAdManager.OnPause(true);
        }

        private void OnEnterForeground()
        {
            DebugAds.Log("[Lifecycle] Enter Foreground");
            InterstitialAdManager.OnPause(false);
            RewardAdManager.OnPause(false);
            BannerAdManager.OnPause(false);
            CollapsibleBannerAdManager.OnPause(false);
            MRecAdManager.OnPause(false);
            AppOpenAdManager.OnPause(false);
            ResumeAdManager.OnPause(false);
        }


        private void OnApplicationPause(bool paused)
        {
            DebugAds.Log($"[Lifecycle] OnApplicationPause: {paused}");
            _lastPaused = paused;
            ScheduleLifecycleEvaluation();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            DebugAds.Log($"[Lifecycle] OnApplicationFocus: {hasFocus}");
            _lastFocused = hasFocus;
            ScheduleLifecycleEvaluation();
        }

        #endregion
    }
}