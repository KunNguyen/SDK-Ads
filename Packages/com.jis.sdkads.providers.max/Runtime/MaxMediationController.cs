using System;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Common;
using UnityEngine;
using UnityEngine.Events;

namespace JisSDKAds.Ads
{
    public partial class MaxMediationController : AdsMediationController
    {
#if UNITY_AD_MAX
        private bool IsWatchSuccess { get; set; } = false;
        public MaxAdSetup m_MaxAdConfig;

        public override void Init()
        {
            if (Status != MediationStatus.NotInited) return;
            base.Init();
            DebugAds.Log("[MAX] Init");
            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                DebugAds.Log("[MAX] SDK Initialized");
                Status = MediationStatus.Inited;
            };
            MaxSdk.SetSdkKey(m_MaxAdConfig.SDKKey);
            MaxSdk.SetHasUserConsent(true);
            MaxSdk.SetDoNotSell(false);
            MaxSdk.InitializeSdk();
        }

        private void OnAdRevenuePaidEvent(AdsType adsType, string adUnitId, MaxSdkBase.AdInfo impressionData)
        {
            double revenue = impressionData.Revenue;
            ImpressionData impression = new ImpressionData
            {
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

        public override void InitInterstitialAd(UnityAction adClosedCallback, UnityAction adLoadSuccessCallback,
            UnityAction adLoadFailedCallback, UnityAction adShowSuccessCallback, UnityAction adShowFailCallback)
        {
            base.InitInterstitialAd(adClosedCallback, adLoadSuccessCallback, adLoadFailedCallback,
                adShowSuccessCallback, adShowFailCallback);
            DebugAds.Log("[MAX] Init Interstitial");
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += (adUnitID, adInfo) =>
                OnAdRevenuePaidEvent(AdsType.INTERSTITIAL, adUnitID, adInfo);
        }

        public override void RequestInterstitialAd()
        {
            base.RequestInterstitialAd();
            if (UseSequentialInterstitial)
            {
                EnsureInterstitialTierLoader();
                _interstitialTierLoader.Load();
                return;
            }

            RequestInterstitialLegacy();
        }

        public override bool IsInterstitialLoaded()
        {
            if (UseSequentialInterstitial)
            {
                EnsureInterstitialTierLoader();
                return _interstitialTierLoader.IsReady;
            }

            return MaxSdk.IsInterstitialReady(m_MaxAdConfig.InterstitialAdUnitID);
        }

        public override void ShowInterstitialAd()
        {
            base.ShowInterstitialAd();
            if (UseSequentialInterstitial)
            {
                EnsureInterstitialTierLoader();
                if (!_interstitialTierLoader.Show())
                    OnAdInterstitialFailToShow(new SequentialTierShowError { Code = 0, Message = "not_ready" });
                return;
            }

            if (MaxSdk.IsInterstitialReady(m_MaxAdConfig.InterstitialAdUnitID))
            {
                MaxSdk.ShowInterstitial(m_MaxAdConfig.InterstitialAdUnitID);
            }
            else
            {
                OnAdInterstitialFailToShow(new SequentialTierShowError { Code = 0, Message = "not_ready" });
            }
        }

        void OnAdInterstitialSuccessToLoad()
        {
            DebugAds.Log("[MAX] Load Interstitial success");
            InterstitialCallbacks.LoadedSuccess?.Invoke();
        }

        void OnAdInterstitialFailedToLoad()
        {
            DebugAds.Log("[MAX] Load Interstitial failed");
            InterstitialCallbacks.LoadedFail?.Invoke();
        }

        void OnAdInterstitialOpening()
        {
            DebugAds.Log("[MAX] Interstitial opened");
            InterstitialCallbacks.Displayed?.Invoke();
        }

        void OnAdInterstitialFailToShow(SequentialTierShowError? error)
        {
            DebugAds.Log("[MAX] Interstitial failed to show: " + (error?.Message ?? "unknown"));
            InterstitialCallbacks.DisplayedFail?.Invoke();
        }

        void OnCloseInterstitialAd()
        {
            DebugAds.Log("[MAX] Interstitial closed");
            InterstitialCallbacks.Closed?.Invoke(true);
            if (!UseSequentialInterstitial)
                RequestInterstitialLegacy();
        }

        void RequestInterstitialLegacy()
        {
            var adUnitId = m_MaxAdConfig.InterstitialAdUnitID;
            if (string.IsNullOrEmpty(adUnitId))
            {
                OnAdInterstitialFailedToLoad();
                return;
            }

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnLegacyInterstitialLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnLegacyInterstitialLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnLegacyInterstitialDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnLegacyInterstitialDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnLegacyInterstitialHidden;

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnLegacyInterstitialLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnLegacyInterstitialLoadFailed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnLegacyInterstitialDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnLegacyInterstitialDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnLegacyInterstitialHidden;

            MaxSdk.LoadInterstitial(adUnitId);
        }

        void OnLegacyInterstitialLoaded(string id, MaxSdkBase.AdInfo info) => OnAdInterstitialSuccessToLoad();
        void OnLegacyInterstitialLoadFailed(string id, MaxSdkBase.ErrorInfo error) => OnAdInterstitialFailedToLoad();
        void OnLegacyInterstitialDisplayed(string id, MaxSdkBase.AdInfo info) => OnAdInterstitialOpening();
        void OnLegacyInterstitialDisplayFailed(string id, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo info) =>
            OnAdInterstitialFailToShow(new SequentialTierShowError { Code = (int)error.Code, Message = error.Message });
        void OnLegacyInterstitialHidden(string id, MaxSdkBase.AdInfo info) => OnCloseInterstitialAd();

        public void DestroyInterstitialAd()
        {
            _interstitialTierLoader?.Destroy();
            _interstitialTierLoader = null;
        }

        #endregion

        #region Rewarded Video

        public override void InitRewardVideoAd(UnityAction videoSuccess, UnityAction<bool> videoClosed,
            UnityAction videoLoadSuccess, UnityAction videoLoadFailed, UnityAction videoStart)
        {
            base.InitRewardVideoAd(videoSuccess, videoClosed, videoLoadSuccess, videoLoadFailed, videoStart);
            DebugAds.Log("[MAX] Init RewardedVideoAd");
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += (adUnitID, adInfo) =>
                OnAdRevenuePaidEvent(AdsType.REWARDED, adUnitID, adInfo);
            RequestRewardVideoAd();
        }

        public override void RequestRewardVideoAd()
        {
            base.RequestRewardVideoAd();
            if (UseSequentialRewarded)
            {
                EnsureRewardedTierLoader();
                _rewardedTierLoader.Load();
                return;
            }

            RequestRewardedLegacy();
        }

        public override void ShowRewardVideoAd()
        {
#if UNITY_EDITOR
            IsWatchSuccess = false;
            OnRewardBasedVideoRewarded();
            return;
#endif
            base.ShowRewardVideoAd();
            if (UseSequentialRewarded)
            {
                EnsureRewardedTierLoader();
                IsWatchSuccess = false;
                if (!_rewardedTierLoader.Show())
                    OnRewardedAdFailedToShow(new SequentialTierShowError { Code = 0, Message = "not_ready" });
                return;
            }

            if (IsRewardVideoLoaded())
            {
                IsWatchSuccess = false;
                MaxSdk.ShowRewardedAd(m_MaxAdConfig.RewardedAdUnitID);
            }
        }

        public override bool IsRewardVideoLoaded()
        {
#if UNITY_EDITOR
            return false;
#else
            if (UseSequentialRewarded)
            {
                EnsureRewardedTierLoader();
                return _rewardedTierLoader.IsReady;
            }
            return MaxSdk.IsRewardedAdReady(m_MaxAdConfig.RewardedAdUnitID);
#endif
        }

        void OnRewardBasedVideoLoaded()
        {
            DebugAds.Log("[MAX] RewardedVideoAd Loaded");
            RewardedVideoCallbacks.LoadedSuccess?.Invoke();
        }

        void OnRewardBasedVideoFailedToLoad()
        {
            DebugAds.Log("[MAX] RewardedVideoAd Load Fail");
            RewardedVideoCallbacks.LoadedFail?.Invoke();
        }

        void OnRewardBasedVideoOpened()
        {
            DebugAds.Log("[MAX] RewardedVideoAd Opened");
            RewardedVideoCallbacks.Displayed?.Invoke();
        }

        void OnRewardBasedVideoRewarded()
        {
            DebugAds.Log("[MAX] RewardedVideoAd Rewarded");
            IsWatchSuccess = true;
            if (Application.platform == RuntimePlatform.Android)
            {
                RewardedVideoCallbacks.Completed?.Invoke();
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                EventManager.InvokeNextFrame(RewardedVideoCallbacks.Completed);
            }
        }

        void OnRewardBasedVideoClosed()
        {
            DebugAds.Log("[MAX] RewardedVideoAd Closed");
            if (Application.platform == RuntimePlatform.IPhonePlayer && IsWatchSuccess)
            {
                EventManager.InvokeNextFrame(RewardedVideoCallbacks.Completed);
            }
            EventManager.InvokeNextFrame(() => RewardedVideoCallbacks.Closed?.Invoke(IsWatchSuccess));
        }

        void OnRewardedAdFailedToShow(SequentialTierShowError? error)
        {
            DebugAds.Log("[MAX] RewardedVideoAd Show Fail: " + (error?.Message ?? "unknown"));
            RewardedVideoCallbacks.DisplayedFailed?.Invoke();
        }

        void RequestRewardedLegacy()
        {
            var adUnitId = m_MaxAdConfig.RewardedAdUnitID;
            if (string.IsNullOrEmpty(adUnitId))
            {
                OnRewardBasedVideoFailedToLoad();
                return;
            }

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnLegacyRewardedLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnLegacyRewardedLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= OnLegacyRewardedDisplayed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnLegacyRewardedDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnLegacyRewardedRewarded;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnLegacyRewardedHidden;

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnLegacyRewardedLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnLegacyRewardedLoadFailed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnLegacyRewardedDisplayed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnLegacyRewardedDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnLegacyRewardedRewarded;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnLegacyRewardedHidden;

            MaxSdk.LoadRewardedAd(adUnitId);
        }

        void OnLegacyRewardedLoaded(string id, MaxSdkBase.AdInfo info) => OnRewardBasedVideoLoaded();
        void OnLegacyRewardedLoadFailed(string id, MaxSdkBase.ErrorInfo error) => OnRewardBasedVideoFailedToLoad();
        void OnLegacyRewardedDisplayed(string id, MaxSdkBase.AdInfo info) => OnRewardBasedVideoOpened();
        void OnLegacyRewardedDisplayFailed(string id, MaxSdkBase.ErrorInfo error, MaxSdkBase.AdInfo info) =>
            OnRewardedAdFailedToShow(new SequentialTierShowError { Code = (int)error.Code, Message = error.Message });
        void OnLegacyRewardedRewarded(string id, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo info) => OnRewardBasedVideoRewarded();
        void OnLegacyRewardedHidden(string id, MaxSdkBase.AdInfo info) => OnRewardBasedVideoClosed();

        public void DestroyRewardedAd()
        {
            _rewardedTierLoader?.Destroy();
            _rewardedTierLoader = null;
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
            UnityAction bannerAdsClickedCallback)
        {
            base.InitBannerAds(
                bannerLoadedSuccessCallback, bannerAdLoadedFailCallback, bannerAdsCollapsedCallback,
                bannerAdsExpandedCallback, bannerAdsDisplayed, bannerAdsDisplayedFailedCallback,
                bannerAdsClickedCallback);
            DebugAds.Log("[MAX] Banner Init ID = " + m_MaxAdConfig.BannerAdUnitID);
            MaxSdk.CreateBanner(m_MaxAdConfig.BannerAdUnitID, m_BannerPosition);
            MaxSdk.SetBannerBackgroundColor(m_MaxAdConfig.BannerAdUnitID, Color.black);

            MaxSdkCallbacks.Banner.OnAdLoadedEvent += BannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += BannerAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += BannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += (adUnitID, adInfo) =>
                OnAdRevenuePaidEvent(AdsType.BANNER, adUnitID, adInfo);
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnBannerAdCollapsedEvent;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnBannerAdExpandedEvent;
        }

        public override void ShowBannerAds()
        {
            base.ShowBannerAds();
            MaxSdk.ShowBanner(m_MaxAdConfig.BannerAdUnitID);
        }

        public override void HideBannerAds()
        {
            base.HideBannerAds();
            MaxSdk.HideBanner(m_MaxAdConfig.BannerAdUnitID);
        }

        public override bool IsBannerLoaded() => m_IsBannerLoaded;

        public override void DestroyBannerAds()
        {
            base.DestroyBannerAds();
            MaxSdk.DestroyBanner(m_MaxAdConfig.BannerAdUnitID);
            m_IsBannerLoaded = false;
        }

        private void BannerAdLoadedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            DebugAds.Log("[MAX] Banner Loaded");
            BannerCallbacks.LoadedSuccess?.Invoke();
            m_IsBannerLoaded = true;
        }

        private void BannerAdLoadFailedEvent(string adUnitID, MaxSdkBase.ErrorInfo errorInfo)
        {
            DebugAds.Log("[MAX] Banner Load Fail");
            BannerCallbacks.LoadedFail?.Invoke();
            m_IsBannerLoaded = false;
        }

        private void BannerAdClickedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            DebugAds.Log("[MAX] Banner Clicked");
            BannerCallbacks.Clicked?.Invoke();
        }

