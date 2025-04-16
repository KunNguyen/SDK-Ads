using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using ABI;
using ABIMaxSDKAds.Scripts;
using UnityEngine.Events;
using Firebase.RemoteConfig;
using SDK.AdsManagers;
using SDK.AdsManagers.Interface;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine.Serialization;

namespace SDK
{
    public enum AdsMediationType
    {
        NONE,
        MAX,
        ADMOB,
        IRONSOURCE
    }

    public enum AdsType
    {
        BANNER,
        INTERSTITIAL,
        REWARDED,
        MREC,
        APP_OPEN,
        COLLAPSIBLE_BANNER
    }

    [ScriptOrder(-99)]
    public class AdsManager : MonoBehaviour
    {
        #region Fields

        public bool IsCheatAds;
        public static AdsManager Instance { get; private set; }

        public SDKSetup m_SDKSetup;
        private int m_LevelPassToShowInterstitial = 2;
        private int m_RewardInterruptCountTime = 0;
        private int m_MaxRewardInterruptCount = 6;
        private bool m_IsActiveInterruptReward = false;
        private bool IsUpdateRemoteConfigSuccess = false;
        private bool IsInitedAdsType;
        private bool IsRemoveAds;
        private bool IsActiveMRECAds;
        public bool IsFirstOpen;
        public bool IsLinkRewardWithRemoveAds;
        [field: SerializeField] public bool IsShowingAds { get; set; }

        public AdsType resumeAdsType;

        public AdsMediationType m_MainAdsMediationType = AdsMediationType.MAX;
        public List<AdsConfig> m_AdsConfigs = new List<AdsConfig>();
        public List<AdsMediationController> m_AdsMediationControllers = new List<AdsMediationController>();

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
            m_IsActiveInterruptReward = true;
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
            ResumeAdsManager.UpdateRemoteConfig();
        }
        private void Init()
        {
            StartCoroutine(coWaitForFirebaseInitialization());

        }

        IEnumerator coWaitForFirebaseInitialization()
        {
            while (!ABIFirebaseManager.Instance.IsFirebaseReady)
            {
                yield return new WaitForEndOfFrame();
            }

            InitConfig();
            InitAdsMediation();
            SetupUnitAdManager();
        }

