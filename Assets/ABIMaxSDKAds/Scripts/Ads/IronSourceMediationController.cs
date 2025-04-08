using SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class IronSourceMediationController : AdsMediationController
{
#if UNITY_AD_IRONSOURCE
#if UNITY_ANDROID
   public string Android_Key;
   public string IOS_Key;

   private bool m_IsWatchSuccess = false;

   public string AppKey
   {
      get
      {
#if UNITY_ANDROID
         return Android_Key;
#elif UNITY_IPHONE
            return IOS_Key;
#else
            return Android_Key;
#endif
      }
      set
      {
#if UNITY_ANDROID
         Android_Key = value;
#elif UNITY_IPHONE
            IOS_Key = value;
#else
            Android_Key = value;
#endif
      }
   }

   public string rewardedAdUnitID;
   public string interstitialAdUnitID;
   public string bannerAdUnitID;

   private void Awake()
   {
   }

   private void Start()
   {
   }

   public override void Init()
   {
      if (IsInited) return;
      base.Init();
      Debug.Log("IronSource: MyAppStart Start called");

      string id = IronSource.Agent.getAdvertiserId();
      Debug.Log("IronSource: IronSource.Agent.getAdvertiserId : " + id);
      Debug.Log("IronSource: unity version" + IronSource.unityVersion());

      // SDK init
      Debug.Log("IronSource: IronSource.Agent.init");
      string uniqueUserID = SystemInfo.deviceUniqueIdentifier;
      IronSource.Agent.setUserId(uniqueUserID);
      IronSource.Agent.setConsent(true);
      IronSource.Agent.setMetaData("do_not_sell", "false");
      IronSource.Agent.setMetaData("is_child_directed", "false");
      IronSource.Agent.setMetaData("is_test_suite", "enable");
      IronSourceEvents.onImpressionDataReadyEvent += IronSourceEvents_onImpressionSuccessEvent;
      IronSourceEvents.onSdkInitializationCompletedEvent += () =>
      {
         Debug.Log("IronSource: onSdkInitializationCompletedEvent");
         IronSource.Agent.launchTestSuite();
      };
      LevelPlay.OnInitFailed += OnInitFailed;
      LevelPlay.OnInitSuccess += OnInitSuccess;
      LevelPlay.Init(AppKey, null,
         new LevelPlayAdFormat[]
            { LevelPlayAdFormat.REWARDED, LevelPlayAdFormat.INTERSTITIAL, LevelPlayAdFormat.BANNER });
      IronSource.Agent.validateIntegration();
   }

   private void OnInitSuccess(LevelPlayConfiguration obj)
   {
      Debug.Log("Init Iron Success ");
      AdsManager.Instance.InitAdsType(AdsMediationType.IRONSOURCE);
   }

   private void OnInitFailed(LevelPlayInitError obj)
   {
      Debug.Log("Init Iron Failed " + obj.ErrorMessage);
   }

   private void IronSourceEvents_onImpressionSuccessEvent(IronSourceImpressionData impressionData)
   {
      if (impressionData?.revenue != null)
      {
         Debug.Log("ImpressionData: " + impressionData.ToString());
         double revenue = impressionData.revenue.Value;
         ImpressionData impression = new ImpressionData
         {
            ad_mediation = AdsMediationType.IRONSOURCE,
            ad_source = impressionData.adNetwork,
            ad_unit_name = impressionData.instanceName,
            ad_format = impressionData.adFormat,
            ad_currency = "USD",
            ad_revenue = revenue,
            ad_type = impressionData.adFormat
         };
         AdRevenuePaidCallback?.Invoke(impression);
      }
   }

   #region InterstitialAd

   private LevelPlayInterstitialAd interstitialAd;

   public override void InitInterstitialAd(UnityAction adClosedCallback, UnityAction adLoadSuccessCallback,
      UnityAction adLoadFailedCallback, UnityAction adShowSuccessCallback, UnityAction adShowFailCallback)
   {
      base.InitInterstitialAd(adClosedCallback, adLoadSuccessCallback, adLoadFailedCallback, adShowSuccessCallback,
         adShowFailCallback);
      Debug.Log("Init IronSource Interstitial");
#if IRONSOURCE_LEGACY
        CreateInterstitialAdLegacy();
#else
      CreateInterstitialAd();
#endif
      RequestInterstitialAd();
   }

   private void CreateInterstitialAdLegacy()
   {
#if IRONSOURCE_LEGACY
        IronSource.Agent.init(AppKey, IronSourceAdUnits.INTERSTITIAL);
        IronSourceInterstitialEvents.onAdLoadFailedEvent += LegacyInterstitialOnAdLoadFailedEvent;
        IronSourceInterstitialEvents.onAdReadyEvent += LegacyInterstitialOnAdLoadedEvent;
        IronSourceInterstitialEvents.onAdClickedEvent += LegacyInterstitialOnAdClickedEvent;
        IronSourceInterstitialEvents.onAdOpenedEvent += LegacyInterstitialOnAdDisplayedEvent;
        IronSourceInterstitialEvents.onAdClosedEvent += LegacyInterstitialOnAdClosedEvent;
        IronSourceInterstitialEvents.onAdShowFailedEvent += LegacyInterstitialOnAdDisplayFailedEvent;
        IronSourceInterstitialEvents.onAdShowSucceededEvent += LegacyInterstitialOnAdInfoChangedEvent;
#endif
   }

   #region Interstitial Event Legacy

   private void LegacyInterstitialOnAdInfoChangedEvent(IronSourceAdInfo adInfo)
   {
      Debug.Log("IronSource: I got InterstitialAdInfoChangedEvent");
   }

   private void LegacyInterstitialOnAdDisplayFailedEvent(IronSourceError error, IronSourceAdInfo adInfo)
   {
      Debug.Log("IronSource: I got InterstitialAdDisplayFailedEvent");
      m_InterstitialAdLoadFailCallback?.Invoke();
   }

   private void LegacyInterstitialOnAdClosedEvent(IronSourceAdInfo adInfo)
   {
      Debug.Log("IronSource: I got InterstitialAdClosedEvent");
      m_InterstitialAdCloseCallback?.Invoke();
   }

   private void LegacyInterstitialOnAdDisplayedEvent(IronSourceAdInfo adInfo)
   {
      Debug.Log("IronSource: I got InterstitialAdDisplayedEvent");
      m_InterstitialAdShowSuccessCallback?.Invoke();
   }

   private void LegacyInterstitialOnAdClickedEvent(IronSourceAdInfo adInfo)
   {
      Debug.Log("IronSource: I got InterstitialAdClickedEvent");
   }

   private void LegacyInterstitialOnAdLoadedEvent(IronSourceAdInfo adInfo)
   {
      Debug.Log("IronSource: I got InterstitialAdLoadedEvent");
      m_InterstitialAdLoadSuccessCallback?.Invoke();
   }

   private void LegacyInterstitialOnAdLoadFailedEvent(IronSourceError adInfo)
   {
      Debug.Log("IronSource: I got InterstitialAdLoadFailedEvent");
      m_InterstitialAdLoadFailCallback?.Invoke();
   }

   #endregion

   private void CreateInterstitialAd()
   {
      Debug.Log("Create InterstitialAd");
      interstitialAd = new LevelPlayInterstitialAd(interstitialAdUnitID);

      // Register to interstitial events
      interstitialAd.OnAdLoaded += InterstitialOnAdLoadedEvent;
      interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
      interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
      interstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
      interstitialAd.OnAdClicked += InterstitialOnAdClickedEvent;
      interstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;
      interstitialAd.OnAdInfoChanged += InterstitialOnAdInfoChangedEvent;
      Debug.Log("Create InterstitialAd Done");
   }

   #region Interstitial Event

   private void InterstitialOnAdLoadedEvent(LevelPlayAdInfo adInfo)
   {
      Debug.Log("IronSource: I got InterstitialAdLoadedEvent");
      m_InterstitialAdLoadSuccessCallback?.Invoke();
   }

   private void InterstitialOnAdLoadFailedEvent(LevelPlayAdError error)
   {
      Debug.Log("IronSource: I got InterstitialAdLoadFailedEvent");
      m_InterstitialAdLoadFailCallback?.Invoke();
   }

   private void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
   {
      Debug.Log("IronSource: I got InterstitialAdDisplayedEvent");
      m_InterstitialAdShowSuccessCallback?.Invoke();
   }

   private void InterstitialOnAdDisplayFailedEvent(LevelPlayAdDisplayInfoError error)
   {
      Debug.Log("IronSource: I got InterstitialAdDisplayFailedEvent");
      m_InterstitialAdShowFailCallback?.Invoke();
   }

   private void InterstitialOnAdClickedEvent(LevelPlayAdInfo adInfo)
   {
      Debug.Log("IronSource: I got InterstitialAdClickedEvent");
   }

   private void InterstitialOnAdClosedEvent(LevelPlayAdInfo adInfo)
   {
      Debug.Log("IronSource: I got InterstitialAdClosedEvent");
      m_InterstitialAdCloseCallback?.Invoke();
   }

   private void InterstitialOnAdInfoChangedEvent(LevelPlayAdInfo adInfo)
   {
   }

   #endregion

   public override void RequestInterstitialAd()
   {
      base.RequestInterstitialAd();
      Debug.Log("Request IronSource Interstitial");
#if IRONSOURCE_LEGACY
        IronSource.Agent.loadInterstitial();
#else
      interstitialAd?.LoadAd();
#endif
   }

   public override void ShowInterstitialAd()
   {
      base.ShowInterstitialAd();
      Debug.Log("Show Iron source interstitial");
#if IRONSOURCE_LEGACY
        IronSource.Agent.showInterstitial();
#else
      interstitialAd?.ShowAd();
#endif
   }

   public override bool IsInterstitialLoaded()
   {
#if IRONSOURCE_LEGACY
        return IronSource.Agent.isInterstitialReady();
#else
      return interstitialAd != null && interstitialAd.IsAdReady();
#endif
   }

   #endregion InterstitialAd

   #region RewardAd

   public override void InitRewardVideoAd(UnityAction<bool> videoClosed, UnityAction videoLoadSuccess,
      UnityAction videoLoadFailed, UnityAction videoStart)
   {
      base.InitRewardVideoAd(videoClosed, videoLoadSuccess, videoLoadFailed, videoStart);
      Debug.Log("Init IronSource video");
      //Add AdInfo Rewarded Video Events
      IronSourceRewardedVideoEvents.onAdOpenedEvent += RewardedVideoOnAdOpenedEvent;
      IronSourceRewardedVideoEvents.onAdClosedEvent += RewardedVideoOnAdClosedEvent;
      IronSourceRewardedVideoEvents.onAdAvailableEvent += RewardedVideoOnAdAvailable;
      IronSourceRewardedVideoEvents.onAdUnavailableEvent += RewardedVideoOnAdUnavailable;
      IronSourceRewardedVideoEvents.onAdShowFailedEvent += RewardedVideoOnAdShowFailedEvent;
      IronSourceRewardedVideoEvents.onAdRewardedEvent += RewardedVideoOnAdRewardedEvent;
      IronSourceRewardedVideoEvents.onAdClickedEvent += RewardedVideoOnAdClickedEvent;

      IronSource.Agent.shouldTrackNetworkState(true);
      IronSource.Agent.init(AppKey, IronSourceAdUnits.REWARDED_VIDEO);
   }

   private void RewardedVideoOnAdClickedEvent(IronSourcePlacement placement, IronSourceAdInfo adInfo)
   {
      Debug.Log("IronSource: I got RewardedVideoAdClickedEvent, name = " + placement.getRewardName());
   }

   private void RewardedVideoOnAdRewardedEvent(IronSourcePlacement placement, IronSourceAdInfo adInfo)
   {
      Debug.Log("IronSource: I got RewardedVideoAdRewardedEvent, name = " + placement.getRewardName());
      m_IsWatchSuccess = true;
      switch (Application.platform)
      {
         case RuntimePlatform.Android:
         {
            if (m_RewardedVideoEarnSuccessCallback != null)
            {
               Debug.Log("Watch video Success Callback!");
               EventManager.AddEventNextFrame(m_RewardedVideoEarnSuccessCallback);
               m_RewardedVideoEarnSuccessCallback = null;
            }

            break;
         }
         case RuntimePlatform.IPhonePlayer:
         {
            if (m_RewardedVideoEarnSuccessCallback != null)
            {
               Debug.Log("Watch video Success Callback!");
               EventManager.AddEventNextFrame(m_RewardedVideoEarnSuccessCallback);
               m_RewardedVideoEarnSuccessCallback = null;
            }

            break;
         }
      }
   }

   private void RewardedVideoOnAdShowFailedEvent(IronSourceError error, IronSourceAdInfo adInfo)
   {
      Debug.Log("IronSource: I got RewardedVideoAdShowFailedEvent, code :  " + error.getCode() + ", description : " +
                error.getDescription());
      m_RewardedVideoLoadFailedCallback?.Invoke();
   }

   private void RewardedVideoOnAdUnavailable()
   {
      Debug.Log("IronSource: RewardedVideoAd Iron Loaded Fail");
      m_RewardedVideoLoadFailedCallback?.Invoke();
   }

   private void RewardedVideoOnAdAvailable(IronSourceAdInfo adInfo)
   {
      Debug.Log("IronSource: RewardedVideoAds Available");
      m_RewardedVideoLoadSuccessCallback?.Invoke();
   }

   private void RewardedVideoOnAdClosedEvent(IronSourceAdInfo adInfo)
   {
      Debug.Log("IronSource: I got RewardedVideoAdClosedEvent");
      if (m_RewardedVideoEarnSuccessCallback != null && m_IsWatchSuccess)
      {
         Debug.Log("Do Callback Success");
         EventManager.AddEventNextFrame(m_RewardedVideoEarnSuccessCallback);
         m_RewardedVideoEarnSuccessCallback = null;
      }
      else
      {
         Debug.Log("Don't have any callback");
      }

      m_RewardedVideoCloseCallback?.Invoke(m_IsWatchSuccess);
   }

   private void RewardedVideoOnAdOpenedEvent(IronSourceAdInfo adInfo)
   {
      Debug.Log("IronSource: I got RewardedVideoAdOpenedEvent");
   }

   public override void RequestRewardVideoAd()
   {
      base.RequestRewardVideoAd();
      Debug.Log("Request ironsource Video");
   }

   public override void ShowRewardVideoAd(UnityAction successCallback, UnityAction failedCallback)
   {
      base.ShowRewardVideoAd(successCallback, failedCallback);
#if !UNITY_EDITOR
        m_IsWatchSuccess = false;
        IronSource.Agent.showRewardedVideo();
#else
      m_IsWatchSuccess = false;
      RewardedVideoOnAdRewardedEvent(null, null);
#endif
   }

   public override bool IsRewardVideoLoaded()
   {
#if !UNITY_EDITOR
        return IronSource.Agent.isRewardedVideoAvailable();
#else
      return true;
#endif
   }

   #endregion RewardAd

   #region Banner

   bool isBannerLoaded = false;
   private bool isDestroyedBannerAd = true;
   private LevelPlayBannerAd bannerAd;

   public override void InitBannerAds(
      UnityAction bannerLoadedSuccessCallback,
      UnityAction bannerAdLoadedFailCallback,
      UnityAction bannerAdsCollapsed,
      UnityAction bannerAdsExpandedCallback,
      UnityAction bannerAdsDisplayed = null,
      UnityAction bannerAdsDisplayedFailedCallback = null,
      UnityAction bannerAdsClickedCallback = null)
   {
      base.InitBannerAds(
         bannerLoadedSuccessCallback,
         bannerAdLoadedFailCallback,
         bannerAdsCollapsed,
         bannerAdsExpandedCallback,
         bannerAdsDisplayed,
         bannerAdsDisplayedFailedCallback,
         bannerAdsClickedCallback);
      Debug.Log("IronSource: Init Banner");
      isDestroyedBannerAd = true;
#if !IRONSOURCE_LEGACY
      CreateBannerAds();
#else
        InitEventBannerLegacy();
        CreateBannerAdsLegacy();
#endif
      RequestBannerAds();
   }

   public override void CreateBannerAds()
   {
      LevelPlayAdSize adSize = LevelPlayAdSize.CreateAdaptiveAdSize();
      bannerAd = new LevelPlayBannerAd(bannerAdUnitID, adSize, LevelPlayBannerPosition.BottomCenter);

      bannerAd.OnAdLoaded += BannerOnAdLoadedEvent;
      bannerAd.OnAdLoadFailed += BannerOnAdLoadFailedEvent;
      bannerAd.OnAdDisplayed += BannerOnAdDisplayedEvent;
      bannerAd.OnAdDisplayFailed += BannerOnAdDisplayFailedEvent;
      bannerAd.OnAdClicked += BannerOnAdClickedEvent;
      bannerAd.OnAdCollapsed += BannerOnAdCollapsedEvent;
      bannerAd.OnAdLeftApplication += BannerOnAdLeftApplicationEvent;
      bannerAd.OnAdExpanded += BannerOnAdExpandedEvent;
      isDestroyedBannerAd = false;
   }

   private void CreateBannerAdsLegacy()
   {
#if IRONSOURCE_LEGACY
        IronSource.Agent.init(AppKey, IronSourceAdUnits.BANNER);
#endif
   }

   #region Banner Event

   #region IronSource Banner Event

   private void BannerOnAdLoadedEvent(LevelPlayAdInfo adInfo)
   {
      Debug.Log("IronSource: I got BannerAdLoadedEvent");
      isBannerLoaded = true;
      m_BannerAdLoadedSuccessCallback?.Invoke();
   }

   private void BannerOnAdLoadFailedEvent(LevelPlayAdError error)
   {
      Debug.Log("IronSource: I got BannerAdLoadFailedEvent " + error.ErrorMessage);
      isBannerLoaded = false;
      m_BannerAdLoadedFailCallback?.Invoke();
   }

   private void BannerOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
   {
      Debug.Log("IronSource: I got BannerAdDisplayedEvent");
      m_BannerAdsDisplayedCallback?.Invoke();
   }

   private void BannerOnAdDisplayFailedEvent(LevelPlayAdDisplayInfoError adDisplayInfoError)
   {
      Debug.Log("IronSource: I got BannerAdDisplayFailedEvent");
      m_BannerAdsDisplayedFailedCallback?.Invoke();
   }

   private void BannerOnAdClickedEvent(LevelPlayAdInfo adInfo)
   {
      Debug.Log("IronSource: I got BannerAdClickedEvent");
      m_BannerAdsClickedCallback?.Invoke();
   }

   private void BannerOnAdCollapsedEvent(LevelPlayAdInfo adInfo)
   {
      Debug.Log("IronSource: I got BannerAdCollapsedEvent");
      m_BannerAdsCollapsedCallback?.Invoke();
   }

   private void BannerOnAdExpandedEvent(LevelPlayAdInfo adInfo)
   {
      Debug.Log("IronSource: I got BannerAdExpandedEvent");
      m_BannerAdsExpandedCallback?.Invoke();
   }

   private void BannerOnAdLeftApplicationEvent(LevelPlayAdInfo adInfo)
   {
      Debug.Log("IronSource: I got BannerAdLeftApplicationEvent");
   }

   #endregion

   #region IronSource Legacy Banner Event

   private void InitEventBannerLegacy()
   {
      IronSourceBannerEvents.onAdLoadedEvent += BannerOnAdLoadedEventLegacy;
      IronSourceBannerEvents.onAdLoadFailedEvent += BannerOnAdLoadFailedEventLegacy;
   }

   private void BannerOnAdLoadFailedEventLegacy(IronSourceError adError)
   {
      Debug.Log("IronSource: I got BannerAdLoadFailedEvent " + adError.getDescription() + " code : " +
                adError.getErrorCode());
   }

   private void BannerOnAdLoadedEventLegacy(IronSourceAdInfo adInfo)
   {
      Debug.Log("IronSource: I got BannerAdLoadedEvent");
      isBannerLoaded = true;
      m_BannerAdLoadedSuccessCallback?.Invoke();
   }

   #endregion

   #endregion

   public override void RequestBannerAds()
   {
      base.RequestBannerAds();
      Debug.Log("IronSource: Request IronSource Banner");
      isBannerLoaded = false;
#if IRONSOURCE_LEGACY
            IronSource.Agent.loadBanner(IronSourceBannerSize.BANNER, IronSourceBannerPosition.BOTTOM);
#else
      if (isDestroyedBannerAd || bannerAd == null)
      {
         CreateBannerAds();
      }

      bannerAd?.LoadAd();
#endif
   }

   public override void ShowBannerAds()
   {
      base.ShowBannerAds();
      Debug.Log("IronSource: Show IronSource Banner");
#if IRONSOURCE_LEGACY
        IronSource.Agent.displayBanner();
#else
      bannerAd?.ShowAd();
#endif
   }

   public override void HideBannerAds()
   {
      base.HideBannerAds();
#if IRONSOURCE_LEGACY
      IronSource.Agent.hideBanner();
#else
      bannerAd?.HideAd();
#endif
   }

   public override void DestroyBannerAds()
   {
      base.DestroyBannerAds();
#if IRONSOURCE_LEGACY
        IronSource.Agent.destroyBanner();
#else
      bannerAd?.DestroyAd();
#endif
      isDestroyedBannerAd = true;
   }

   public override bool IsBannerLoaded()
   {
      return isBannerLoaded;
   }

   #endregion

   void OnApplicationPause(bool isPaused)
   {
      IronSource.Agent.onApplicationPause(isPaused);
   }


#endif

#endif
   public override AdsMediationType GetAdsMediationType()
   {
      return AdsMediationType.IRONSOURCE;
   }
}