        private void OnBannerAdCollapsedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            DebugAds.Log("[MAX] Banner Collapsed");
            BannerCallbacks.Collapsed?.Invoke();
        }

        private void OnBannerAdExpandedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            DebugAds.Log("[MAX] Banner Expanded");
            BannerCallbacks.Expanded?.Invoke();
            BannerCallbacks.Displayed?.Invoke();
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
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += (adUnitID, adInfo) =>
                OnAdRevenuePaidEvent(AdsType.APP_OPEN, adUnitID, adInfo);
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenAdHiddenEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAppOpenAdDisplayedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenAdDisplayFailedEvent;
            RequestAppOpenAds();
        }

        public override void ShowAppOpenAds()
        {
            base.ShowAppOpenAds();
            if (MaxSdk.IsAppOpenAdReady(m_MaxAdConfig.AppOpenAdUnitID))
                MaxSdk.ShowAppOpenAd(m_MaxAdConfig.AppOpenAdUnitID);
        }

        public override void RequestAppOpenAds() =>
            MaxSdk.LoadAppOpenAd(m_MaxAdConfig.AppOpenAdUnitID);

        public override bool IsAppOpenAdsLoaded() =>
            MaxSdk.IsAppOpenAdReady(m_MaxAdConfig.AppOpenAdUnitID);

        private void OnAppOpenAdLoadedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            DebugAds.Log("[MAX] App Open Ads Loaded");
            AppOpenAdCallbacks.LoadedSuccess?.Invoke();
        }

        private void OnAppOpenAdLoadFailedEvent(string adUnitID, MaxSdkBase.ErrorInfo errorInfo)
        {
            DebugAds.Log("[MAX] App Open Ads Load Fail");
            AppOpenAdCallbacks.LoadedFail?.Invoke();
        }

        private void OnAppOpenAdDisplayedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            DebugAds.Log("[MAX] App Open Ads Displayed");
            AppOpenAdCallbacks.Displayed?.Invoke();
        }

        private void OnAppOpenAdDisplayFailedEvent(string adUnitID, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            DebugAds.Log("[MAX] App Open Ads Displayed Fail");
            AppOpenAdCallbacks.DisplayedFail?.Invoke();
        }

        private void OnAppOpenAdHiddenEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            DebugAds.Log("[MAX] App Open Ads Hidden");
            AppOpenAdCallbacks.Closed?.Invoke(true);
        }

        #endregion

        #region Rewarded Interstitial

        public void LoadRewardedInterstitial()
        {
            DebugAds.LogWarning("[MAX][RewardedInterstitial] Unsupported by the installed AppLovin MAX Unity SDK.");
        }

        public bool IsRewardedInterstitialLoaded()
        {
            return false;
        }

        public void ShowRewardedInterstitial(UnityAction rewardCallback, UnityAction<bool> closedCallback = null,
            UnityAction failedCallback = null)
        {
            DebugAds.LogWarning("[MAX][RewardedInterstitial] Unsupported by the installed AppLovin MAX Unity SDK.");
            failedCallback?.Invoke();
            closedCallback?.Invoke(false);
        }

        #endregion

        private void OnApplicationQuit()
        {
            _interstitialTierLoader?.Destroy();
            _rewardedTierLoader?.Destroy();
        }

#endif
        public override AdsMediationType GetAdsMediationType() => AdsMediationType.MAX;
    }

#if !UNITY_AD_MAX
    public enum BannerPosition
    {
        TopLeft, TopCenter, TopRight,
        Centered, CenterLeft, CenterRight,
        BottomLeft, BottomCenter, BottomRight
    }
#endif
}
