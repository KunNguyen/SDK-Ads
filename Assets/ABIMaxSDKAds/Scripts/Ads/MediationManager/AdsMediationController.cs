using System.Collections;
using System.Collections.Generic;
using SDK.Struct;
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

        [field: SerializeField] public AdsMediationType AdsMediationType  { get; set; }
        public UnityAction<ImpressionData> AdRevenuePaidCallback { get; set; }

        public bool IsInited = false;
        public virtual void Init() {
            IsInited = true;
        }

        #region Banner Ads
        
        protected BannerCallbacks BannerCallbacks { get; set; } = new BannerCallbacks();
        
        public virtual void InitBannerAds(
            UnityAction bannerLoadedSuccessCallback, UnityAction bannerAdLoadedFailCallback, 
            UnityAction bannerAdsCollapsedCallback, UnityAction bannerAdsExpandedCallback,
            UnityAction bannerAdsDisplayed = null, UnityAction bannerAdsDisplayedFailedCallback = null,
            UnityAction bannerAdsClickedCallback = null) {
            
            BannerCallbacks = new BannerCallbacks
            {
                LoadedSuccess = bannerLoadedSuccessCallback,
                LoadedFail = bannerAdLoadedFailCallback,
                Collapsed = bannerAdsCollapsedCallback,
                Expanded = bannerAdsExpandedCallback,
                Displayed = bannerAdsDisplayed,
                DisplayedFail = bannerAdsDisplayedFailedCallback,
                Clicked = bannerAdsClickedCallback
            };
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
        protected CollapsibleBannerCallbacks CollapsibleCallbacks { get; set; } = new CollapsibleBannerCallbacks();

        
        public virtual void InitCollapsibleBannerAds(UnityAction loadedSuccessCallback, UnityAction loadedFailCallback, UnityAction collapsedCallback, UnityAction expandedCallback, UnityAction destroyedCallback, UnityAction hideCallback) {
            CollapsibleCallbacks = new CollapsibleBannerCallbacks
            {
                LoadedSuccess = loadedSuccessCallback,
                LoadedFail = loadedFailCallback,
                Collapsed = collapsedCallback,
                Expanded = expandedCallback,
                Destroyed = destroyedCallback,
                Hided = hideCallback
            };
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
        
        protected InterstitialCallbacks InterstitialCallbacks { get; set; } = new InterstitialCallbacks();
        public virtual void InitInterstitialAd(UnityAction adClosedCallback, UnityAction adLoadSuccessCallback, UnityAction adLoadFailedCallback, UnityAction adShowSuccessCallback, UnityAction adShowFailCallback) {
            InterstitialCallbacks = new InterstitialCallbacks
            {
                Closed = (isShow) => { adClosedCallback.Invoke();},
                LoadedSuccess = adLoadSuccessCallback,
                LoadedFail = adLoadFailedCallback,
                Displayed = adShowSuccessCallback,
                DisplayedFail= adShowFailCallback
            };
        }
        public virtual void ShowInterstitialAd() { }
        public virtual void RequestInterstitialAd() {
        }
        public virtual bool IsInterstitialLoaded() {
            return false;
        }

        #endregion

        #region Reward Ads
        protected RewardedVideoCallbacks RewardedVideoCallbacks { get; set; } = new RewardedVideoCallbacks();
        public virtual void InitRewardVideoAd(UnityAction videoSuccess, UnityAction<bool> videoClosed, UnityAction videoLoadSuccess, UnityAction videoLoadFailed, UnityAction videoStart) {
            RewardedVideoCallbacks = new RewardedVideoCallbacks
            {
                Completed = videoSuccess,
                Closed = videoClosed,
                LoadedSuccess = videoLoadSuccess,
                LoadedFail = videoLoadFailed,
                Displayed = videoStart,
                DisplayedFailed = videoLoadFailed,
                Clicked = videoLoadSuccess
            };
        }
        public virtual void RequestRewardVideoAd() {
        }
        public virtual void ShowRewardVideoAd() {
        }
        public virtual bool IsRewardVideoLoaded() {
            return false;
        }

        #endregion

        #region MRec Ads
        protected MRecCallbacks MRecCallbacks { get; set; } = new MRecCallbacks();
        
        public virtual void InitRMecAds(UnityAction adLoadedCallback, UnityAction adLoadFailedCallback, UnityAction adClickedCallback, UnityAction adExpandedCallback, UnityAction adCollapsedCallback) {
            MRecCallbacks = new MRecCallbacks
            {
                LoadedSuccess = adLoadedCallback,
                LoadedFail = adLoadFailedCallback,
                Clicked = adClickedCallback,
                Expanded = adExpandedCallback,
                Collapsed = adCollapsedCallback
            };
        }
        public virtual void RequestMRecAds() {
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
        
        protected AppOpenAdCallbacks AppOpenAdCallbacks { get; set; } = new AppOpenAdCallbacks();
        
        public virtual void InitAppOpenAds(UnityAction adLoadedCallback, UnityAction adLoadFailedCallback, 
            UnityAction adClosedCallback, UnityAction adDisplayedCallback, UnityAction adFailedToDisplayCallback)
        {
            AppOpenAdCallbacks = new AppOpenAdCallbacks
            {
                LoadedSuccess = adLoadedCallback,
                LoadedFail = adLoadFailedCallback,
                Closed = (b) => { adClosedCallback.Invoke(); },
                Displayed = adDisplayedCallback,
                DisplayedFail = adFailedToDisplayCallback
            };
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
