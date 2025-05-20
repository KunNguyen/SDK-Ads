using System;
using System.Collections;
using System.Collections.Generic;
using ABIMaxSDKAds.Scripts;
using UnityEngine;
using UnityEngine.Events;
namespace SDK {
    public class MaxMediationController : AdsMediationController {
#if UNITY_AD_MAX
        private bool IsWatchSuccess { get; set; } = false;
        public MaxAdSetup m_MaxAdConfig;

        private void Awake() {
        }
        private void Start() {
            
        }

        public override void Init()
        {
            if (IsInited) return;
            base.Init();
            DebugAds.Log("unity-script: MyAppStart Start called");
            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration => {
                // AppLovin SDK is initialized, configure and start loading ads.
                DebugAds.Log("MAX SDK Initialized");
                AdsManager.Instance.InitAds(AdsMediationType.MAX);
                MaxSdk.ShowMediationDebugger();
            };
            MaxSdk.SetSdkKey(m_MaxAdConfig.SDKKey);
            MaxSdk.SetHasUserConsent(true);
            MaxSdk.SetDoNotSell(false);
            MaxSdk.InitializeSdk();
        }

        private void OnAdRevenuePaidEvent(AdsType adsType, string adUnitId, MaxSdkBase.AdInfo impressionData) {
            double revenue = impressionData.Revenue;
            ImpressionData impression = new ImpressionData {
                ad_mediation = AdsMediationType.MAX,
                ad_source = impressionData.NetworkName,
                ad_unit_name = impressionData.AdUnitIdentifier,
                ad_format = impressionData.AdFormat,
                ad_revenue = revenue,
                ad_currency = "USD",
                ad_type = impressionData.AdFormat
            };
            AdRevenuePaidCallback?.Invoke(impression);
        }
        #region Interstitial
        public override void InitInterstitialAd(UnityAction adClosedCallback, UnityAction adLoadSuccessCallback, UnityAction adLoadFailedCallback, UnityAction adShowSuccessCallback, UnityAction adShowFailCallback) {
            base.InitInterstitialAd(adClosedCallback, adLoadSuccessCallback, adLoadFailedCallback, adShowSuccessCallback, adShowFailCallback);
            DebugAds.Log("Init MAX Interstitial");
            // Attach callbacks
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialFailedToDisplayEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialDismissedEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += (adUnitID, adInfo) => { OnAdRevenuePaidEvent(AdsType.INTERSTITIAL, adUnitID, adInfo);};
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialAdShowSucceededEvent;
        }
        public override void RequestInterstitialAd() {
            base.RequestInterstitialAd();
            DebugAds.Log("Request MAX Interstitial");
            MaxSdk.LoadInterstitial(m_MaxAdConfig.InterstitialAdUnitID);
        }
        public override void ShowInterstitialAd() {
            base.ShowInterstitialAd();
            DebugAds.Log("Show MAX Interstitial");
            MaxSdk.ShowInterstitial(m_MaxAdConfig.InterstitialAdUnitID);
        }
        public override bool IsInterstitialLoaded() {
            return MaxSdk.IsInterstitialReady(m_MaxAdConfig.InterstitialAdUnitID);
        }
        void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) {
            DebugAds.Log("Load MAX Interstitial Success");
            InterstitialCallbacks.LoadedSuccess?.Invoke();
        }
        void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo) {
            DebugAds.Log("Load MAX Interstitial Fail");
            InterstitialCallbacks.LoadedFail?.Invoke();
        }
        void InterstitialFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo) {
            DebugAds.Log("unity-script: I got InterstitialAdShowFailedEvent, code :  " + errorInfo.Code + ", description : " + errorInfo.Message);
            InterstitialCallbacks.Displayed?.Invoke();
        }
        void OnInterstitialDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) {
            DebugAds.Log("Interstitial dismissed");
            InterstitialCallbacks.Closed?.Invoke(true);
        }
        void OnInterstitialAdShowSucceededEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) {
            DebugAds.Log("unity-script: I got InterstitialAdShowSuccee");
            InterstitialCallbacks.Displayed?.Invoke();
        }
        #endregion

        #region Rewards Video
        public override void InitRewardVideoAd(UnityAction videoSuccess,UnityAction<bool> videoClosed, UnityAction videoLoadSuccess,
            UnityAction videoLoadFailed, UnityAction videoStart) {
            base.InitRewardVideoAd(videoSuccess, videoClosed, videoLoadSuccess, videoLoadFailed,videoStart);

            DebugAds.Log("Init MAX RewardedVideoAd");
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += Rewarded_OnAdStartedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += Rewarded_OnAdShowFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += Rewarded_OnAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += Rewarded_OnAdRewardedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += Rewarded_OnAdClosedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += Rewarded_OnAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += Rewarded_OnAdLoadedFailEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += (adUnitID, adInfo) => { OnAdRevenuePaidEvent(AdsType.REWARDED, adUnitID, adInfo);};
            RequestRewardVideoAd();
        }


        public override void RequestRewardVideoAd() {
            base.RequestRewardVideoAd();
            DebugAds.Log("Request MAX RewardedVideoAd");
#if UNITY_EDITOR
            Rewarded_OnAdLoadedFailEvent("", null);
#else
            MaxSdk.LoadRewardedAd(m_MaxAdConfig.RewardedAdUnitID);
#endif
        }
        public override void ShowRewardVideoAd(){
#if UNITY_EDITOR
            IsWatchSuccess = false;
            Rewarded_OnAdRewardedEvent("", new MaxSdkBase.Reward(), null);
#else
        IsWatchSuccess = false;
        MaxSdk.ShowRewardedAd(m_MaxAdConfig.RewardedAdUnitID);{
#endif
        }
        public override bool IsRewardVideoLoaded() {
#if UNITY_EDITOR
            return false;
#else
            return MaxSdk.IsRewardedAdReady(m_MaxAdConfig.RewardedAdUnitID);
#endif
        }

        /************* RewardedVideo Delegates *************/
        private void Rewarded_OnAdLoadedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            DebugAds.Log("MAX Ads: RewardedVideoAd MAX Loaded Success");
            RewardedVideoCallbacks.LoadedSuccess?.Invoke();
        }
        private void Rewarded_OnAdLoadedFailEvent(string adUnitID, MaxSdkBase.ErrorInfo adError) {
            DebugAds.Log("MAX Ads: RewardedVideoAd MAX Loaded Fail");
            RewardedVideoCallbacks.LoadedFail?.Invoke();
        }
        void Rewarded_OnAdRewardedEvent(string adUnitID, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
        DebugAds.Log("MAX Ads: I got RewardedVideoAdRewardedEvent");
            IsWatchSuccess = true;
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                {
                    if (RewardedVideoCallbacks.Completed != null) {
                        DebugAds.Log("MAX Ads: Watch video Success Callback!");
                        RewardedVideoCallbacks.Completed.Invoke();
                        RewardedVideoCallbacks.Completed = null;
                    }

                    break;
                }
                case RuntimePlatform.IPhonePlayer:
                {
                    if (RewardedVideoCallbacks.Completed != null) {
                        DebugAds.Log("MAX Ads: Watch video Success Callback!");
                        EventManager.AddEventNextFrame(RewardedVideoCallbacks.Completed);
                        RewardedVideoCallbacks.Completed = null;
                    }
                    break;
                }
            }
        }
        void Rewarded_OnAdClosedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            DebugAds.Log("MAX Ads: I got RewardedVideoAdClosedEvent");
            if (RewardedVideoCallbacks.Completed != null && IsWatchSuccess) {
                EventManager.AddEventNextFrame(RewardedVideoCallbacks.Completed);
                RewardedVideoCallbacks.Completed = null;
            }

            RewardedVideoCallbacks.Closed?.Invoke(IsWatchSuccess);
        }
        void Rewarded_OnAdStartedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            DebugAds.Log("MAX Ads: I got RewardedVideoAdStartedEvent");
            RewardedVideoCallbacks.Displayed?.Invoke();
        }
        void RewardedVideoAdEndedEvent() {
            DebugAds.Log("MAX Ads: I got RewardedVideoAdEndedEvent");
            IsWatchSuccess = true;
        }
        void Rewarded_OnAdShowFailedEvent(string adUnitID, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo) {
            DebugAds.Log("MAX Ads: I got RewardedVideoAdShowFailedEvent, code :  " + errorInfo.Code + ", description : " + errorInfo.Message);
            RewardedVideoCallbacks.DisplayedFailed?.Invoke();
        }
        void Rewarded_OnAdClickedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            DebugAds.Log("MAX Ads: I got RewardedVideoAdClickedEvent");
        }
        #endregion

        #region Banner

        public MaxSdkBase.BannerPosition m_BannerPosition;
        private bool m_IsBannerLoaded;
        public override void InitBannerAds(
            UnityAction bannerLoadedSuccessCallback, 
            UnityAction bannerAdLoadedFailCallback, 
            UnityAction bannerAdsCollapsedCallback, 
            UnityAction bannerAdsExpandedCallback,
            UnityAction bannerAdsDisplayed, 
            UnityAction bannerAdsDisplayedFailedCallback,
            UnityAction bannerAdsClickedCallback) {
            base.InitBannerAds(
                bannerLoadedSuccessCallback, 
                bannerAdLoadedFailCallback, 
                bannerAdsCollapsedCallback, 
                bannerAdsExpandedCallback,
                bannerAdsDisplayed,
                bannerAdsDisplayedFailedCallback,
                bannerAdsClickedCallback);
            DebugAds.Log("Banner MAX Init ID = " + m_MaxAdConfig.BannerAdUnitID);
            MaxSdk.CreateBanner(m_MaxAdConfig.BannerAdUnitID, m_BannerPosition);
            MaxSdk.SetBannerBackgroundColor(m_MaxAdConfig.BannerAdUnitID, Color.black);
            
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += BannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += BannerAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += BannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += (adUnitID, adInfo) => { OnAdRevenuePaidEvent(AdsType.BANNER, adUnitID, adInfo);};
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnBannerAdCollapsedEvent;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnBannerAdExpandedEvent;
        }

        public override void ShowBannerAds() {
            base.ShowBannerAds();
            DebugAds.Log("MAX Mediation Banner Call Show");
            MaxSdk.ShowBanner(m_MaxAdConfig.BannerAdUnitID);
        }
        public override void HideBannerAds()
        {
            base.HideBannerAds();
            DebugAds.Log("MAX Mediation Banner Call Hide");
            MaxSdk.HideBanner(m_MaxAdConfig.BannerAdUnitID);
        }

        public override bool IsBannerLoaded()
        {
            return m_IsBannerLoaded;
        }

        private void BannerAdLoadedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            DebugAds.Log("MAX Mediation Banner Loaded Success");
            BannerCallbacks.LoadedSuccess?.Invoke();
            m_IsBannerLoaded = true;
        }
        private void BannerAdLoadFailedEvent(string adUnitID, MaxSdkBase.ErrorInfo errorInfo)
        {
            DebugAds.Log("MAX Mediation Banner Loaded Fail");
            BannerCallbacks.LoadedFail?.Invoke();
            m_IsBannerLoaded = false;
        }
        private void BannerAdClickedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            DebugAds.Log("MAX Mediation Banner Clicked");
            BannerCallbacks.Clicked?.Invoke();
        }
        private void OnBannerAdCollapsedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            DebugAds.Log("MAX Mediation Banner Collapsed");
            BannerCallbacks.Collapsed?.Invoke();
        }
        private void OnBannerAdExpandedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            DebugAds.Log("MAX Mediation Banner Expanded");
            BannerCallbacks.Expanded?.Invoke();
        }
        #endregion
        
        #region MREC
        private bool m_IsMRecLoaded = false;
        public override void InitRMecAds(UnityAction adLoadedCallback, UnityAction adLoadFailedCallback, UnityAction adClickedCallback, UnityAction adExpandedCallback, UnityAction adCollapsedCallback)
        {
            base.InitRMecAds(adLoadedCallback, adLoadFailedCallback, adClickedCallback, adExpandedCallback, adCollapsedCallback);
            DebugAds.Log("MAX Start Init MREC");
            MaxSdkCallbacks.MRec.OnAdLoadedEvent      += OnMRecAdLoadedEvent;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent  += OnMRecAdLoadFailedEvent;
            MaxSdkCallbacks.MRec.OnAdClickedEvent     += OnMRecAdClickedEvent;
            MaxSdkCallbacks.MRec.OnAdExpandedEvent    += OnMRecAdExpandedEvent;
            MaxSdkCallbacks.MRec.OnAdCollapsedEvent   += OnMRecAdCollapsedEvent;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += (adUnitID, adInfo) => { OnAdRevenuePaidEvent(AdsType.MREC, adUnitID, adInfo);};
            MaxSdk.CreateMRec(m_MaxAdConfig.MrecAdUnitID, MaxSdkBase.AdViewPosition.BottomCenter);
        }
        public override void RequestMRecAds()
        {
            base.RequestMRecAds();
            DebugAds.Log("MAX Mediation MREC Call Request");
            MaxSdk.LoadMRec(m_MaxAdConfig.MrecAdUnitID);
        }
        public override bool IsMRecLoaded()
        {
            return m_IsMRecLoaded;
        }
        private void OnMRecAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            DebugAds.Log("MAX Mediation MREC Loaded Success");
            m_IsMRecLoaded = true;
            MRecCallbacks.LoadedSuccess?.Invoke();
        }
        private void OnMRecAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo error)
        {
            DebugAds.Log("MAX Mediation MREC Loaded Fail");
            m_IsMRecLoaded = false;
            MRecCallbacks.LoadedFail?.Invoke();
        }
        private void OnMRecAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            MRecCallbacks.Clicked?.Invoke();
        }
        private void OnMRecAdExpandedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            MRecCallbacks.Expanded?.Invoke();
        }
        private void OnMRecAdCollapsedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            MRecCallbacks.Collapsed?.Invoke();
        }
        public override void ShowMRecAds()
        {
            base.ShowMRecAds();
            MaxSdk.ShowMRec(m_MaxAdConfig.MrecAdUnitID);
        }
        public override void HideMRecAds()
        {
            base.HideMRecAds();
            MaxSdk.HideMRec(m_MaxAdConfig.MrecAdUnitID);
        }
        #endregion
        
        #region App Open Ads

        public override void InitAppOpenAds(UnityAction adLoadedCallback, UnityAction adLoadFailedCallback, 
            UnityAction adClosedCallback, UnityAction adDisplayedCallback, UnityAction adFailedToDisplayCallback)
        {
            base.InitAppOpenAds(adLoadedCallback, adLoadFailedCallback, 
                adClosedCallback, adDisplayedCallback, adFailedToDisplayCallback);
            
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAppOpenAdLoadedEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAppOpenAdLoadFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent += OnAppOpenAdClickedEvent;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += (adUnitID, adInfo) => { OnAdRevenuePaidEvent(AdsType.APP_OPEN, adUnitID, adInfo);};
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenAdHiddenEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAppOpenAdDisplayedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenAdDisplayFailedEvent;
            RequestAppOpenAds();
        }
        public override void ShowAppOpenAds()
        {
            base.ShowAppOpenAds();
            MaxSdk.ShowAppOpenAd(m_MaxAdConfig.AppOpenAdUnitID);
        }
        public override void RequestAppOpenAds()
        {
            MaxSdk.LoadAppOpenAd(m_MaxAdConfig.AppOpenAdUnitID);
        }
        public override bool IsAppOpenAdsLoaded()
        {
            return MaxSdk.IsAppOpenAdReady(m_MaxAdConfig.AppOpenAdUnitID);
        }
        private void OnAppOpenAdLoadedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            DebugAds.Log("MAX Mediation App Open Ads Loaded Success");
            AppOpenAdCallbacks.LoadedSuccess?.Invoke();
        }
        private void OnAppOpenAdLoadFailedEvent(string adUnitID, MaxSdkBase.ErrorInfo errorInfo)
        {
            DebugAds.Log("MAX Mediation App Open Ads Loaded Fail");
            AppOpenAdCallbacks.LoadedFail?.Invoke();
        }
        private void OnAppOpenAdClickedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
        }
        private void OnAppOpenAdDisplayedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            DebugAds.Log("MAX Mediation App Open Ads Displayed");
            AppOpenAdCallbacks.Displayed?.Invoke();
        }
        private void OnAppOpenAdDisplayFailedEvent(string adUnitID, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            DebugAds.Log("MAX Mediation App Open Ads Displayed Fail");
            AppOpenAdCallbacks.DisplayedFail?.Invoke();
        }
        private void OnAppOpenAdHiddenEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            DebugAds.Log("MAX Mediation App Open Ads Hidden");
            AppOpenAdCallbacks.Closed?.Invoke(true);
        }
        #endregion
 #endif
        public override AdsMediationType GetAdsMediationType() {
            return AdsMediationType.MAX;
        }
    }
    #if !UNITY_AD_MAX
    public enum BannerPosition
    {
        TopLeft,
        TopCenter,
        TopRight,
        Centered,
        CenterLeft,
        CenterRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }
    #endif
}
