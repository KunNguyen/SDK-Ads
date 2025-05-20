using System;
using System.Collections;
using System.Collections.Generic;
using ABIMaxSDKAds.Scripts;
using ABIMaxSDKAds.Scripts.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SDK
{
#if UNITY_AD_ADMOB
     using GoogleMobileAds.Api;
     using GoogleMobileAds.Ump.Api;

     public class AdmobMediationController : AdsMediationController
     {
          public AdmobAdSetup m_AdmobAdSetup;

          private InterstitialAd m_InterstitialAds;
          private RewardedAd m_RewardVideoAds;
          private BannerView m_MRECAds;
          private AppOpenAd m_AppOpenAd;
          private bool m_IsWatchSuccess = false;

          public override void Init()
          {
               if (IsInited) return;
               base.Init();
               InitAdmob();
          }

          #region Consent

          private void InitConsent()
          {
               ConsentDebugSettings debugSettings = new ConsentDebugSettings
               {
                    DebugGeography = DebugGeography.EEA,
                    TestDeviceHashedIds =
                         new List<string>
                         {
                              "8EC8C174AE81E71DF002C15B0B8458D9"
                         }
               };
               ConsentRequestParameters request = new ConsentRequestParameters
               {
                    ConsentDebugSettings = debugSettings,
               };
               ConsentInformation.Update(request, OnConsentInfoUpdated);
          }

          private void OnConsentInfoUpdated(FormError consentError)
          {
               if (consentError != null)
               {
                    // Handle the error.
                    DebugAds.LogError(consentError.Message);
                    return;
               }

               // If the error is null, the consent information state was updated.
               // You are now ready to check if a form is available.
               ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
               {
                    if (formError != null)
                    {
                         // Consent gathering failed.
                         DebugAds.LogError(consentError.Message);
                         return;
                    }

                    // Consent has been gathered.
                    if (ConsentInformation.CanRequestAds())
                    {
                         // Initialize the Mobile Ads SDK.
                         InitAdmob();
                    }
               });
          }

          private void InitAdmob()
          {
               MobileAds.Initialize((initStatus) =>
               {
                    Dictionary<string, AdapterStatus> map = initStatus.getAdapterStatusMap();
                    foreach (KeyValuePair<string, AdapterStatus> keyValuePair in map)
                    {
                         string className = keyValuePair.Key;
                         AdapterStatus status = keyValuePair.Value;
                         switch (status.InitializationState)
                         {
                              case AdapterState.NotReady:
                                   // The adapter initialization did not complete.
                                   print("Adapter: " + className + " not ready.");
                                   break;
                              case AdapterState.Ready:
                                   // The adapter was successfully initialized.
                                   print("Adapter: " + className + " is initialized.");
                                   break;
                         }
                    }

                    AdsManager.Instance.InitAds(AdsMediationType.ADMOB);
               });
               RequestConfiguration requestConfiguration = new RequestConfiguration();
               requestConfiguration.TestDeviceIds.Add("F0DE51766DB7C0740DEF1633ACCB3755");
               requestConfiguration.TestDeviceIds.Add("4849D928DCE8B9A471CE3BAB5E57E08A");
               MobileAds.SetRequestConfiguration(requestConfiguration);
          }

          #endregion

          #region Banner Ads

          private BannerView BannerViewAds { get; set; }
          public AdPosition m_BannerPosition;
          public bool IsBannerShowingOnStart = false;
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
               BannerViewAds ??= CreateBannerView();
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
               return BannerViewAds != null;
          }

          private void OnAdBannerLoaded(BannerView bannerView)
          {
               DebugAds.Log("HandleAdLoaded event received");
               m_AdmobAdSetup.BannerAdUnitID.Refresh();
               BannerCallbacks.LoadedSuccess?.Invoke();
          }

          private void OnAdBannerFailedToLoad(LoadAdError args)
          {
               DebugAds.Log("AdmobBanner Fail: " + args.GetMessage());
               m_AdmobAdSetup.BannerAdUnitID.ChangeID();
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

          #region Collapsible Banner

          private BannerView CollapsibleBanner { get; set; }
          public AdPosition m_CollapsibleBannerPosition;
          public bool IsCollapsibleBannerShowingOnStart = false;

          public override void InitCollapsibleBannerAds(UnityAction loadedSuccessCallback,
               UnityAction loadedFailCallback,
               UnityAction collapsedCallback, UnityAction expandedCallback,
               UnityAction destroyedCallback, UnityAction hideCallback)
          {
               base.InitCollapsibleBannerAds(loadedSuccessCallback, loadedFailCallback,
                    collapsedCallback, expandedCallback, destroyedCallback,
                    hideCallback);
               DebugAds.Log("Init Admob Collapsible Banner");
          }

          private BannerView CreateCollapsibleBannerView()
          {
               DebugAds.Log("Creating Collapsible Banner view");
               string adUnitId = GetCollapsibleBannerID();
               // Create a 320x50 banner at top of the screen
               AdSize adaptiveSize =
                    AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
               BannerView bannerView = new BannerView(adUnitId, adaptiveSize, m_CollapsibleBannerPosition);
               RegisterCollapsibleBannerEvents(bannerView);
               return bannerView;
          }

          private void LoadCollapsibleBannerAds(BannerView collapsibleBannerView)
          {
               DebugAds.Log("Call Load Collapsible Banner Ads");
               AdRequest adRequest = new AdRequest();
               adRequest.Extras.Add("collapsible", "bottom");
               collapsibleBannerView?.LoadAd(adRequest);
          }

          public override void RefreshCollapsibleBannerAds()
          {
               DebugAds.Log("Call Refresh Collapsible Banner Ads");
               DestroyCollapsibleBannerAds();
               CollapsibleBanner = CreateCollapsibleBannerView();
               AdRequest adRequest = new AdRequest();
               adRequest.Extras.Add("collapsible_request_id", UUID.Generate());

               CollapsibleBanner?.LoadAd(adRequest);
          }

          public override void RequestCollapsibleBannerAds(bool isOpenOnStart)
          {
               base.RequestCollapsibleBannerAds(isOpenOnStart);
               CollapsibleBanner = CreateCollapsibleBannerView();
               LoadCollapsibleBannerAds(CollapsibleBanner);
          }

          private void RegisterCollapsibleBannerEvents(BannerView bannerView)
          {
               bannerView.OnBannerAdLoaded += () => { OnAdCollapsibleBannerLoaded(bannerView); };
               bannerView.OnBannerAdLoadFailed += OnAdCollapsibleBannerFailedToLoad;
               bannerView.OnAdFullScreenContentOpened += () => { OnAdCollapsibleBannerOpened(bannerView); };
               bannerView.OnAdFullScreenContentClosed += OnAdCollapsibleBannerClosed;
               bannerView.OnAdPaid += OnAdCollapsibleBannerPaid;
          }

          public override void ShowCollapsibleBannerAds()
          {
               base.ShowCollapsibleBannerAds();
               DestroyCollapsibleBannerAds();

               if (CollapsibleBanner != null)
               {
                    DebugAds.Log("Start show collapsible banner ads");
                    CollapsibleBanner.Show();
               }
               else
               {
                    DebugAds.Log("Collapsible Banner is not loaded yet");
                    RequestCollapsibleBannerAds(true);
               }
          }

          public override void HideCollapsibleBannerAds()
          {
               base.HideCollapsibleBannerAds();
               CollapsibleBanner?.Hide();
               CollapsibleCallbacks.Hided?.Invoke();
          }

          public override bool IsCollapsibleBannerLoaded()
          {
               return CollapsibleBanner != null;
          }

          private void OnAdCollapsibleBannerLoaded(BannerView bannerView)
          {
               DebugAds.Log("Admob Collapsible Banner Loaded");
               m_AdmobAdSetup.CollapsibleBannerAdUnitID.Refresh();
               CollapsibleCallbacks.LoadedSuccess?.Invoke();
               if (IsCollapsibleBannerShowingOnStart)
               {
                    IsCollapsibleBannerShowingOnStart = false;
                    ShowCollapsibleBannerAds();
               }
          }

          private void OnAdCollapsibleBannerFailedToLoad(LoadAdError args)
          {
               DebugAds.Log("Admob Collapsible Banner Fail: " + args.GetMessage());
               m_AdmobAdSetup.CollapsibleBannerAdUnitID.ChangeID();
               CollapsibleCallbacks.LoadedFail?.Invoke();
          }

          private void ReloadCollapsibleBannerAds()
          {
               CollapsibleBanner = CreateCollapsibleBannerView();
               LoadCollapsibleBannerAds(CollapsibleBanner);
          }

          private void OnAdCollapsibleBannerOpened(BannerView bannerView)
          {
               DebugAds.Log("Admob Collapsible Banner Opened");
               CollapsibleCallbacks.Displayed?.Invoke();
          }

          private void OnAdCollapsibleBannerClosed()
          {
               DebugAds.Log("Admob Collapsible Banner Closed");
               CollapsibleCallbacks.Closed?.Invoke(true);
          }

          private void OnAdCollapsibleBannerPaid(AdValue adValue)
          {
               DebugAds.Log("Admob Collapsible Banner Paid");
               HandleAdPaidEvent("collapsible", adValue, CollapsibleBanner.GetResponseInfo());
          }

          /// <summary>
          /// Destroys the ad.
          /// </summary>
          public override void DestroyCollapsibleBannerAds()
          {
               base.DestroyCollapsibleBannerAds();
               if (CollapsibleBanner != null)
               {
                    DebugAds.Log("Destroying banner ad.");
                    CollapsibleBanner.Destroy();
                    CollapsibleBanner = null;
                    CollapsibleCallbacks.Destroyed?.Invoke();
               }
               else
               {
                    DebugAds.Log("Don't have any banner to destroy.");
               }
          }

          public string GetCollapsibleBannerID()
          {
               return m_AdmobAdSetup.CollapsibleBannerAdUnitID.ID;
          }

          #endregion

          #region Interstitial

          public override void InitInterstitialAd(UnityAction adClosedCallback, UnityAction adLoadSuccessCallback,
               UnityAction adLoadFailedCallback, UnityAction adShowSuccessCallback, UnityAction adShowFailCallback)
          {
               base.InitInterstitialAd(adClosedCallback, adLoadSuccessCallback, adLoadFailedCallback,
                    adShowSuccessCallback, adShowFailCallback);
               DebugAds.Log("Init Admob Interstitial");
               RequestInterstitialAd();
          }

          public override void RequestInterstitialAd()
          {
               base.RequestInterstitialAd();
               DebugAds.Log("Request interstitial ads");

               if (m_InterstitialAds != null)
               {
                    m_InterstitialAds.Destroy();
                    m_InterstitialAds = null;
               }

               AdRequest adRequest = new AdRequest();
               adRequest.Keywords.Add("unity-admob-sample");

               string adUnitId = GetInterstitialAdUnit();
               InterstitialAd.Load(adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
               {
                    if (error != null || ad == null)
                    {
                         DebugAds.LogError("interstitial ad failed to load an ad " + "with error : " + error);
                         OnAdInterstitialFailedToLoad();
                         return;
                    }

                    DebugAds.Log("Interstitial ad loaded with response : " + ad.GetResponseInfo());
                    m_InterstitialAds = ad;
                    RegisterInterstitialAd(ad);
                    OnAdInterstitialSuccessToLoad();
               });
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
               return m_InterstitialAds != null && m_InterstitialAds.CanShowAd();
          }

          public override void ShowInterstitialAd()
          {
               base.ShowInterstitialAd();
               if (m_InterstitialAds.CanShowAd())
               {
                    m_InterstitialAds.Show();
               }
          }

          private void OnCloseInterstitialAd()
          {
               DebugAds.Log("Close Interstitial");
               InterstitialCallbacks.Closed?.Invoke(true);
          }

          private void OnAdInterstitialSuccessToLoad()
          {
               DebugAds.Log("Load Interstitial success");
               InterstitialCallbacks.LoadedSuccess?.Invoke();
               m_AdmobAdSetup.InterstitialAdUnitID.Refresh();
          }

          private void OnAdInterstitialFailedToLoad()
          {
               DebugAds.Log("Load Interstitial failed Admob");
               InterstitialCallbacks.LoadedFail?.Invoke();
               m_AdmobAdSetup.InterstitialAdUnitID.ChangeID();
          }

          private void OnAdInterstitialOpening()
          {
               DebugAds.Log("Interstitial ad opened.");
               InterstitialCallbacks.Displayed?.Invoke();
          }

          private void OnAdInterstitialFailToShow(AdError e)
          {
               DebugAds.Log("Interstitial ad failed to show with error: " + e.GetMessage());
               InterstitialCallbacks.DisplayedFail?.Invoke();
          }

          private void OnAdInterstitialPaid(AdValue adValue)
          {
               HandleAdPaidEvent("interstitial", adValue, m_InterstitialAds.GetResponseInfo());
          }

          public void DestroyInterstitialAd()
          {
               if (m_InterstitialAds != null)
               {
                    DebugAds.Log("Destroying interstitial ad.");
                    m_InterstitialAds.Destroy();
                    m_InterstitialAds = null;
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
               if (m_RewardVideoAds != null)
               {
                    DestroyRewardedAd();
               }

               string adUnitId = GetRewardedAdID();
               DebugAds.Log("RewardedVideoAd ADMOB Reload ID " + adUnitId);
               if (string.IsNullOrEmpty(adUnitId))
               {
                    RewardedVideoCallbacks.LoadedSuccess?.Invoke();
                    m_AdmobAdSetup.RewardedAdUnitID.ChangeID();
               }

               if (m_RewardVideoAds != null && m_RewardVideoAds.CanShowAd()) return;
               var adRequest = new AdRequest();

               RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
               {
                    if (error != null || ad == null)
                    {
                         DebugAds.LogError("Rewarded ad failed to load an ad with error : " + error);
                         OnRewardBasedVideoFailedToLoad();
                         return;
                    }

                    m_RewardVideoAds = ad;
                    RegisterRewardAdEvent(ad);
                    OnRewardBasedVideoLoaded();
               });
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
               if (IsRewardVideoLoaded())
               {
                    DebugAds.Log("RewardedVideoAd ADMOB Show");
                    m_IsWatchSuccess = false;
                    m_RewardVideoAds.Show((Reward reward) => { OnRewardBasedVideoRewarded(); });
               }
          }

          public override bool IsRewardVideoLoaded()
          {
#if UNITY_EDITOR
               return false;
#endif
               if (m_RewardVideoAds != null)
               {
                    return m_RewardVideoAds.CanShowAd();
               }

               return false;
          }

          private void OnRewardBasedVideoClosed()
          {
               DebugAds.Log("RewardedVideoAd ADMOB Closed");
               if (Application.platform == RuntimePlatform.IPhonePlayer)
               {
                    if (m_IsWatchSuccess)
                    {
                         if (RewardedVideoCallbacks.Completed != null)
                         {
                              EventManager.AddEventNextFrame(RewardedVideoCallbacks.Completed);
                         }
                    }
               }

               if (RewardedVideoCallbacks.Closed != null)
               {
                    EventManager.AddEventNextFrame(() => { RewardedVideoCallbacks.Closed.Invoke(m_IsWatchSuccess); });
               }
          }

          private void OnRewardBasedVideoRewarded()
          {
               DebugAds.Log("RewardedVideoAd ADMOB Rewarded");
               m_IsWatchSuccess = true;
               if (Application.platform == RuntimePlatform.Android)
               {
                    if (RewardedVideoCallbacks.Completed != null)
                    {
                         EventManager.AddEventNextFrame(RewardedVideoCallbacks.Completed);
                    }
               }
          }

          private void OnRewardBasedVideoLoaded()
          {
               DebugAds.Log("RewardedVideoAd ADMOB Load Success");
               RewardedVideoCallbacks.LoadedSuccess?.Invoke();
               m_AdmobAdSetup.RewardedAdUnitID.Refresh();
          }

          private void OnRewardBasedVideoFailedToLoad()
          {
               DebugAds.Log("RewardedVideoAd ADMOB Load Fail");
               RewardedVideoCallbacks.LoadedFail?.Invoke();
               m_AdmobAdSetup.RewardedAdUnitID.ChangeID();
          }

          public void OnRewardedAdFailedToShow(AdError args)
          {
               DebugAds.Log("RewardedVideoAd ADMOB Show Fail " + args.GetMessage());
               RewardedVideoCallbacks.DisplayedFailed?.Invoke();
          }

          private void OnRewardBasedVideoOpened()
          {
               DebugAds.Log("Opened video success");
          }

          public void DestroyRewardedAd()
          {
               if (m_RewardVideoAds != null)
               {
                    DebugAds.Log("Destroying rewarded ad.");
                    m_RewardVideoAds.Destroy();
                    m_RewardVideoAds = null;
               }
          }

          private void OnAdRewardedAdPaid(AdValue adValue)
          {
               HandleAdPaidEvent("rewarded", adValue, m_RewardVideoAds.GetResponseInfo());
          }

          public string GetRewardedAdID()
          {
               return m_AdmobAdSetup.RewardedAdUnitID.ID;
          }

          #endregion

          #region MREC Ads

          public AdPosition m_MRecPosition { get; set; }

          public override void InitRMecAds(UnityAction adLoadedCallback, UnityAction adLoadFailedCallback,
               UnityAction adClickedCallback,
               UnityAction adExpandedCallback, UnityAction adCollapsedCallback)
          {
               base.InitRMecAds(adLoadedCallback, adLoadFailedCallback, adClickedCallback, adExpandedCallback,
                    adCollapsedCallback);
               DebugAds.Log("Init Admob MREC");
               RequestMRecAds();
               HideMRecAds();
          }

          public void CreateMRecAdsView()
          {
               DebugAds.Log("Creating MREC view");
               if (m_MRECAds != null)
               {
                    DestroyMRecAds();
               }

               string adUnitId = GetMRECAdID();
               // Create a 320x50 banner at top of the screen
               m_MRECAds = new BannerView(adUnitId, AdSize.MediumRectangle, m_MRecPosition);
               RegisterMRECAdsEvents(m_MRECAds);
          }

          public override void RequestMRecAds()
          {
               base.RequestMRecAds();
               if (m_MRECAds == null)
               {
                    CreateMRecAdsView();
               }

               AdRequest adRequest = new AdRequest();
               adRequest.Keywords.Add("unity-admob-sample");

               // Load the banner with the request.
               m_MRECAds.LoadAd(adRequest);
          }

          private void RegisterMRECAdsEvents(BannerView mrecBannerView)
          {
               mrecBannerView.OnBannerAdLoaded += MRecAdsOnOnBannerAdLoaded;
               mrecBannerView.OnBannerAdLoadFailed += MRecAdsOnOnBannerAdLoadFailed;
               mrecBannerView.OnAdFullScreenContentOpened += MRecAdsOnOnAdFullScreenContentOpened;
               mrecBannerView.OnAdFullScreenContentClosed += MRecAdsOnOnAdFullScreenContentClosed;
               mrecBannerView.OnAdPaid += MRecAdsOnOnAdPaid;
          }

          public override void ShowMRecAds()
          {
               DebugAds.Log("Show Admob MREC Ads");
               base.ShowMRecAds();
               m_MRECAds.Show();
          }

          public override void HideMRecAds()
          {
               DebugAds.Log("Hide Admob MREC Ads");
               base.HideMRecAds();
               m_MRECAds.Hide();
          }

          private void MRecAdsOnOnBannerAdLoaded()
          {
               DebugAds.Log("Admob MRec Ads Loaded");
               MRecCallbacks.LoadedSuccess?.Invoke();
          }
          private void MRecAdsOnOnBannerAdLoadFailed(LoadAdError obj)
          {
               DebugAds.Log("Admob MRec Ads Failed to load the ad. (reason: {0})" + obj.GetMessage());
               MRecCallbacks.LoadedFail?.Invoke();
          }
          private void MRecAdsOnOnAdPaid(AdValue adValue)
          {
               DebugAds.Log("Admob MRec Ads Paid");
               HandleAdPaidEvent("mrec", adValue, m_MRECAds.GetResponseInfo());
          }

          private void MRecAdsOnOnAdFullScreenContentClosed()
          {
               DebugAds.Log("Admob MRec Ads Closed");
               MRecCallbacks.Closed?.Invoke(true);
          }

          private void MRecAdsOnOnAdFullScreenContentOpened()
          {
               DebugAds.Log("Admob MRec Ads Opened");
               MRecCallbacks.Displayed?.Invoke();
          }

          private void DestroyMRecAds()
          {
               if (m_MRECAds != null)
               {
                    DebugAds.Log("Destroying MREC Ad.");
                    m_MRECAds.Destroy();
                    m_MRECAds = null;
               }
          }

          public override bool IsMRecLoaded()
          {
               return m_MRECAds != null;
          }

          private string GetMRECAdID()
          {
               return m_AdmobAdSetup.MrecAdUnitID.ID;
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
               if (m_AppOpenAd != null)
               {
                    m_AppOpenAd.Destroy();
                    m_AppOpenAd = null;
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
               if (m_AppOpenAd != null && m_AppOpenAd.CanShowAd())
               {
                    m_AppOpenAd.Show();
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
               return m_AppOpenAd != null && m_AppOpenAd.CanShowAd();
          }


          #region App Open Ads Events

          private void OnAppOpenAdLoadedSuccess(AppOpenAd appOpenAd)
          {
               DebugAds.Log("Admob AppOpenAds Loaded");
               // App open ad is loaded.
               m_AppOpenAd = appOpenAd;
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
               m_AppOpenAd = null;
               AppOpenAdCallbacks.Closed?.Invoke(true);
          }

          private void OnAppOpenAdFailedToPresentFullScreenContent(AdError args)
          {
               DebugAds.LogFormat("Admob AppOpenAd Failed to present the ad (reason: {0})", args.GetMessage());
               m_AppOpenAd = null;
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
               HandleAdPaidEvent("app_open_ad", adValue, m_AppOpenAd.GetResponseInfo());
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
                    catch (Exception exception)
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
                    ad_unit_name = adSourceInstanceId,
                    ad_format = adFormat.ToUpper(),
                    ad_currency = "USD",
                    ad_revenue = revenue
               };
               AdRevenuePaidCallback?.Invoke(impression);
          }

          private void OnApplicationQuit()
          {
               m_InterstitialAds?.Destroy();
          }

          public override AdsMediationType GetAdsMediationType()
          {
               return AdsMediationType.ADMOB;
          }

          public override bool IsActiveAdsType(AdsType adsType)
          {
               if (!m_IsActive) return false;
               return adsType switch
               {
                    AdsType.BANNER => m_AdmobAdSetup.BannerAdUnitID.IsActive(),
                    AdsType.INTERSTITIAL => m_AdmobAdSetup.InterstitialAdUnitID.IsActive(),
                    AdsType.REWARDED => m_AdmobAdSetup.RewardedAdUnitID.IsActive(),
                    AdsType.MREC => m_AdmobAdSetup.MrecAdUnitID.IsActive(),
                    AdsType.APP_OPEN => m_AdmobAdSetup.AppOpenAdUnitID.IsActive(),
                    _ => false
               };
          }
     }
#endif
}