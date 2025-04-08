using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SDK {
    public abstract class AdsMediationController : MonoBehaviour {
        [SerializeField]
        protected bool m_IsActive;
        public bool IsActive
        {
            get => m_IsActive;
            set => m_IsActive = value;
        }

        protected AdsMediationType m_AdsMediationType;
        protected UnityAction<ImpressionData> m_AdRevenuePaidCallback;

        public UnityAction<ImpressionData> AdRevenuePaidCallback
        {
            get => m_AdRevenuePaidCallback;
            set => m_AdRevenuePaidCallback = value;
        }

        public bool IsInited = false;
        public virtual void Init() {
            IsInited = true;
        }

        #region Banner Ads
        protected UnityAction m_BannerAdLoadedSuccessCallback;
        protected UnityAction m_BannerAdLoadedFailCallback;
        protected UnityAction m_BannerAdsDisplayedCallback;
        protected UnityAction m_BannerAdsDisplayedFailedCallback;
        protected UnityAction m_BannerAdsClickedCallback;
        protected UnityAction m_BannerAdsCollapsedCallback;
        protected UnityAction m_BannerAdsExpandedCallback;
        
        public virtual void InitBannerAds(
            UnityAction bannerLoadedSuccessCallback, UnityAction bannerAdLoadedFailCallback, 
            UnityAction bannerAdsCollapsedCallback, UnityAction bannerAdsExpandedCallback,
            UnityAction bannerAdsDisplayed = null, UnityAction bannerAdsDisplayedFailedCallback = null,
            UnityAction bannerAdsClickedCallback = null) {
            m_BannerAdLoadedSuccessCallback = bannerLoadedSuccessCallback;
            m_BannerAdLoadedFailCallback = bannerAdLoadedFailCallback;
            m_BannerAdsCollapsedCallback = bannerAdsCollapsedCallback;
            m_BannerAdsExpandedCallback = bannerAdsExpandedCallback;
            m_BannerAdsDisplayedCallback = bannerAdsDisplayed;
            m_BannerAdsDisplayedFailedCallback = bannerAdsDisplayedFailedCallback;
            m_BannerAdsClickedCallback = bannerAdsClickedCallback;
        }
        public virtual void RequestBannerAds() {
        }
        public virtual void ShowBannerAds() {
        }
        public virtual void HideBannerAds() {
        }
        public virtual void CreateBannerAds() {
        }
        public virtual void DestroyBannerAds() {
        }
        public virtual bool IsBannerLoaded() {
            return false;
        }
        #endregion

        #region  Collapsible Banner

        protected UnityAction m_CollapsibleBannerAdLoadedSuccessCallback;
        protected UnityAction m_CollapsibleBannerAdLoadedFailCallback;
        protected UnityAction m_CollapsibleBannerAdsCollapsedCallback;
        protected UnityAction m_CollapsibleBannerAdsExpandedCallback;
        protected UnityAction m_CollapsibleBannerAdsDestroyedCallback;
        protected UnityAction m_CollapsibleBannerAdsHideCallback;
        public virtual void InitCollapsibleBannerAds(UnityAction bannerLoadedSuccessCallback, UnityAction bannerAdLoadedFailCallback, UnityAction bannerAdsCollapsedCallback, UnityAction bannerAdsExpandedCallback, UnityAction bannerAdsDestroyedCallback, UnityAction bannerAdsHideCallback) {
            m_CollapsibleBannerAdLoadedSuccessCallback = bannerLoadedSuccessCallback;
            m_CollapsibleBannerAdLoadedFailCallback = bannerAdLoadedFailCallback;
            m_CollapsibleBannerAdsCollapsedCallback = bannerAdsCollapsedCallback;
            m_CollapsibleBannerAdsExpandedCallback = bannerAdsExpandedCallback;
            m_CollapsibleBannerAdsDestroyedCallback = bannerAdsDestroyedCallback;
            m_CollapsibleBannerAdsHideCallback = bannerAdsHideCallback;
        }
        public virtual void RequestCollapsibleBannerAds(bool isOpenOnStart) {
        }
        public virtual void RefreshCollapsibleBannerAds() {
        }
        public virtual void ShowCollapsibleBannerAds() {
        }
        public virtual void HideCollapsibleBannerAds() {
        }
        public virtual void DestroyCollapsibleBannerAds() {
        }
        public virtual bool IsCollapsibleBannerLoaded() {
            return false;
        }

        #endregion

        #region Interstitial Ads
        protected UnityAction m_InterstitialAdCloseCallback;
        protected UnityAction m_InterstitialAdLoadSuccessCallback;
        protected UnityAction m_InterstitialAdLoadFailCallback;
        protected UnityAction m_InterstitialAdShowSuccessCallback;
        protected UnityAction m_InterstitialAdShowFailCallback;
        public virtual void InitInterstitialAd(UnityAction adClosedCallback, UnityAction adLoadSuccessCallback, UnityAction adLoadFailedCallback, UnityAction adShowSuccessCallback, UnityAction adShowFailCallback) {
            m_InterstitialAdCloseCallback = adClosedCallback;
            m_InterstitialAdLoadSuccessCallback = adLoadSuccessCallback;
            m_InterstitialAdLoadFailCallback = adLoadFailedCallback;
            m_InterstitialAdShowSuccessCallback = adShowSuccessCallback;
            m_InterstitialAdShowFailCallback = adShowFailCallback;
        }
        public virtual void ShowInterstitialAd() { }
        public virtual void RequestInterstitialAd() {
        }
        public virtual bool IsInterstitialLoaded() {
            return false;
        }

        #endregion

        #region Reward Ads
        protected UnityAction<bool> m_RewardedVideoCloseCallback;
        protected UnityAction m_RewardedVideoLoadSuccessCallback;
        protected UnityAction m_RewardedVideoLoadFailedCallback;
        protected UnityAction m_RewardedVideoEarnSuccessCallback;
        protected UnityAction m_RewardedVideoShowStartCallback;
        protected UnityAction m_RewardedVideoShowFailCallback;
        public virtual void InitRewardVideoAd(UnityAction<bool> videoClosed, UnityAction videoLoadSuccess, UnityAction videoLoadFailed, UnityAction videoStart) {
            m_RewardedVideoCloseCallback = videoClosed;
            m_RewardedVideoLoadSuccessCallback = videoLoadSuccess;
            m_RewardedVideoLoadFailedCallback = videoLoadFailed;
            m_RewardedVideoShowStartCallback = videoStart;
        }
        public virtual void RequestRewardVideoAd() {
        }
        public virtual void ShowRewardVideoAd(UnityAction successCallback, UnityAction failedCallback) {
            m_RewardedVideoEarnSuccessCallback = successCallback;
            m_RewardedVideoShowFailCallback = failedCallback;
        }
        public virtual bool IsRewardVideoLoaded() {
            return false;
        }

        #endregion

        #region MRec Ads
        protected UnityAction m_MRecAdLoadedCallback;
        protected UnityAction m_MRecAdLoadFailCallback;
        protected UnityAction m_MRecAdClickedCallback;
        protected UnityAction m_MRecAdExpandedCallback;
        protected UnityAction m_MRecAdCollapsedCallback;
        public virtual void InitRMecAds(UnityAction adLoadedCallback, UnityAction adLoadFailedCallback, UnityAction adClickedCallback, UnityAction adExpandedCallback, UnityAction adCollapsedCallback) {
            m_MRecAdLoadedCallback = adLoadedCallback;
            m_MRecAdLoadFailCallback = adLoadFailedCallback;
            m_MRecAdClickedCallback = adClickedCallback;
            m_MRecAdExpandedCallback = adExpandedCallback;
            m_MRecAdCollapsedCallback = adCollapsedCallback;
        }
        public virtual void ShowMRecAds() {
            
        }
        public virtual void HideMRecAds() {
            
        }
        public virtual bool IsMRecLoaded() {
            return false;
        }
        #endregion

        #region App Open Ads
        protected UnityAction m_AppOpenAdLoadedCallback;
        protected UnityAction m_AppOpenAdLoadFailedCallback;
        protected UnityAction m_AppOpenAdClosedCallback;
        protected UnityAction m_AppOpenAdDisplayedCallback;
        protected UnityAction m_AppOpenAdFailedToDisplayCallback;
        public virtual void InitAppOpenAds(UnityAction adLoadedCallback, UnityAction adLoadFailedCallback, 
            UnityAction adClosedCallback, UnityAction adDisplayedCallback, UnityAction adFailedToDisplayCallback)
        {
            m_AppOpenAdLoadedCallback = adLoadedCallback;
            m_AppOpenAdLoadFailedCallback = adLoadFailedCallback;
            m_AppOpenAdClosedCallback = adClosedCallback;
            m_AppOpenAdDisplayedCallback = adDisplayedCallback;
            m_AppOpenAdFailedToDisplayCallback = adFailedToDisplayCallback;
        }

        public virtual void ShowAppOpenAds()
        {
        }
        public virtual void RequestAppOpenAds()
        {
        }
        public virtual bool IsAppOpenAdsLoaded()
        {
            return false;
        }
        #endregion
        public virtual bool IsActiveAdsType(AdsType adsType) {
            return m_IsActive;
        }
        public abstract AdsMediationType GetAdsMediationType();
    }

    [System.Serializable]
    public class AdUnitID
    {
        #if UNITY_ANDROID
        [LabelText("ID")]
        public string AndroidID;
        #elif UNITY_IOS
        [LabelText("ID")]
        public string IOSID;
        #endif
        public string ID
        {
            get
            {
#if UNITY_ANDROID
                return AndroidID;
#elif UNITY_IOS
            return IOSID;
#else
            return "";
#endif
            }
            set
            {
#if UNITY_ANDROID
                AndroidID = value;
#elif UNITY_IOS
                IOSID = value;
#else
#endif
            }
        }
    }
}