        private void InitConfig()
        {
            foreach (AdsConfig adsConfig in m_AdsConfigs)
            {
                AdsMediationType adsMediationType = m_SDKSetup.GetAdsMediationType(adsConfig.adsType);
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

            IsInitedAdsType = true;
        }
        public  void InitAds(AdsMediationType adsMediationType)
        {
            InitInterstitial(adsMediationType);
            InitRewardedVideo(adsMediationType);
            InitBannerAds(adsMediationType);
            InitCollapsibleBanner(adsMediationType);
            InitMRecAds(adsMediationType);
            InitAppOpenAds(adsMediationType);
        }

        private void LoadRemoveAds()
        {
            IsRemoveAds = PlayerPrefs.GetInt(key_local_remove_ads, 0) == 1;
        }

        public void SetRemoveAds(bool isRemove)
        {
            IsRemoveAds = isRemove;
            PlayerPrefs.SetInt(key_local_remove_ads, isRemove ? 1 : 0);
            DestroyBanner();
            DestroyCollapsibleBanner();
        }

        private AdsConfig GetAdsConfig(AdsType adsType)
        {
            return m_AdsConfigs.Find(x => x.adsType == adsType);
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
            return adsMediationType switch
            {
                AdsMediationType.MAX => m_AdsMediationControllers[0],
                AdsMediationType.ADMOB => m_AdsMediationControllers[1],
                AdsMediationType.IRONSOURCE => m_AdsMediationControllers[2],
                _ => null
            };
        }
        private void MarkShowingAds(bool isShowing)
        {
            if (isShowing)
            {
                IsShowingAds = true;
            }
            else
            {
                EventManager.AddEventNextFrame(() => { StartCoroutine(coWaitingMarkShowingAdsDone()); });
            }
        }

        IEnumerator coWaitingMarkShowingAdsDone()
        {
            yield return new WaitForSeconds(2f);
            IsShowingAds = false;
        }

        #endregion

        #region EditorUpdate

        public void UpdateAdsMediationConfig()
        {
            if (m_SDKSetup == null) return;
            UpdateAdsMediationConfig(m_SDKSetup);
        }

        public void UpdateAdsMediationConfig(SDKSetup sdkSetup)
        {
            m_SDKSetup = sdkSetup;
            m_MainAdsMediationType = m_SDKSetup.adsMediationType;
            foreach (AdsConfig adsConfig in m_AdsConfigs)
            {
                AdsMediationType adsMediationType = m_SDKSetup.GetAdsMediationType(adsConfig.adsType);
                adsConfig.adsMediationType = adsMediationType;
            }

            IsLinkRewardWithRemoveAds = m_SDKSetup.IsLinkToRemoveAds;
            UpdateMaxMediation();
            UpdateAdmobMediation();
            UpdateIronSourceMediation();
        }

        private void UpdateMaxMediation()
        {
#if UNITY_AD_MAX
            const AdsMediationType adsMediationType = AdsMediationType.MAX;
            MaxMediationController maxMediationController =
                GetAdsMediationController(adsMediationType) as MaxMediationController;
            if (maxMediationController == null) return;
            if (m_SDKSetup.adsMediationType == adsMediationType)
            {
                maxMediationController.m_MaxAdConfig.SDKKey = m_SDKSetup.maxAdsSetup.SDKKey;
            }

            maxMediationController.m_MaxAdConfig.InterstitialAdUnitID =
                m_SDKSetup.interstitialAdsMediationType == adsMediationType
                    ? m_SDKSetup.maxAdsSetup.InterstitialAdUnitID
                    : "";

            maxMediationController.m_MaxAdConfig.RewardedAdUnitID =
                m_SDKSetup.rewardedAdsMediationType == adsMediationType ? m_SDKSetup.maxAdsSetup.RewardedAdUnitID : "";

            maxMediationController.m_MaxAdConfig.BannerAdUnitID = m_SDKSetup.bannerAdsMediationType == adsMediationType
                ? m_SDKSetup.maxAdsSetup.BannerAdUnitID
                : "";
#if UNITY_AD_MAX
            maxMediationController.m_BannerPosition = m_SDKSetup.maxBannerAdsPosition;
#endif

            maxMediationController.m_MaxAdConfig.CollapsibleBannerAdUnitID =
                m_SDKSetup.collapsibleBannerAdsMediationType == adsMediationType
                    ? m_SDKSetup.maxAdsSetup.CollapsibleBannerAdUnitID
                    : "";

            maxMediationController.m_MaxAdConfig.MrecAdUnitID = m_SDKSetup.mrecAdsMediationType == adsMediationType
                ? m_SDKSetup.maxAdsSetup.MrecAdUnitID
                : "";

            maxMediationController.m_MaxAdConfig.AppOpenAdUnitID =
                m_SDKSetup.appOpenAdsMediationType == adsMediationType ? m_SDKSetup.maxAdsSetup.AppOpenAdUnitID : "";

#if UNITY_EDITOR
            EditorUtility.SetDirty(maxMediationController);
            DebugAds.Log("Update Max Mediation Done");
#endif
#endif
        }

        private void UpdateAdmobMediation()
        {
#if UNITY_AD_ADMOB
            const AdsMediationType adsMediationType = AdsMediationType.ADMOB;
            AdmobMediationController admobMediationController =
                GetAdsMediationController(adsMediationType) as AdmobMediationController;
            if (admobMediationController == null) return;
            if (m_SDKSetup.interstitialAdsMediationType == adsMediationType)
            {
                m_MainAdsMediationType = adsMediationType;
                admobMediationController.m_AdmobAdSetup.InterstitialAdUnitIDList =
                    m_SDKSetup.admobAdsSetup.InterstitialAdUnitIDList;
            }
            else
            {
                admobMediationController.m_AdmobAdSetup.InterstitialAdUnitIDList = new List<string>();
            }

            admobMediationController.m_AdmobAdSetup.RewardedAdUnitIDList =
                m_SDKSetup.rewardedAdsMediationType == adsMediationType
                    ? m_SDKSetup.admobAdsSetup.RewardedAdUnitIDList
                    : new List<string>();

            {
                admobMediationController.m_AdmobAdSetup.BannerAdUnitIDList =
                    m_SDKSetup.bannerAdsMediationType == adsMediationType
                        ? m_SDKSetup.admobAdsSetup.BannerAdUnitIDList
                        : new List<string>();
                admobMediationController.IsBannerShowingOnStart = m_SDKSetup.isBannerShowingOnStart;
                admobMediationController.m_BannerPosition = m_SDKSetup.admobBannerAdsPosition;
            }

            {
                admobMediationController.m_AdmobAdSetup.CollapsibleBannerAdUnitIDList =
                    m_SDKSetup.collapsibleBannerAdsMediationType == adsMediationType
                        ? m_SDKSetup.admobAdsSetup.CollapsibleBannerAdUnitIDList
                        : new List<string>();
                admobMediationController.IsCollapsibleBannerShowingOnStart =
                    m_SDKSetup.isShowingOnStartCollapsibleBanner;
                CollapsibleBannerAdManager.IsAutoRefresh = m_SDKSetup.isAutoCloseCollapsibleBanner;
                

                CollapsibleBannerAdManager.IsAutoRefresh = m_SDKSetup.isAutoRefreshCollapsibleBanner;
                CollapsibleBannerAdManager.AutoRefreshTime= m_SDKSetup.autoRefreshTime;

                admobMediationController.m_CollapsibleBannerPosition = m_SDKSetup.adsPositionCollapsibleBanner;
            }
            {
                admobMediationController.m_AdmobAdSetup.MrecAdUnitIDList =
                    m_SDKSetup.mrecAdsMediationType == adsMediationType
                        ? m_SDKSetup.admobAdsSetup.MrecAdUnitIDList
                        : new List<string>();
                admobMediationController.m_MRECPosition = m_SDKSetup.mrecAdsPosition;
            }
            admobMediationController.m_AdmobAdSetup.AppOpenAdUnitIDList =
                m_SDKSetup.appOpenAdsMediationType == adsMediationType
                    ? m_SDKSetup.admobAdsSetup.AppOpenAdUnitIDList
                    : new List<string>();
#if UNITY_EDITOR
            EditorUtility.SetDirty(admobMediationController);
            DebugAds.Log("Update Admob Mediation Done");
#endif
#endif
        }

        private void UpdateIronSourceMediation()
        {
#if UNITY_AD_IRONSOURCE
            const AdsMediationType adsMediationType = AdsMediationType.IRONSOURCE;
            IronSourceMediationController ironSourceMediationController =
 GetAdsMediationController(adsMediationType) as IronSourceMediationController;
            if (ironSourceMediationController == null) return;
            if (m_SDKSetup.adsMediationType == adsMediationType)
            {
                ironSourceMediationController.AppKey = m_SDKSetup.ironSourceAdSetup.appKey;
            }
            
            ironSourceMediationController.interstitialAdUnitID =
 m_SDKSetup.interstitialAdsMediationType == adsMediationType ? m_SDKSetup.ironSourceAdSetup.interstitialID : "";
            ironSourceMediationController.rewardedAdUnitID =
 m_SDKSetup.rewardedAdsMediationType == adsMediationType ? m_SDKSetup.ironSourceAdSetup.rewardedID : "";
            ironSourceMediationController.bannerAdUnitID =
 m_SDKSetup.bannerAdsMediationType == adsMediationType ? m_SDKSetup.ironSourceAdSetup.bannerID : "";
            isAutoRefreshBannerByCode = m_SDKSetup.isAutoRefreshBannerByCode;
#if UNITY_EDITOR
            EditorUtility.SetDirty(ironSourceMediationController);
            DebugAds.Log("Update IronSource Mediation Done");
#endif
#endif
        }

        #endregion

        #region Interstitial

        [field: SerializeField] public InterstitialAdManager InterstitialAdManager { get; set; }
        private AdsConfig InterstitialAdsConfig => GetAdsConfig(AdsType.INTERSTITIAL);

        private void SetupInterstitialAds()
        {
            InterstitialAdManager.Setup( 
                InterstitialAdsConfig,
                m_SDKSetup,
                GetSelectedMediation(AdsType.INTERSTITIAL));
            InterstitialAdManager.IsRemoveAds = () => IsRemoveAds;
            InterstitialAdManager.IsCheatAds = () => IsCheatAds;
            InterstitialAdManager.MarkShowingAds = MarkShowingAds;
            InterstitialAdManager.IsShowingAdChecking = () => IsShowingAds;
        }
        private void InitInterstitial(AdsMediationType adsMediationType)
        {
            InterstitialAdManager.Init(adsMediationType);
            DebugAds.Log("Setup Interstitial Done");
        }
        public void ShowInterstitial(
            UnityAction closedCallback = null, 
            UnityAction showSuccessCallback = null,
            UnityAction showFailCallback = null,
            bool isTracking = true, bool isSkipCapping = false)
        {
            InterstitialAdManager.CallToShowAd("", closedCallback, showSuccessCallback, showFailCallback, isTracking, isSkipCapping);
        }
        public bool IsInterstitialAdLoaded()
        {
            bool isInterstitialAdLoaded = GetSelectedMediation(AdsType.INTERSTITIAL).IsInterstitialLoaded();
            return isInterstitialAdLoaded;
        }
        private void ResetAdsInterstitialCappingTime()
        {
            InterstitialAdManager.RestartCooldown();
        }
        public bool IsReadyToShowInterstitial()
        {
            return InterstitialAdManager.IsAdReady();
        }
        public bool IsPassLevelToShowInterstitial(int level)
        {
            DebugAds.Log("currentLevel: " + level + " Level need passed " + m_LevelPassToShowInterstitial);
            return level >= m_LevelPassToShowInterstitial;
        }
        #endregion

        #region Banner Ads
        private AdsConfig BannerAdsConfig => GetAdsConfig(AdsType.BANNER);
        [field: SerializeField] public BannerAdManager BannerAdManager { get; set; }

        private void SetupBannerAds()
        {
            DebugAds.Log("Setup Banner");
            BannerAdManager.Setup(
                BannerAdsConfig,
                m_SDKSetup,
                GetSelectedMediation(AdsType.BANNER));
            BannerAdManager.IsRemoveAds = () => IsRemoveAds;
            BannerAdManager.IsCheatAds = () => IsCheatAds;
            HideBannerAds();
        }
        private void InitBannerAds(AdsMediationType adsMediationType)
        {
            DebugAds.Log("Init Banner");
            BannerAdManager.Init(adsMediationType);
        }

        public void RequestBannerAds()
        {
            BannerAdManager.RequestAd();
        }
        public void ShowBannerAds()
        {
            DebugAds.Log(("Call Show Banner Ads"));
            BannerAdManager.Show();
        }
        public void HideBannerAds()
        {
            DebugAds.Log(("Call Hide Banner Ads"));
            BannerAdManager.Hide();
        }

        public void DestroyBanner()
        {
            BannerAdManager.DestroyAd();
        }
        public bool IsBannerLoaded()
        {
            return BannerAdManager.IsLoaded();
        }
        #endregion

        #region Collapsible Banner
        private AdsConfig CollapsibleBannerAdsConfig => GetAdsConfig(AdsType.COLLAPSIBLE_BANNER);
        [field: SerializeField] public CollapsibleBannerAdManager CollapsibleBannerAdManager { get; set; }

        private UnityAction m_CollapsibleBannerCloseCallback;

        private void SetupCollapsibleBannerAds()
        {
            DebugAds.Log("Setup Collapsible Banner");
            CollapsibleBannerAdManager.Setup(
                CollapsibleBannerAdsConfig,
                m_SDKSetup,
                GetSelectedMediation(AdsType.COLLAPSIBLE_BANNER));
            CollapsibleBannerAdManager.IsRemoveAds = () => IsRemoveAds;
            CollapsibleBannerAdManager.IsCheatAds = () => IsCheatAds;
            HideCollapsibleBannerAds();
        }
        private void InitCollapsibleBanner(AdsMediationType adsMediationType)
        {
            DebugAds.Log("Init Collapsible Banner");
            CollapsibleBannerAdManager.Init(adsMediationType);
            
        }
        public bool IsCollapsibleBannerExpended()
        {
            return CollapsibleBannerAdManager.IsExpanded;
        }

        public bool IsCollapsibleBannerShowing()
        {
            return CollapsibleBannerAdManager.IsShowingAd;
        }
        public void ShowCollapsibleBannerAds(UnityAction closeCallback = null)
        {
            DebugAds.Log(("Call Show Collapsible Banner Ads"));
            CollapsibleBannerAdManager.CallToShowAd("", closeCallback);
        }

        public void HideCollapsibleBannerAds()
        {
            CollapsibleBannerAdManager.Hide();
        }

        public void DestroyCollapsibleBanner()
        {
            CollapsibleBannerAdManager.DestroyAd();
        }

        public bool IsCollapsibleBannerLoaded()
        {
            return CollapsibleBannerAdManager.IsLoaded();
        }
        #endregion

        #region Reward Ads

        private AdsConfig RewardVideoAdsConfig => GetAdsConfig(AdsType.REWARDED);
        [field: SerializeField] public RewardAdManager RewardAdManager { get; set; }

        private string m_RewardedPlacement;
        private void SetupRewardVideo()
        {
            RewardAdManager.Setup(RewardVideoAdsConfig, m_SDKSetup, GetSelectedMediation(AdsType.REWARDED));
            RewardAdManager.IsRemoveAds = () => IsRemoveAds;
            RewardAdManager.IsCheatAds = () => IsCheatAds;
            RewardAdManager.MarkShowingAds = MarkShowingAds;
            RewardAdManager.IsShowingAdChecking = () => IsShowingAds;
        }
        private void InitRewardedVideo(AdsMediationType adsMediationType)
        {
            RewardAdManager.Init(adsMediationType);
        }
        public void ShowRewardVideo(string rewardedPlacement, UnityAction successCallback,
            UnityAction<bool> closedCallback = null, UnityAction failedCallback = null)
        {
            RewardAdManager.CallToShowRewardAd(rewardedPlacement, closedCallback, successCallback, failedCallback);
        }
        public bool IsRewardVideoLoaded()
        {
            return RewardAdManager.IsLoaded();
        }
        public bool IsReadyToShowRewardVideo()
        {
            return RewardAdManager.IsAdReady();
        }
        #endregion Reward Ads

        #region MRec Ads

        private AdsConfig MRecAdsConfig => GetAdsConfig(AdsType.MREC);
        [field: SerializeField] private MRECAdManager MRecAdManager { get; set; }
        private void SetupMRecAds()
        {
            DebugAds.Log("Setup MRec");
            MRecAdManager.Setup(MRecAdsConfig, m_SDKSetup, GetSelectedMediation(AdsType.MREC));
            MRecAdManager.IsRemoveAds = () => IsRemoveAds;
            MRecAdManager.IsCheatAds = () => IsCheatAds;
        }
        private void InitMRecAds(AdsMediationType adsMediationType)
        {
            DebugAds.Log("Init MRec");
            MRecAdManager.Init(adsMediationType);
        }
        public void ShowMRecAds()
        {
            MRecAdManager.Show();
            HideBannerAds();
        }
        public void HideMRecAds()
        {
            DebugAds.Log("Call Hide MRec Ads");
            MRecAdManager.Hide();
        }
        public bool IsMRecShowing()
        {
            return MRecAdManager.IsShowingAd;
        }
        public bool IsMRecLoaded()
        {
            return MRecAdManager.IsLoaded();
        }
        public bool IsMRecReadyToShow()
        {
            return MRecAdManager.IsAdReady();
        }
        #endregion

        #region App Open Ads

        private AdsConfig AppOpenAdsConfig => GetAdsConfig(AdsType.APP_OPEN);
        [field: SerializeField] public AppOpenAdManager AppOpenAdManager { get; set; }
        private void SetupAppOpenAds()
        {
            AppOpenAdManager.Setup(
                AppOpenAdsConfig,
                m_SDKSetup,
                GetSelectedMediation(AdsType.APP_OPEN));

            AppOpenAdManager.IsRemoveAds = () => IsRemoveAds;
            AppOpenAdManager.IsCheatAds = () => IsCheatAds;
            AppOpenAdManager.MarkShowingAds = MarkShowingAds;
            AppOpenAdManager.IsShowingAdChecking = () => IsShowingAds;
            
            DebugAds.Log("Setup App Open Ads Done");
        }
        private void InitAppOpenAds(AdsMediationType adsMediationType)
        {
            DebugAds.Log("Init App Open Ads");
            AppOpenAdManager.Init(adsMediationType);
        }

        private void ShowAppOpenAds()
        {
            AppOpenAdManager.CallToShowAd();
        }
        
        private void DelayShowAppOpenAds()
        {
            StartCoroutine(coDelayShowAppOpenAds());
        }

        IEnumerator coDelayShowAppOpenAds()
        {
            yield return new WaitForSeconds(0.3f);
            ShowAppOpenAds();
        }
        private void RequestAppOpenAds()
        {
            AppOpenAdManager.RequestAd();
        }
        private bool IsAppOpenAdsLoaded()
        {
            return AppOpenAdManager.IsLoaded();
        }
        #endregion

        #region Resume Ads

        [field: SerializeField] public ResumeAdManager ResumeAdsManager { get; set; }

        private async void ShowResumeAds()
        {
            DebugAds.Log("Show Resume Ads");
            switch (resumeAdsType)
            {
                case AdsType.INTERSTITIAL:
                {
                    await ShowLoadingPanel();
                    if (IsReadyToShowInterstitial())
                    {
                        ShowInterstitial(
                            CloseLoadingPanel,
                            CloseLoadingPanel,
                            CloseLoadingPanel);
                        return;
                    }
                    else
                    {
                        CloseLoadingPanel();
                        return;
                    }

                    break;
                }
                case AdsType.APP_OPEN:
                {
                    if (AppOpenAdsConfig.isActive)
                    {
                        DelayShowAppOpenAds();
                        return;
                    }

                    break;
                }
            }

            IsShowingAds = false;
        }

        private async Task ShowLoadingPanel()
        {
            DebugAds.Log("Show Loading Panel");
        }

        private void CloseLoadingPanel()
        {
            DebugAds.Log("Close Loading Panel");
        }
        #endregion

        private void OnAdRevenuePaidEvent(ImpressionData impressionData)
        {
            DebugAds.Log("Paid Ad Revenue - Ads Type = " + impressionData.ad_type);
            ABIAnalyticsManager.TrackAdImpression(impressionData);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackAppsflyerAdRevenue(impressionData);
#endif
        }

        private void OnApplicationPause(bool paused)
        {
            ResumeAdsManager.OnPause(paused);
        }

        [System.Serializable]
        public class UUID
        {
            public string uuid;

            public static string Generate()
            {
                UUID newUuid = new UUID { uuid = System.Guid.NewGuid().ToString() };
                return newUuid.uuid;
            }
        }
    }
}
