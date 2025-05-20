using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using ABIMaxSDKAds.Scripts;
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
          [field: SerializeField, PropertyOrder(-1)] public SDKSetup SDKSetup { get; set; }
          [field: SerializeField, PropertyOrder(-1)] public AdsStateMachine AdsStateMachine { get; set; }
          [field: SerializeField, ReadOnly, PropertyOrder(-1)] private bool IsUpdateRemoteConfigSuccess { get; set; } = false;
          [field: SerializeField, PropertyOrder(-1)] public bool IsRemoveAds { get; set; }
          [field: SerializeField, PropertyOrder(-1)] public bool IsFirstOpen { get; set; }
          [field: SerializeField, PropertyOrder(-1)] public bool IsReady { get; set; }
          
          [ShowInInspector, ReadOnly, PropertyOrder(-1)]
          public bool IsActiveAdImpressionTracking => SDKSetup != null && SDKSetup.IsActiveAdImpressionTracking;
          
          [ShowInInspector, ReadOnly, PropertyOrder(-1)]
          public bool IsActiveCustomAdImpressionEvent =>
               SDKSetup != null && SDKSetup.IsActiveCustomAdImpressionTracking && !string.IsNullOrEmpty(AdsImpressionEventName);

          [ShowInInspector, ReadOnly, PropertyOrder(-1), ShowIf("@IsActiveCustomAdImpressionEvent == true")]
          public string AdsImpressionEventName => SDKSetup != null ? SDKSetup.CustomAdImpressionEventName : "";
          [field: SerializeField] public AdsMediationType MainAdsMediationType { get; set; } = AdsMediationType.MAX;
          [field: SerializeField] public List<AdsConfig> AdsConfigs { get; set; } = new List<AdsConfig>();

          [field: SerializeField]
          public List<AdsMediationController> AdsMediationControllers { get; set; } =
               new List<AdsMediationController>();

          private UnityEvent OnRemoveAdsEvent = new UnityEvent();

          private const string key_local_remove_ads = "key_local_remove_ads";

          #endregion

          #region System

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
               InterstitialAdManager.UpdateRemoteConfig();
               RewardAdManager.UpdateRemoteConfig();
               BannerAdManager.UpdateRemoteConfig();
               CollapsibleBannerAdManager.UpdateRemoteConfig();
               MRecAdManager.UpdateRemoteConfig();
               AppOpenAdManager.UpdateRemoteConfig();
               ResumeAdManager.UpdateRemoteConfig();
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
               
          }

          private void InitConfig()
          {
               foreach (AdsConfig adsConfig in AdsConfigs)
               {
                    AdsMediationType adsMediationType = SDKSetup.GetAdsMediationType(adsConfig.adsType);
                    adsConfig.Init(GetAdsMediationController(adsMediationType), OnAdRevenuePaidEvent);
               }
          }

          private void InitAdsMediation()
          {
               DebugAds.Log("Init Ads Mediation");
               {
                    AdsMediationController adsMediationController = GetSelectedMediation(AdsType.INTERSTITIAL);
                    if (adsMediationController != null && !adsMediationController.IsInited)
                    {
                         GetSelectedMediation(AdsType.INTERSTITIAL).Init();
                    }
               }

               {
                    AdsMediationController adsMediationController = GetSelectedMediation(AdsType.REWARDED);
                    if (adsMediationController != null && !adsMediationController.IsInited)
                    {
                         GetSelectedMediation(AdsType.REWARDED).Init();
                    }
               }

               {
                    AdsMediationController adsMediationController = GetSelectedMediation(AdsType.BANNER);
                    if (adsMediationController != null && !adsMediationController.IsInited)
                    {
                         GetSelectedMediation(AdsType.BANNER).Init();
                    }
               }

               {
                    AdsMediationController adsMediationController = GetSelectedMediation(AdsType.COLLAPSIBLE_BANNER);
                    if (adsMediationController != null && !adsMediationController.IsInited)
                    {
                         GetSelectedMediation(AdsType.COLLAPSIBLE_BANNER).Init();
                    }
               }

               {
                    AdsMediationController adsMediationController = GetSelectedMediation(AdsType.MREC);
                    if (adsMediationController != null && !adsMediationController.IsInited)
                    {
                         GetSelectedMediation(AdsType.MREC).Init();
                    }
               }

               {
                    AdsMediationController adsMediationController = GetSelectedMediation(AdsType.APP_OPEN);
                    if (adsMediationController != null && !adsMediationController.IsInited)
                    {
                         GetSelectedMediation(AdsType.APP_OPEN).Init();
                    }
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

          public void InitAds(AdsMediationType adsMediationType)
          {
               InitInterstitial(adsMediationType);
               InitRewardedVideo(adsMediationType);
               InitBannerAds(adsMediationType);
               InitCollapsibleBanner(adsMediationType);
               InitMRecAds(adsMediationType);
               InitAppOpenAds(adsMediationType);
               InitResumeAdManager();
               IsReady = true;
          }

          private void LoadRemoveAds()
          {
               SetRemoveAds(PlayerPrefs.GetInt(key_local_remove_ads, 0) == 1);
          }

          public void SetRemoveAds(bool isRemove)
          {
               IsRemoveAds = isRemove;
               PlayerPrefs.SetInt(key_local_remove_ads, isRemove ? 1 : 0);
               if (IsRemoveAds)
               {
                    OnRemoveAdsEvent.Invoke();
               }
          }

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

          public bool IsShowingAds()
          {
               return AdsStateMachine.GetCurrentState() == AdsStateMachine.AdsState.ShowingAds;
          }
          #endregion

          

          private async Task ShowLoadingPanel()
          {
               DebugAds.Log("Show Loading Panel");
               await Task.Delay(1000);
          }

          private void CloseLoadingPanel()
          {
               DebugAds.Log("Close Loading Panel");
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
          }

          private void OnApplicationPause(bool paused)
          {
               DebugAds.Log("Application Pause");
               InterstitialAdManager.OnPause(paused);
               RewardAdManager.OnPause(paused);
               BannerAdManager.OnPause(paused);
               CollapsibleBannerAdManager.OnPause(paused);
               MRecAdManager.OnPause(paused);
               AppOpenAdManager.OnPause(paused);
               ResumeAdManager.OnPause(paused);
          }
     }
}