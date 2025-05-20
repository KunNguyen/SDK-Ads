using SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using ABIMaxSDKAds.Scripts;
using UnityEngine;
using UnityEngine.Events;

public class IronSourceMediationController : AdsMediationController
{
#if UNITY_AD_IRONSOURCE
#if UNITY_ANDROID
   public string Android_Key;
   public string IOS_Key;

   private bool IsWatchSuccess { get; set; } = false;

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
      DebugAds.Log("IronSource: MyAppStart Start called");

      string id = IronSource.Agent.getAdvertiserId();
      DebugAds.Log("IronSource: IronSource.Agent.getAdvertiserId : " + id);
      DebugAds.Log("IronSource: unity version" + IronSource.unityVersion());

      // SDK init
      DebugAds.Log("IronSource: IronSource.Agent.init");
      string uniqueUserID = SystemInfo.deviceUniqueIdentifier;
      IronSource.Agent.setUserId(uniqueUserID);
      IronSource.Agent.setConsent(true);
      IronSource.Agent.setMetaData("do_not_sell", "false");
      IronSource.Agent.setMetaData("is_child_directed", "false");
      IronSource.Agent.setMetaData("is_test_suite", "enable");
      IronSourceEvents.onImpressionDataReadyEvent += IronSourceEvents_onImpressionSuccessEvent;
      IronSourceEvents.onSdkInitializationCompletedEvent += () =>
      {
         DebugAds.Log("IronSource: onSdkInitializationCompletedEvent");
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
      DebugAds.Log("Init Iron Success ");
      AdsManager.Instance.InitAds(AdsMediationType.IRONSOURCE);
   }

   private void OnInitFailed(LevelPlayInitError obj)
   {
      DebugAds.Log("Init Iron Failed " + obj.ErrorMessage);
   }

   private void IronSourceEvents_onImpressionSuccessEvent(IronSourceImpressionData impressionData)
   {
      if (impressionData?.revenue != null)
      {
         DebugAds.Log("ImpressionData: " + impressionData.ToString());
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
      DebugAds.Log("Init IronSource Interstitial");
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
      DebugAds.Log("IronSource: I got InterstitialAdInfoChangedEvent");
   }

   private void LegacyInterstitialOnAdDisplayFailedEvent(IronSourceError error, IronSourceAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got InterstitialAdDisplayFailedEvent");
      InterstitialCallbacks.DisplayedFail?.Invoke();
   }

   private void LegacyInterstitialOnAdClosedEvent(IronSourceAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got InterstitialAdClosedEvent");
      InterstitialCallbacks.Closed?.Invoke(true);
   }

   private void LegacyInterstitialOnAdDisplayedEvent(IronSourceAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got InterstitialAdDisplayedEvent");
      InterstitialCallbacks.Displayed?.Invoke();
   }

   private void LegacyInterstitialOnAdClickedEvent(IronSourceAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got InterstitialAdClickedEvent");
   }

   private void LegacyInterstitialOnAdLoadedEvent(IronSourceAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got InterstitialAdLoadedEvent");
      InterstitialCallbacks.LoadedSuccess?.Invoke();
   }

   private void LegacyInterstitialOnAdLoadFailedEvent(IronSourceError adInfo)
   {
      DebugAds.Log("IronSource: I got InterstitialAdLoadFailedEvent");
      InterstitialCallbacks.LoadedFail?.Invoke();
   }

   #endregion

   private void CreateInterstitialAd()
   {
      DebugAds.Log("Create InterstitialAd");
      interstitialAd = new LevelPlayInterstitialAd(interstitialAdUnitID);

      // Register to interstitial events
      interstitialAd.OnAdLoaded += InterstitialOnAdLoadedEvent;
      interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
      interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
      interstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
      interstitialAd.OnAdClicked += InterstitialOnAdClickedEvent;
      interstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;
      interstitialAd.OnAdInfoChanged += InterstitialOnAdInfoChangedEvent;
      DebugAds.Log("Create InterstitialAd Done");
   }

   #region Interstitial Event

   private void InterstitialOnAdLoadedEvent(LevelPlayAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got InterstitialAdLoadedEvent");
      InterstitialCallbacks.LoadedSuccess?.Invoke();
   }

   private void InterstitialOnAdLoadFailedEvent(LevelPlayAdError error)
   {
      DebugAds.Log("IronSource: I got InterstitialAdLoadFailedEvent");
      InterstitialCallbacks.LoadedFail?.Invoke();
   }

   private void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got InterstitialAdDisplayedEvent");
      InterstitialCallbacks.Displayed?.Invoke();
   }

   private void InterstitialOnAdDisplayFailedEvent(LevelPlayAdDisplayInfoError error)
   {
      DebugAds.Log("IronSource: I got InterstitialAdDisplayFailedEvent");
      InterstitialCallbacks.DisplayedFail?.Invoke();
   }

   private void InterstitialOnAdClickedEvent(LevelPlayAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got InterstitialAdClickedEvent");
   }

   private void InterstitialOnAdClosedEvent(LevelPlayAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got InterstitialAdClosedEvent");
      InterstitialCallbacks.Closed?.Invoke(true);
   }

   private void InterstitialOnAdInfoChangedEvent(LevelPlayAdInfo adInfo)
   {
   }

   #endregion

   public override void RequestInterstitialAd()
   {
      base.RequestInterstitialAd();
      DebugAds.Log("Request IronSource Interstitial");
#if IRONSOURCE_LEGACY
        IronSource.Agent.loadInterstitial();
#else
      interstitialAd?.LoadAd();
#endif
   }

