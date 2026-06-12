using System;
using System.Collections;
using System.Collections.Generic;
using JisSDKAds.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace JisSDKAds.Ads
{
#if UNITY_AD_ADMOB
     using GoogleMobileAds.Api;
     using JisSDKAds.Ads.SequentialTier;
     using JisSDKAds.Providers.AdMob;
#endif

     /// <summary>
     /// AdMob mediation controller. Class luôn tồn tại để prefab có thể reference.
     /// Implementation thực chỉ compile khi UNITY_AD_ADMOB được định nghĩa.
     /// </summary>
     public partial class AdmobMediationController : AdsMediationController, IAdMobRewardedInterstitialHost, IAdMobConsentHost
     {
          public AdmobAdSetup m_AdmobAdSetup;

#if UNITY_AD_ADMOB
          private InterstitialAd InterstitialAds { get; set; }
          private RewardedAd RewardVideoAds { get; set; }
          private AppOpenAd AppOpenAd { get; set; }
          private bool IsWatchSuccess { get; set; } = false;
          #region Init

          public override void Init()
          {
               if (Status == MediationStatus.Inited)
                    return;

               if (AdMobMobileAdsInitializer.IsReady)
               {
                    Status = MediationStatus.Inited;
                    return;
               }

               if (Status != MediationStatus.NotInited)
                    return;

               base.Init();
               AdMobMobileAdsInitializer.EnsureInitialized(IsActiveConsent, OnSharedMobileAdsInitComplete);
          }

          void OnSharedMobileAdsInitComplete(bool success)
          {
               if (Status == MediationStatus.Inited || Status == MediationStatus.FailedToInit)
                    return;

               Status = success ? MediationStatus.Inited : MediationStatus.FailedToInit;
          }

          #endregion

          #region Consent

          public void ShowConsentFormAgain()
          {
               if (!AdmobUmpConsent.IsAvailable)
               {
                    DebugAds.LogWarning($"[AdMob] UMP not available. {AdmobUmpConsent.PluginHint}");
                    return;
               }

               AdmobUmpConsent.ShowPrivacyOptionsForm(
                    msg => DebugAds.LogError("Lỗi CMP: " + msg),
                    () => DebugAds.Log("User đã cập nhật consent."));
          }

          #endregion
          
          #region Banner Ads

          private BannerView BannerViewAds { get; set; }
          public AdPosition m_BannerPosition;
          public bool IsBannerShowingOnStart = false;
          private bool _bannerAdLoaded;
          private int retryTemp = 0;

          public override void InitBannerAds(
               UnityAction bannerLoadedCallback, UnityAction bannerAdLoadedFailCallback,
               UnityAction bannerAdsCollapsedCallback, UnityAction bannerAdsExpandedCallback,
               UnityAction bannerAdsDisplayed = null, UnityAction bannerAdsDisplayedFailedCallback = null,
               UnityAction bannerAdsClickedCallback = null)
          {
               base.InitBannerAds(
                    bannerLoadedCallback, bannerAdLoadedFailCallback, bannerAdsCollapsedCallback,
                    bannerAdsExpandedCallback, bannerAdsDisplayed, bannerAdsDisplayedFailedCallback,
                    bannerAdsClickedCallback);
               DebugAds.Log("Init Admob Banner");
               RequestBannerAds();
               if (!IsBannerShowingOnStart)
               {
                    BannerViewAds.Hide();
               }
               else
               {
                    BannerViewAds.Show();
               }
          }

          private BannerView CreateBannerView()
          {
               DebugAds.Log("Creating banner view");
               string adUnitId = GetBannerID();
               BannerView bannerView = new BannerView(adUnitId, AdSize.Banner, m_BannerPosition);
               RegisterBannerEvents(bannerView);
               return bannerView;
          }

          private void LoadBannerAds(BannerView bannerView)
          {
               AdRequest adRequest = new AdRequest();
               bannerView?.LoadAd(adRequest);
          }

          public override void RequestBannerAds()
          {
               base.RequestBannerAds();
               DestroyBannerAds();
               BannerViewAds = CreateBannerView();
               _bannerAdLoaded = false;
               LoadBannerAds(BannerViewAds);
          }

          private void RegisterBannerEvents(BannerView bannerView)
          {
               bannerView.OnBannerAdLoaded += () => { OnAdBannerLoaded(bannerView); };
               bannerView.OnBannerAdLoadFailed += OnAdBannerFailedToLoad;
               bannerView.OnAdFullScreenContentOpened += () => { OnAdBannerOpened(bannerView); };
               bannerView.OnAdFullScreenContentClosed += OnAdBannerClosed;
               bannerView.OnAdClicked += OnAdBannerClicked;
               bannerView.OnAdPaid += OnAdBannerPaid;
          }

          public override void ShowBannerAds()
          {
               base.ShowBannerAds();

               if (BannerViewAds != null)
               {
                    DebugAds.Log("Start Show banner ads");
                    BannerViewAds.Show();
               }
               else
               {
                    DebugAds.Log("Banner is not loaded yet");
                    RequestBannerAds();
                    BannerViewAds?.Show();
               }
          }

          public override void HideBannerAds()
          {
               base.HideBannerAds();
               BannerViewAds?.Hide();
          }

          public override bool IsBannerLoaded()
          {
               return BannerViewAds != null && _bannerAdLoaded;
          }

          private void OnAdBannerLoaded(BannerView bannerView)
          {
               DebugAds.Log("HandleAdLoaded event received");
               _bannerAdLoaded = true;
               BannerCallbacks.LoadedSuccess?.Invoke();
          }

          private void OnAdBannerFailedToLoad(LoadAdError args)
          {
               DebugAds.Log("AdmobBanner Fail: " + args.GetMessage());
               _bannerAdLoaded = false;
               BannerCallbacks.LoadedFail?.Invoke();
          }
          private void OnAdBannerOpened(BannerView bannerView)
          {
               DebugAds.Log("AdmobBanner Opened");
               BannerCallbacks.Displayed?.Invoke();
          }

          private void OnAdBannerClosed()
          {
               DebugAds.Log("AdmobBanner Closed");
               BannerCallbacks.Collapsed?.Invoke();
          }
          private void OnAdBannerClicked()
          {
               DebugAds.Log("AdmobBanner Clicked");
               BannerCallbacks.Clicked?.Invoke();
          }

          private void OnAdBannerPaid(AdValue adValue)
          {
               DebugAds.Log("AdmobBanner Paid");
               HandleAdPaidEvent("banner", adValue, BannerViewAds.GetResponseInfo());
          }

          /// <summary>
          /// Destroys the ad.
          /// </summary>
          public override void DestroyBannerAds()
          {
               base.DestroyBannerAds();
               _bannerAdLoaded = false;
               if (BannerViewAds != null)
               {
                    DebugAds.Log("Destroying banner ad.");
                    BannerViewAds.Destroy();
                    BannerViewAds = null;
               }
               else
               {
                    DebugAds.Log("Don't have any banner to destroy.");
               }
          }

          private string GetBannerID()
          {
               return m_AdmobAdSetup.BannerAdUnitID.ID;
          }

          #endregion

          #region Interstitial

          public override void InitInterstitialAd(UnityAction adClosedCallback, UnityAction adLoadSuccessCallback,
               UnityAction adLoadFailedCallback, UnityAction adShowSuccessCallback, UnityAction adShowFailCallback)
          {
               base.InitInterstitialAd(adClosedCallback, adLoadSuccessCallback, adLoadFailedCallback,
                    adShowSuccessCallback, adShowFailCallback);
               DebugAds.Log("Init Admob Interstitial");
          }

          public override void RequestInterstitialAd()
          {
               base.RequestInterstitialAd();
               if (AdsManager.UsesJisAdsCoreForStandardLoads())
                    return;

               if (UseSequentialInterstitial)
               {
                    EnsureInterstitialTierLoader();
                    _interstitialTierLoader?.Load();
                    return;
               }

               RequestInterstitialLegacy();
          }

          private void RegisterInterstitialAd(InterstitialAd interstitialAd)
          {
               interstitialAd.OnAdFullScreenContentClosed += OnCloseInterstitialAd;
               interstitialAd.OnAdFullScreenContentOpened += OnAdInterstitialOpening;
               interstitialAd.OnAdFullScreenContentFailed += OnAdInterstitialFailToShow;
               interstitialAd.OnAdPaid += OnAdInterstitialPaid;
          }

          public override bool IsInterstitialLoaded()
          {
               if (UseSequentialInterstitial)
               {
                    if (AdsManager.UsesJisAdsCoreForStandardLoads())
                         return false;
                    EnsureInterstitialTierLoader();
                    return _interstitialTierLoader != null && _interstitialTierLoader.IsReady;
               }

               return InterstitialAds != null && InterstitialAds.CanShowAd();
          }

          public override void ShowInterstitialAd()
          {
               base.ShowInterstitialAd();
               if (UseSequentialInterstitial)
               {
                    if (AdsManager.UsesJisAdsCoreForStandardLoads())
                         return;
                    EnsureInterstitialTierLoader();
                    if (_interstitialTierLoader == null || !_interstitialTierLoader.Show())
                         OnAdInterstitialFailToShow(null);
                    return;
               }

               if (InterstitialAds != null && InterstitialAds.CanShowAd())
               {
                    InterstitialAds.Show();
               }
               else
               {
                    // Legacy path: ensure callers get a failure signal instead of silently doing nothing.
                    OnAdInterstitialFailToShow(null);
               }
          }

          private void OnCloseInterstitialAd()
          {
               DebugAds.Log("Close Interstitial");
               InterstitialCallbacks.Closed?.Invoke(true);

               // Legacy interstitials are one-time use. After closing, dispose and request a fresh ad.
               if (!UseSequentialInterstitial)
               {
                    try
                    {
                         InterstitialAds?.Destroy();
                    }
                    catch
                    {
                         // ignore best-effort cleanup
                    }

                    InterstitialAds = null;
                    RequestInterstitialLegacy();
               }
          }

          private void OnAdInterstitialSuccessToLoad()
          {
               DebugAds.Log("Load Interstitial success");
               InterstitialCallbacks.LoadedSuccess?.Invoke();
               if (!UseSequentialInterstitial)
                    m_AdmobAdSetup.InterstitialAdUnitID.Refresh();
          }

          private void OnAdInterstitialFailedToLoad()
          {
               DebugAds.Log("Load Interstitial failed Admob");
               InterstitialCallbacks.LoadedFail?.Invoke();
               if (!UseSequentialInterstitial)
                    m_AdmobAdSetup.InterstitialAdUnitID.ChangeID();
          }

          private void OnAdInterstitialOpening()
          {
               DebugAds.Log("Interstitial ad opened.");
               InterstitialCallbacks.Displayed?.Invoke();
          }

          private void OnAdInterstitialFailToShow(AdError e)
          {
               DebugAds.Log("Interstitial ad failed to show with error: " + (e != null ? e.GetMessage() : "unknown"));
               InterstitialCallbacks.DisplayedFail?.Invoke();

               // If the legacy instance got into a bad state, dispose and try to reload.
               if (!UseSequentialInterstitial)
               {
                    try
                    {
                         InterstitialAds?.Destroy();
                    }
                    catch
                    {
                         // ignore best-effort cleanup
                    }

                    InterstitialAds = null;
                    RequestInterstitialLegacy();
               }
          }

          public void DestroyInterstitialAd()
          {
               _interstitialTierLoader?.Destroy();
               _interstitialTierLoader = null;
               if (InterstitialAds != null)
               {
                    DebugAds.Log("Destroying interstitial ad.");
                    InterstitialAds.Destroy();
                    InterstitialAds = null;
               }
          }

          public string GetInterstitialAdUnit()
          {
               return m_AdmobAdSetup.InterstitialAdUnitID.ID;
          }

          #endregion

          #region Rewarded Ads

          public override void InitRewardVideoAd(UnityAction videoSuccess,UnityAction<bool> videoClosed, UnityAction videoLoadSuccess,
               UnityAction videoLoadFailed, UnityAction videoStart)
          {
               base.InitRewardVideoAd(videoSuccess, videoClosed, videoLoadSuccess, videoLoadFailed, videoStart);
               DebugAds.Log("Init Reward Video");
          }

          public override void RequestRewardVideoAd()
          {
               base.RequestRewardVideoAd();
               if (AdsManager.UsesJisAdsCoreForStandardLoads())
                    return;

               if (UseSequentialRewarded)
               {
                    EnsureRewardedTierLoader();
                    _rewardedTierLoader?.Load();
                    return;
               }

               RequestRewardVideoLegacy();
          }

          private void RegisterRewardAdEvent(RewardedAd rewardedAd)
          {
               rewardedAd.OnAdFullScreenContentOpened += OnRewardBasedVideoOpened;
               rewardedAd.OnAdFullScreenContentFailed += OnRewardedAdFailedToShow;
               rewardedAd.OnAdFullScreenContentClosed += OnRewardBasedVideoClosed;
               rewardedAd.OnAdPaid += OnAdRewardedAdPaid;
          }

          public override void ShowRewardVideoAd()
          {
               base.ShowRewardVideoAd();
               if (UseSequentialRewarded)
               {
                    if (AdsManager.UsesJisAdsCoreForStandardLoads())
                         return;
                    EnsureRewardedTierLoader();
                    IsWatchSuccess = false;
                    if (_rewardedTierLoader == null)
                    {
                         OnRewardedAdFailedToShow(null);
                         return;
                    }
                    if (!_rewardedTierLoader.IsReady)
                         AdLoadCoordinator.Instance.PrepareUrgentRewarded();
                    if (!_rewardedTierLoader.Show())
                         OnRewardedAdFailedToShow(null);
                    return;
               }

               if (IsRewardVideoLoaded())
               {
                    DebugAds.Log("RewardedVideoAd ADMOB Show");
                    IsWatchSuccess = false;
                    RewardVideoAds.Show((Reward reward) => { OnRewardBasedVideoRewarded(); });
               }
          }

          public override bool IsRewardVideoLoaded()
          {
#if UNITY_EDITOR
               return false;
#else
               if (UseSequentialRewarded)
               {
                    if (AdsManager.UsesJisAdsCoreForStandardLoads())
                         return false;
                    EnsureRewardedTierLoader();
                    return _rewardedTierLoader != null && _rewardedTierLoader.IsReady;
               }

               return RewardVideoAds != null && RewardVideoAds.CanShowAd();
#endif
          }

          private void OnRewardBasedVideoClosed()
          {
               DebugAds.Log("RewardedVideoAd ADMOB Closed");
               if (Application.platform == RuntimePlatform.IPhonePlayer)
               {
                    if (IsWatchSuccess)
                    {
                         if (RewardedVideoCallbacks.Completed != null)
                         {
                              EventManager.InvokeNextFrame(RewardedVideoCallbacks.Completed);
                         }
                    }
               }

               if (RewardedVideoCallbacks.Closed != null)
               {
                    EventManager.InvokeNextFrame(() => { RewardedVideoCallbacks.Closed.Invoke(IsWatchSuccess); });
               }
          }

          private void OnRewardBasedVideoRewarded()
          {
               DebugAds.Log("RewardedVideoAd ADMOB Rewarded");
               IsWatchSuccess = true;
               if (Application.platform == RuntimePlatform.Android)
               {
                    if (RewardedVideoCallbacks.Completed != null)
                    {
                         EventManager.InvokeNextFrame(RewardedVideoCallbacks.Completed);
                    }
               }
          }

          private void OnRewardBasedVideoLoaded()
          {
               DebugAds.Log("RewardedVideoAd ADMOB Load Success");
               RewardedVideoCallbacks.LoadedSuccess?.Invoke();
               if (!UseSequentialRewarded)
                    m_AdmobAdSetup.RewardedAdUnitID.Refresh();
          }

          private void OnRewardBasedVideoFailedToLoad()
          {
               DebugAds.Log("RewardedVideoAd ADMOB Load Fail");
               RewardedVideoCallbacks.LoadedFail?.Invoke();
               if (!UseSequentialRewarded)
                    m_AdmobAdSetup.RewardedAdUnitID.ChangeID();
          }

          public void OnRewardedAdFailedToShow(AdError args)
          {
               DebugAds.Log("RewardedVideoAd ADMOB Show Fail " + (args != null ? args.GetMessage() : "unknown"));
               RewardedVideoCallbacks.DisplayedFailed?.Invoke();
          }

          private void OnRewardBasedVideoOpened()
          {
               DebugAds.Log("Opened video success");
          }

          public void DestroyRewardedAd()
          {
               if (RewardVideoAds != null)
               {
                    DebugAds.Log("Destroying rewarded ad.");
                    RewardVideoAds.Destroy();
                    RewardVideoAds = null;
               }
          }


          public string GetRewardedAdID()
          {
               return m_AdmobAdSetup.RewardedAdUnitID.ID;
          }

          #endregion

          #region App Open Ads

          public override void InitAppOpenAds(UnityAction adLoadedCallback, UnityAction adLoadFailedCallback,
               UnityAction adClosedCallback,
               UnityAction adDisplayedCallback, UnityAction adFailedToDisplayCallback)
          {
               DebugAds.Log(("Init Admob App Open Ads"));
               base.InitAppOpenAds(adLoadedCallback, adLoadFailedCallback, adClosedCallback, adDisplayedCallback,
                    adFailedToDisplayCallback);
               RequestAppOpenAds();
          }

          public override void RequestAppOpenAds()
          {
               base.RequestAppOpenAds();
               DebugAds.Log("Request Admob App Open Ads");
               if (AppOpenAd != null)
               {
                    AppOpenAd.Destroy();
                    AppOpenAd = null;
               }

               AdRequest request = new AdRequest();

               // Load an app open ad for portrait orientation
               AppOpenAd.Load(m_AdmobAdSetup.AppOpenAdUnitID.ID, request, ((appOpenAd, error) =>
               {
                    if (error != null)
                    {
                         // Handle the error.
                         OnAppOpenAdFailedToLoad(error);
                         return;
                    }

                    OnAppOpenAdLoadedSuccess(appOpenAd);
               }));
          }

          public override void ShowAppOpenAds()
          {
               base.ShowAppOpenAds();
               if (AppOpenAd != null && AppOpenAd.CanShowAd())
               {
                    AppOpenAd.Show();
               }
          }

          private void RegisterAppOpenAdEventHandlers(AppOpenAd ad)
          {
               ad.OnAdFullScreenContentClosed += OnAppOpenAdDidDismissFullScreenContent;
               ad.OnAdFullScreenContentFailed += OnAppOpenAdFailedToPresentFullScreenContent;
               ad.OnAdFullScreenContentOpened += OnAppOpenAdDidPresentFullScreenContent;
               ad.OnAdImpressionRecorded += OnAppOpenAdDidRecordImpression;
               ad.OnAdPaid += OnAppOpenAppPaidEvent;
          }

          public override bool IsAppOpenAdsLoaded()
          {
               return AppOpenAd != null && AppOpenAd.CanShowAd();
          }


          #region App Open Ads Events

          private void OnAppOpenAdLoadedSuccess(AppOpenAd appOpenAd)
          {
               DebugAds.Log("Admob AppOpenAds Loaded");
               // App open ad is loaded.
               AppOpenAd = appOpenAd;
               RegisterAppOpenAdEventHandlers(appOpenAd);
               AppOpenAdCallbacks.LoadedSuccess?.Invoke();
          }

          private void OnAppOpenAdFailedToLoad(LoadAdError error)
          {
               DebugAds.LogFormat("Admob AppOpenAd Failed to load the ad. (reason: {0})", error.GetMessage());
               AppOpenAdCallbacks.LoadedFail?.Invoke();
               m_AdmobAdSetup.AppOpenAdUnitID.ChangeID();
          }

          private void OnAppOpenAdDidDismissFullScreenContent()
          {
               DebugAds.Log("Admob AppOpenAds Dismissed");
               AppOpenAd = null;
               AppOpenAdCallbacks.Closed?.Invoke(true);
          }

          private void OnAppOpenAdFailedToPresentFullScreenContent(AdError args)
          {
               DebugAds.LogFormat("Admob AppOpenAd Failed to present the ad (reason: {0})", args.GetMessage());
               AppOpenAd = null;
               AppOpenAdCallbacks.DisplayedFail?.Invoke();
          }

          private void OnAppOpenAdDidPresentFullScreenContent()
          {
               DebugAds.Log("Admob AppOpenAds opened");
               AppOpenAdCallbacks.Displayed?.Invoke();
          }

          private void OnAppOpenAdDidRecordImpression()
          {
               DebugAds.Log("Admob AppOpenAds Recorded Impression");
          }

          private void OnAppOpenAppPaidEvent(AdValue adValue)
          {
               DebugAds.Log("Admob AppOpenAds Paid");
               HandleAdPaidEvent("app_open_ad", adValue, AppOpenAd.GetResponseInfo());
          }

          #endregion

          #endregion

          private void HandleAdPaidEvent(string adFormat, AdValue adValue, ResponseInfo responseInfo)
          {
               string adSourceInstanceId = "";
               string adSourceInstanceName = "";
               string adSourceName = "";
               string adapterClassName = "";
               string adSourceId = "";

               AdapterResponseInfo loadedAdapterResponseInfo = responseInfo?.GetLoadedAdapterResponseInfo();
               if (loadedAdapterResponseInfo != null)
               {
                    try
                    {
                         adSourceInstanceId = loadedAdapterResponseInfo.AdSourceInstanceId;
                    }
                    catch (Exception)
                    {
                         // ignored
                    }

                    adSourceInstanceName = loadedAdapterResponseInfo.AdSourceInstanceName;
                    adSourceName = loadedAdapterResponseInfo.AdSourceName;
                    adapterClassName = loadedAdapterResponseInfo.AdapterClassName;
                    adSourceId = loadedAdapterResponseInfo.AdSourceId;
               }

               DebugAds.Log("Admob Paid AdSourceId: " + adSourceId + " AdSourceInstanceId: " + adSourceInstanceId +
                            " AdSourceInstanceName: " + adSourceInstanceName + " AdSourceName: " + adSourceName +
                            " AdapterClassName: " + adapterClassName);

               double revenue = (double)adValue.Value / 1000000;
               ImpressionData impression = new ImpressionData
               {
                    ad_mediation = AdsMediationType.ADMOB,
                    ad_source = adSourceName,
                    ad_sourceID = adSourceId,
                    ad_unit_name = adSourceInstanceId,
                    ad_format = adFormat.ToUpper(),
                    ad_currency = "USD",
                    ad_revenue = revenue
               };
               AdRevenuePaidCallback?.Invoke(impression);
          }

          private void OnApplicationQuit()
          {
               _interstitialTierLoader?.Destroy();
               _rewardedTierLoader?.Destroy();
               InterstitialAds?.Destroy();
          }

          public override AdsMediationType GetAdsMediationType()
          {
               return AdsMediationType.ADMOB;
          }

          public override bool IsActiveAdsType(AdsType adsType)
          {
               if (!isActive) return false;
               return adsType switch
               {
                    AdsType.BANNER => m_AdmobAdSetup.BannerAdUnitID.IsActive(),
                    AdsType.INTERSTITIAL => m_AdmobAdSetup.InterstitialAdUnitID.IsActive(),
                    AdsType.REWARDED => m_AdmobAdSetup.RewardedAdUnitID.IsActive(),
                    AdsType.APP_OPEN => m_AdmobAdSetup.AppOpenAdUnitID.IsActive(),
                    _ => false
               };
          }
#else
          public override void Init()
          {
               base.Init();
               Status = MediationStatus.FailedToInit;
          }

          public void ShowConsentFormAgain() { }

          public override bool IsActiveAdsType(AdsType adsType) => false;

          public override AdsMediationType GetAdsMediationType() => AdsMediationType.ADMOB;

          public void ConfigureRewardedInterstitial(AdmobAdSetup setup, string androidUnitId, string iosUnitId) { }
          public void LoadRewardedInterstitial() { }
          public bool IsRewardedInterstitialLoaded() => false;
          public void ShowRewardedInterstitial(UnityAction rewardCallback, UnityAction<bool> closedCallback = null,
               UnityAction failedCallback = null)
          {
               failedCallback?.Invoke();
               closedCallback?.Invoke(false);
          }
#endif
     }
}