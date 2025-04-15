using System;
using System.Collections;
using System.Collections.Generic;
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
                Debug.LogError(consentError);
                return;
            }
            // If the error is null, the consent information state was updated.
            // You are now ready to check if a form is available.
            ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
            {
                if (formError != null)
                {
                    // Consent gathering failed.
                    Debug.LogError(consentError);
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
                            AdsManager.Instance.InitAds(AdsMediationType.ADMOB);
                            break;
                    }
                }
            });
            RequestConfiguration requestConfiguration = new RequestConfiguration();
            requestConfiguration.TestDeviceIds.Add("F0DE51766DB7C0740DEF1633ACCB3755");
            MobileAds.SetRequestConfiguration(requestConfiguration);
        }
        #endregion

        #region Banner Ads
        private BannerView m_BannerViewAds;
        public AdPosition m_BannerPosition;
        public bool IsBannerShowingOnStart = false;
        public override void InitBannerAds(
            UnityAction bannerLoadedCallback, UnityAction bannerAdLoadedFailCallback,
            UnityAction bannerAdsCollapsedCallback, UnityAction bannerAdsExpandedCallback, 
            UnityAction bannerAdsDisplayed = null, UnityAction bannerAdsDisplayedFailedCallback = null,
            UnityAction bannerAdsClickedCallback = null)
        {
            base.InitBannerAds(
                bannerLoadedCallback, bannerAdLoadedFailCallback, bannerAdsCollapsedCallback, 
                bannerAdsExpandedCallback, bannerAdsDisplayed, bannerAdsDisplayedFailedCallback, bannerAdsClickedCallback);
            Debug.Log("Init Admob Banner");
            RequestBannerAds();
            if (!IsBannerShowingOnStart)
            {
                m_BannerViewAds.Hide();
            }
            else
            {
                m_BannerViewAds.Show();
            }
        }

        private BannerView CreateBannerView()
        {
            Debug.Log("Creating banner view");
            string adUnitId = GetBannerID();
            // Create a 320x50 banner at top of the screen
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
            m_BannerViewAds ??= CreateBannerView();
            LoadBannerAds(m_BannerViewAds);
        }

        private void RegisterBannerEvents(BannerView bannerView)
        {
            bannerView.OnBannerAdLoaded += () =>
            {
                OnAdBannerLoaded(bannerView);
            };
            bannerView.OnBannerAdLoadFailed += OnAdBannerFailedToLoad;
            bannerView.OnAdFullScreenContentOpened += () =>
            {
                OnAdBannerOpened(bannerView);
            };
            bannerView.OnAdFullScreenContentClosed += OnAdBannerClosed;
            bannerView.OnAdPaid += (adValue) =>
            {
                OnAdBannerPaid(bannerView, adValue);
            };
        }

        public override void ShowBannerAds()
        {
            base.ShowBannerAds();
            
            if (m_BannerViewAds != null)
            {
                Debug.Log("Start Show banner ads");
                m_BannerViewAds.Show();
            }
            else
            {
                Debug.Log("Banner is not loaded yet");
                RequestBannerAds();
                m_BannerViewAds?.Show();
            }
        }
        public override void HideBannerAds()
        {
            base.HideBannerAds();
            m_BannerViewAds?.Hide();
        }
        public override bool IsBannerLoaded()
        {
            return m_BannerViewAds != null;
        }
        private void OnAdBannerLoaded(BannerView bannerView)
        {
            Debug.Log("HandleAdLoaded event received");
            m_AdmobAdSetup.BannerAdUnitID.Refresh();
            m_BannerAdLoadedSuccessCallback?.Invoke();
        }

        private void OnAdBannerFailedToLoad(LoadAdError args)
        {
            Debug.Log("AdmobBanner Fail: " + args.GetMessage());
            m_AdmobAdSetup.BannerAdUnitID.ChangeID();
            m_BannerAdLoadedFailCallback?.Invoke();
            LoadBannerAds(CreateBannerView());
        }

        private void OnAdBannerOpened(BannerView bannerView)
        {
            Debug.Log("AdmobBanner Opened");
            m_BannerAdsExpandedCallback?.Invoke();
        }

        private void OnAdBannerClosed()
        {
            Debug.Log("AdmobBanner Closed");
            m_BannerAdsCollapsedCallback?.Invoke();
        }

        private void OnAdBannerPaid(BannerView bannerView, AdValue adValue)
        {
            double revenue = (double)adValue.Value / 1000000;
            string currencyCode = adValue.CurrencyCode;
            
            ResponseInfo responseInfo = bannerView.GetResponseInfo();
            
            AdapterResponseInfo loadedAdapterResponseInfo = responseInfo.GetLoadedAdapterResponseInfo();
            string adSourceId = loadedAdapterResponseInfo.AdSourceId;
            string adSourceInstanceId = loadedAdapterResponseInfo.AdSourceInstanceId;
            string adSourceInstanceName = loadedAdapterResponseInfo.AdSourceInstanceName;
            string adSourceName = loadedAdapterResponseInfo.AdSourceName;
            string adapterClassName = loadedAdapterResponseInfo.AdapterClassName;
            long latencyMillis = loadedAdapterResponseInfo.LatencyMillis;
            
            ImpressionData impression = new ImpressionData
            {
                ad_mediation = AdsMediationType.ADMOB,
                ad_source = adSourceName,
                ad_unit_name = adSourceInstanceName,
                ad_format = "Banner",
                ad_currency = "USD",
                ad_revenue = revenue
            };
        }

        /// <summary>
        /// Destroys the ad.
        /// </summary>
        public override void DestroyBannerAds()
        {
            base.DestroyBannerAds();
            if (m_BannerViewAds != null)
            {
                Debug.Log("Destroying banner ad.");
                m_BannerViewAds.Destroy();
                m_BannerViewAds = null;
            }
            else
            {
                Debug.Log("Don't have any banner to destroy.");
            }
        }

        private string GetBannerID()
        {
            return m_AdmobAdSetup.BannerAdUnitID.ID;
        }

        #endregion
        
        #region Collapsible Banner
        private BannerView m_CurrentCollapsibleBanner;
        public AdPosition m_CollapsibleBannerPosition;
        public bool IsCollapsibleBannerShowingOnStart = false;
        public override void InitCollapsibleBannerAds(UnityAction bannerLoadedCallback, UnityAction bannerAdLoadedFailCallback,
            UnityAction bannerAdsCollapsedCallback, UnityAction bannerAdsExpandedCallback, UnityAction bannerAdsDestroyedCallback, UnityAction bannerAdsHideCallback)
        {
            base.InitCollapsibleBannerAds(bannerLoadedCallback, bannerAdLoadedFailCallback, bannerAdsCollapsedCallback, bannerAdsExpandedCallback, bannerAdsDestroyedCallback, bannerAdsHideCallback);
            Debug.Log("Init Admob Collapsible Banner");
        }
        
        private BannerView CreateCollapsibleBannerView()
        {
            Debug.Log("Creating Collapsible Banner view");
            string adUnitId = GetCollapsibleBannerID();
            // Create a 320x50 banner at top of the screen
            AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
            BannerView bannerView = new BannerView(adUnitId, adaptiveSize, m_CollapsibleBannerPosition);
            RegisterCollapsibleBannerEvents(bannerView);
            return bannerView;
        }
        
        private void LoadCollapsibleBannerAds(BannerView bannerView)
        {
            Debug.Log("Call Load Collapsible Banner Ads");
            AdRequest adRequest = new AdRequest();
            adRequest.Extras.Add("collapsible",  "bottom");   
            bannerView?.LoadAd(adRequest);
        }
        public override void RefreshCollapsibleBannerAds()
        {
            Debug.Log("Call Refresh Collapsible Banner Ads");
            DestroyCollapsibleBannerAds();
            m_CurrentCollapsibleBanner = CreateCollapsibleBannerView();
            AdRequest adRequest = new AdRequest();
            adRequest.Extras.Add("collapsible_request_id", AdsManager.UUID.Generate());
            
            m_CurrentCollapsibleBanner?.LoadAd(adRequest);
        }
        public override void RequestCollapsibleBannerAds(bool isOpenOnStart)
        {
            base.RequestCollapsibleBannerAds(isOpenOnStart);
            m_CurrentCollapsibleBanner = CreateCollapsibleBannerView();
            LoadCollapsibleBannerAds(m_CurrentCollapsibleBanner);
        }
        private void RegisterCollapsibleBannerEvents(BannerView bannerView)
        {
            bannerView.OnBannerAdLoaded += () =>
            {
                OnAdCollapsibleBannerLoaded(bannerView);
            };
            bannerView.OnBannerAdLoadFailed += OnAdCollapsibleBannerFailedToLoad;
            bannerView.OnAdFullScreenContentOpened += () =>
            {
                OnAdCollapsibleBannerOpened(bannerView);
            };
            bannerView.OnAdFullScreenContentClosed += OnAdCollapsibleBannerClosed;
            bannerView.OnAdPaid += OnAdCollapsibleBannerPaid;
        }

        public override void ShowCollapsibleBannerAds()
        {
            base.ShowCollapsibleBannerAds();
            DestroyCollapsibleBannerAds();
            
            if (m_CurrentCollapsibleBanner != null)
            {
                Debug.Log("Start show collapsible banner ads");
                m_CurrentCollapsibleBanner.Show();
            }
            else
            {
                Debug.Log("Collapsible Banner is not loaded yet");
                RequestCollapsibleBannerAds(true);
            }
        }
        public override void HideCollapsibleBannerAds()
        {
            base.HideCollapsibleBannerAds();
            m_CurrentCollapsibleBanner?.Hide();
            m_CollapsibleBannerAdsHideCallback?.Invoke();
        }
        public override bool IsCollapsibleBannerLoaded()
        {
            return m_CurrentCollapsibleBanner != null;
        }
        private void OnAdCollapsibleBannerLoaded(BannerView bannerView)
        {
            Debug.Log("Admob Collapsible Banner Loaded");
            m_AdmobAdSetup.CollapsibleBannerAdUnitID.Refresh();
            m_CollapsibleBannerAdLoadedSuccessCallback?.Invoke();
            if (IsCollapsibleBannerShowingOnStart)
            {
                IsCollapsibleBannerShowingOnStart = false;
                ShowCollapsibleBannerAds();
            }
        }
        private void OnAdCollapsibleBannerFailedToLoad(LoadAdError args)
        {
            Debug.Log("Admob Collapsible Banner Fail: " + args.GetMessage());
            m_AdmobAdSetup.CollapsibleBannerAdUnitID.ChangeID();
            m_CollapsibleBannerAdLoadedFailCallback?.Invoke();
        }
        private void ReloadCollapsibleBannerAds()
        {
            m_CurrentCollapsibleBanner = CreateCollapsibleBannerView();
            LoadCollapsibleBannerAds(m_CurrentCollapsibleBanner);
        }
        private void OnAdCollapsibleBannerOpened(BannerView bannerView)
        {
            Debug.Log("Admob Collapsible Banner Opened");
            m_CollapsibleBannerAdsExpandedCallback?.Invoke();
        }
        private void OnAdCollapsibleBannerClosed()
        {
            Debug.Log("Admob Collapsible Banner Closed");
            m_CollapsibleBannerAdsCollapsedCallback?.Invoke();
        }
        private void OnAdCollapsibleBannerPaid(AdValue adValue)
        {
            
        }

        /// <summary>
        /// Destroys the ad.
        /// </summary>
        public override void DestroyCollapsibleBannerAds()
        {
            base.DestroyCollapsibleBannerAds();
            if (m_CurrentCollapsibleBanner != null)
            {
                Debug.Log("Destroying banner ad.");
                m_CurrentCollapsibleBanner.Destroy();
                m_CurrentCollapsibleBanner = null;
                m_CollapsibleBannerAdsDestroyedCallback?.Invoke();
            }
            else
            {
                Debug.Log("Don't have any banner to destroy.");
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
            Debug.Log("Init Admob Interstitial");
            RequestInterstitialAd();
        }

        public override void RequestInterstitialAd()
        {
            base.RequestInterstitialAd();
            Debug.Log("Request interstitial ads");

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
                    Debug.LogError("interstitial ad failed to load an ad " + "with error : " + error);
                    OnAdInterstitialFailedToLoad();
                    return;
                }

                Debug.Log("Interstitial ad loaded with response : " + ad.GetResponseInfo());
                m_InterstitialAds = ad;
                RegisterInterstititalAd(ad);
                OnAdInterstitialSuccessToLoad();
            });
        }

        private void RegisterInterstititalAd(InterstitialAd interstitialAd)
        {
            interstitialAd.OnAdFullScreenContentClosed += OnCloseInterstitialAd;
            interstitialAd.OnAdFullScreenContentOpened += OnAdInterstitialOpening;
            interstitialAd.OnAdFullScreenContentFailed += OnAdInterstitialFailToShow;
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
            m_InterstitialAdCloseCallback?.Invoke();
            Debug.Log("Close Interstitial");
        }

        private void OnAdInterstitialSuccessToLoad()
        {
            m_InterstitialAdLoadSuccessCallback?.Invoke();
            m_AdmobAdSetup.InterstitialAdUnitID.Refresh();
            Debug.Log("Load Interstitial success");
        }

        private void OnAdInterstitialFailedToLoad()
        {
            m_InterstitialAdLoadFailCallback?.Invoke();
            m_AdmobAdSetup.InterstitialAdUnitID.ChangeID();
            Debug.Log("Load Interstitial failed Admob");
        }

        private void OnAdInterstitialOpening()
        {
            m_InterstitialAdShowSuccessCallback?.Invoke();
        }

        private void OnAdInterstitialFailToShow(AdError e)
        {
            m_InterstitialAdShowFailCallback?.Invoke();
        }

        public void DestroyInterstitialAd()
        {
            if (m_InterstitialAds != null)
            {
                Debug.Log("Destroying interstitial ad.");
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

        public override void InitRewardVideoAd(UnityAction<bool> videoClosed, UnityAction videoLoadSuccess,
            UnityAction videoLoadFailed, UnityAction videoStart)
        {
            base.InitRewardVideoAd(videoClosed, videoLoadSuccess, videoLoadFailed, videoStart);
            Debug.Log("Init Reward Video");
        }

        public override void RequestRewardVideoAd()
        {
            base.RequestRewardVideoAd();
            if (m_RewardVideoAds != null)
            {
                DestroyRewardedAd();
            }

            string adUnitId = GetRewardedAdID();
            Debug.Log("RewardedVideoAd ADMOB Reloaded ID " + adUnitId);
            if (string.IsNullOrEmpty(adUnitId))
            {
                m_RewardedVideoLoadFailedCallback?.Invoke();
                m_AdmobAdSetup.RewardedAdUnitID.ChangeID();
            }

            if (m_RewardVideoAds != null && m_RewardVideoAds.CanShowAd()) return;

            var adRequest = new AdRequest();

            RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("Rewarded ad failed to load an ad with error : " + error);
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

        public override void ShowRewardVideoAd(UnityAction successCallback, UnityAction failedCallback)
        {
            base.ShowRewardVideoAd(successCallback, failedCallback);
            if (IsRewardVideoLoaded())
            {
                Debug.Log("RewardedVideoAd ADMOB Show");
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
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                if (m_IsWatchSuccess)
                {
                    if (m_RewardedVideoEarnSuccessCallback != null)
                    {
                        EventManager.AddEventNextFrame(m_RewardedVideoEarnSuccessCallback);
                    }
                }
            }

            if (m_RewardedVideoCloseCallback != null)
            {
                EventManager.AddEventNextFrame(() =>
                {
                  m_RewardedVideoCloseCallback.Invoke(m_IsWatchSuccess);  
                });
            }
        }

        private void OnRewardBasedVideoRewarded()
        {
            Debug.Log("RewardedVideoAd ADMOB Rewarded");
            m_IsWatchSuccess = true;
            if (Application.platform == RuntimePlatform.Android)
            {
                if (m_RewardedVideoEarnSuccessCallback != null)
                {
                    EventManager.AddEventNextFrame(m_RewardedVideoEarnSuccessCallback);
                }
            }
        }

        private void OnRewardBasedVideoLoaded()
        {
            Debug.Log("RewardedVideoAd ADMOB Load Success");
            m_RewardedVideoLoadSuccessCallback?.Invoke();
            m_AdmobAdSetup.RewardedAdUnitID.Refresh();
        }

        private void OnRewardBasedVideoFailedToLoad()
        {
            Debug.Log("RewardedVideoAd ADMOB Load Fail");
            m_RewardedVideoLoadFailedCallback?.Invoke();
            m_AdmobAdSetup.RewardedAdUnitID.ChangeID();
        }

        public void OnRewardedAdFailedToShow(AdError args)
        {
            Debug.Log("RewardedVideoAd ADMOB Show Fail " + args.GetMessage());
            m_RewardedVideoShowFailCallback?.Invoke();
        }

        private void OnRewardBasedVideoOpened()
        {
            Debug.Log("Opened video success");
        }

        public void DestroyRewardedAd()
        {
            if (m_RewardVideoAds != null)
            {
                Debug.Log("Destroying rewarded ad.");
                m_RewardVideoAds.Destroy();
                m_RewardVideoAds = null;
            }
        }

        private void OnAdRewardedAdPaid(AdValue adValue)
        {
        }

        public string GetRewardedAdID()
        {
            return m_AdmobAdSetup.RewardedAdUnitID.ID;
        }

        #endregion

        #region MREC Ads
        public AdPosition m_MRECPosition;
        public override void InitRMecAds(UnityAction adLoadedCallback, UnityAction adLoadFailedCallback,
            UnityAction adClickedCallback,
            UnityAction adExpandedCallback, UnityAction adCollapsedCallback)
        {
            base.InitRMecAds(adLoadedCallback, adLoadFailedCallback, adClickedCallback, adExpandedCallback, adCollapsedCallback);
            Debug.Log("Init Admob MREC");
            RequestMRecAds();
            HideMRecAds();
        }

        public void CreateMRECAdsView()
        {
            Debug.Log("Creating MREC view");
            if (m_MRECAds != null)
            {
                DestroyMRECAds();
            }

            string adUnitId = GetMRECAdID();
            // Create a 320x50 banner at top of the screen
            m_MRECAds = new BannerView(adUnitId, AdSize.MediumRectangle, m_MRECPosition);
            RegisterMRECAdsEvents(m_MRECAds);
        }

        public override void RequestMRecAds()
        {
            base.RequestMRecAds();
            if (m_MRECAds == null)
            {
                CreateMRECAdsView();
            }

            AdRequest adRequest = new AdRequest();
            adRequest.Keywords.Add("unity-admob-sample");

            // Load the banner with the request.
            m_MRECAds.LoadAd(adRequest);
        }

        private void RegisterMRECAdsEvents(BannerView bannerView)
        {
            bannerView.OnBannerAdLoaded += MRECAdsOnOnBannerAdLoaded;
            bannerView.OnBannerAdLoadFailed += MRECAdsOnOnBannerAdLoadFailed;
            bannerView.OnAdFullScreenContentOpened += MRECAdsOnOnAdFullScreenContentOpened;
            bannerView.OnAdFullScreenContentClosed += MRECAdsOnOnAdFullScreenContentClosed;
            bannerView.OnAdPaid += MRECAdsOnOnAdPaid;
        }

        public override void ShowMRecAds()
        {
            Debug.Log("Show Admob MREC Ads");
            base.ShowMRecAds();
            m_MRECAds.Show();
        }

        public override void HideMRecAds()
        {
            Debug.Log("Hide Admob MREC Ads");
            base.HideMRecAds();
            m_MRECAds.Hide();
        }

        private void MRECAdsOnOnBannerAdLoaded()
        {
            Debug.Log("Admob MREC Ads Loaded");
            m_MRecAdLoadedCallback?.Invoke();
        }

        private void MRECAdsOnOnAdPaid(AdValue adValue)
        {
            Debug.Log("Admob MREC Ads Paid");
            HandleAdPaidEvent("mrec", adValue, m_MRECAds.GetResponseInfo());
        }

        private void MRECAdsOnOnAdFullScreenContentClosed()
        {
            Debug.Log("Admob MREC Ads Closed");
            m_MRecAdCollapsedCallback?.Invoke();
        }

        private void MRECAdsOnOnAdFullScreenContentOpened()
        {
            Debug.Log("Admob MREC Ads Opened");
            m_MRecAdExpandedCallback?.Invoke();
        }

        private void MRECAdsOnOnBannerAdLoadFailed(LoadAdError obj)
        {
            Debug.Log("Admob MREC Ads Failed to load the ad. (reason: {0})" + obj.GetMessage());
            m_MRecAdLoadFailCallback?.Invoke();
        }

        private void DestroyMRECAds()
        {
            if (m_MRECAds != null)
            {
                Debug.Log("Destroying MREC Ad.");
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
            Debug.Log(("Init Admob App Open Ads"));
            base.InitAppOpenAds(adLoadedCallback, adLoadFailedCallback, adClosedCallback, adDisplayedCallback,
                adFailedToDisplayCallback);
            RequestAppOpenAds();
        }

        public override void RequestAppOpenAds()
        {
            base.RequestAppOpenAds();
            Debug.Log("Request Admob App Open Ads");
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
            Debug.Log("Admob AppOpenAds Loaded");
            // App open ad is loaded.
            m_AppOpenAd = appOpenAd;
            RegisterAppOpenAdEventHandlers(appOpenAd);
            m_AppOpenAdLoadedCallback?.Invoke();
        }

        private void OnAppOpenAdFailedToLoad(LoadAdError error)
        {
            Debug.LogFormat("Admob AppOpenAd Failed to load the ad. (reason: {0})", error.GetMessage());
            m_AppOpenAdLoadFailedCallback?.Invoke();
            m_AdmobAdSetup.AppOpenAdUnitID.ChangeID();
        }

        private void OnAppOpenAdDidDismissFullScreenContent()
        {
            Debug.Log("Admob AppOpenAds Dismissed");
            m_AppOpenAd = null;
            m_AppOpenAdClosedCallback?.Invoke();
        }

        private void OnAppOpenAdFailedToPresentFullScreenContent(AdError args)
        {
            Debug.LogFormat("Admob AppOpenAd Failed to present the ad (reason: {0})", args.GetMessage());
            m_AppOpenAd = null;
            m_AppOpenAdFailedToDisplayCallback?.Invoke();
        }

        private void OnAppOpenAdDidPresentFullScreenContent()
        {
            Debug.Log("Admob AppOpenAds opened");
            m_AppOpenAdDisplayedCallback?.Invoke();
        }

        private void OnAppOpenAdDidRecordImpression()
        {
            Debug.Log("Admob AppOpenAds Recorded Impression");
        }

        private void OnAppOpenAppPaidEvent(AdValue adValue)
        {
            Debug.Log("Admob AppOpenAds Paid");
            HandleAdPaidEvent("app_open_ad",adValue, m_AppOpenAd.GetResponseInfo());
        }

        #endregion

        #endregion
        
        private void HandleAdPaidEvent(string adFormat, AdValue adValue, ResponseInfo responseInfo)
        {
            AdapterResponseInfo loadedAdapterResponseInfo = responseInfo.GetLoadedAdapterResponseInfo();
            string adSourceId = loadedAdapterResponseInfo.AdSourceId;
            string adSourceInstanceId = loadedAdapterResponseInfo.AdSourceInstanceId;
            string adSourceInstanceName = loadedAdapterResponseInfo.AdSourceInstanceName;
            string adSourceName = loadedAdapterResponseInfo.AdSourceName;
            string adapterClassName = loadedAdapterResponseInfo.AdapterClassName;
            
            Debug.Log("Admob Paid AdSourceId: " + adSourceId + " AdSourceInstanceId: " + adSourceInstanceId + " AdSourceInstanceName: " + adSourceInstanceName + " AdSourceName: " + adSourceName + " AdapterClassName: " + adapterClassName);
        
            double revenue = (double)adValue.Value / 1000000;
            ImpressionData impression = new ImpressionData {
                ad_mediation = AdsMediationType.ADMOB,
                ad_source = adSourceName,
                ad_unit_name = adSourceInstanceId,
                ad_format = adFormat,
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
    
    [Serializable]
    public class AdScheduleUnitID
    {
#if UNITY_ANDROID
        public List<string> AndroidID = new List<string>();
#elif UNITY_IOS
        public List<string> IosID = new List<string>();
#endif

        private int currentID;

        public void ChangeID()
        {
            currentID++;
            if (currentID >= CurrentPlatformID.Count)
            {
                currentID = 0;
            }
        }

        public void Refresh()
        {
            currentID = 0;
        }

        public string ID => CurrentPlatformID.Count == 0 ? "" : CurrentPlatformID[currentID];

        public List<string> CurrentPlatformID
        {
            get
            {
#if UNITY_ANDROID
                return AndroidID;
#elif UNITY_IOS
                return IosID;
#else
                return null;
#endif
            }
            set
            {
#if UNITY_ANDROID
                AndroidID = value;
#elif UNITY_IOS
                IosID = value;
#endif
            }
        }

        public bool IsActive()
        {
            return CurrentPlatformID.Count > 0;
        }
    }
}