   public override void ShowInterstitialAd()
   {
      base.ShowInterstitialAd();
      DebugAds.Log("Show Iron source interstitial");
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
      DebugAds.Log("Init IronSource video");
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
      DebugAds.Log("IronSource: I got RewardedVideoAdClickedEvent, name = " + placement.getRewardName());
   }

   private void RewardedVideoOnAdRewardedEvent(IronSourcePlacement placement, IronSourceAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got RewardedVideoAdRewardedEvent, name = " + placement.getRewardName());
      IsWatchSuccess = true;
      switch (Application.platform)
      {
         case RuntimePlatform.Android:
         {
            if (RewardedVideoCallbacks.Completed != null)
            {
               DebugAds.Log("Watch video Success Callback!");
               EventManager.AddEventNextFrame(RewardedVideoCallbacks.Completed);
               RewardedVideoCallbacks.Completed = null;
            }

            break;
         }
         case RuntimePlatform.IPhonePlayer:
         {
            if (RewardedVideoCallbacks.Completed != null)
            {
               DebugAds.Log("Watch video Success Callback!");
               EventManager.AddEventNextFrame(RewardedVideoCallbacks.Completed);
               RewardedVideoCallbacks.Completed = null;
            }

            break;
         }
      }
   }

   private void RewardedVideoOnAdShowFailedEvent(IronSourceError error, IronSourceAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got RewardedVideoAdShowFailedEvent, code :  " + error.getCode() + ", description : " +
                error.getDescription());
      RewardedVideoCallbacks.DisplayedFailed?.Invoke();
   }

   private void RewardedVideoOnAdUnavailable()
   {
      DebugAds.Log("IronSource: RewardedVideoAd Iron Loaded Fail");
      RewardedVideoCallbacks.LoadedFail?.Invoke();
   }

   private void RewardedVideoOnAdAvailable(IronSourceAdInfo adInfo)
   {
      DebugAds.Log("IronSource: RewardedVideoAds Available");
      RewardedVideoCallbacks.LoadedSuccess?.Invoke();
   }

   private void RewardedVideoOnAdClosedEvent(IronSourceAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got RewardedVideoAdClosedEvent");
      if (RewardedVideoCallbacks.Completed != null && IsWatchSuccess)
      {
         DebugAds.Log("Do Callback Success");
         EventManager.AddEventNextFrame(RewardedVideoCallbacks.Completed);
         RewardedVideoCallbacks.Completed = null;
      }
      else
      {
         DebugAds.Log("Don't have any callback");
      }

      RewardedVideoCallbacks.Closed?.Invoke(IsWatchSuccess);
   }

   private void RewardedVideoOnAdOpenedEvent(IronSourceAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got RewardedVideoAdOpenedEvent");
   }

   public override void RequestRewardVideoAd()
   {
      base.RequestRewardVideoAd();
      DebugAds.Log("Request ironsource Video");
   }

   public override void ShowRewardVideoAd(UnityAction successCallback, UnityAction failedCallback)
   {
      base.ShowRewardVideoAd(successCallback, failedCallback);
#if !UNITY_EDITOR
        m_IsWatchSuccess = false;
        IronSource.Agent.showRewardedVideo();
#else
      IsWatchSuccess = false;
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
      DebugAds.Log("IronSource: Init Banner");
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
      DebugAds.Log("IronSource: I got BannerAdLoadedEvent");
      isBannerLoaded = true;
      BannerCallbacks.LoadedSuccess?.Invoke();
   }

   private void BannerOnAdLoadFailedEvent(LevelPlayAdError error)
   {
      DebugAds.Log("IronSource: I got BannerAdLoadFailedEvent " + error.ErrorMessage);
      isBannerLoaded = false;
      BannerCallbacks.LoadedFail?.Invoke();
   }

   private void BannerOnAdDisplayedEvent(LevelPlayAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got BannerAdDisplayedEvent");
      BannerCallbacks.Displayed?.Invoke();
   }

   private void BannerOnAdDisplayFailedEvent(LevelPlayAdDisplayInfoError adDisplayInfoError)
   {
      DebugAds.Log("IronSource: I got BannerAdDisplayFailedEvent");
      BannerCallbacks.DisplayedFail?.Invoke();
   }

   private void BannerOnAdClickedEvent(LevelPlayAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got BannerAdClickedEvent");
      BannerCallbacks.Clicked?.Invoke();
   }

   private void BannerOnAdCollapsedEvent(LevelPlayAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got BannerAdCollapsedEvent");
      BannerCallbacks.Collapsed?.Invoke();
   }

   private void BannerOnAdExpandedEvent(LevelPlayAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got BannerAdExpandedEvent");
      BannerCallbacks.Expanded?.Invoke();
   }

   private void BannerOnAdLeftApplicationEvent(LevelPlayAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got BannerAdLeftApplicationEvent");
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
      DebugAds.Log("IronSource: I got BannerAdLoadFailedEvent " + adError.getDescription() + " code : " +
                adError.getErrorCode());
   }

   private void BannerOnAdLoadedEventLegacy(IronSourceAdInfo adInfo)
   {
      DebugAds.Log("IronSource: I got BannerAdLoadedEvent");
      isBannerLoaded = true;
      BannerCallbacks.LoadedSuccess?.Invoke();
   }

   #endregion

   #endregion

   public override void RequestBannerAds()
   {
      base.RequestBannerAds();
      DebugAds.Log("IronSource: Request IronSource Banner");
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
      DebugAds.Log("IronSource: Show IronSource Banner");
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