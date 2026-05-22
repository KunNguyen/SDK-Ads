using System.Collections.Generic;
#if UNITY_AD_ADMOB
using GoogleMobileAds.Api;
#endif
using UnityEngine;

namespace JisSDKAds.Ads
{
     public partial class SDKSetup
    {
        public AdsMediationType collapsibleBannerAdsMediationType;

#if UNITY_AD_ADMOB
        public AdPosition adsPositionCollapsibleBanner;
#else
        [HideInInspector]
        public int adsPositionCollapsibleBannerFallback = 8; // BottomCenter when AdMob
#endif

        public bool isShowingOnStartCollapsibleBanner = false;

        public bool isAutoRefreshCollapsibleBanner = false;

        public bool isAutoRefreshExtendCollapsibleBanner = false;

        [Range(20f, 60f)]
        public float autoRefreshTime = 30;

        public bool isAutoCloseCollapsibleBanner = false;

        [Range(20f, 60f)]
        public float autoCloseTime = 30;

        public string collapsibleBannerAdUnitID_MAX
        {
            get => maxAdsSetup.CollapsibleBannerAdUnitID;
            set => maxAdsSetup.CollapsibleBannerAdUnitID = value;
        }

        public List<string> collapsibleBannerAdUnitID_ADMOB
        {
            get => admobAdsSetup.CollapsibleBannerAdUnitIDList;
            set => admobAdsSetup.CollapsibleBannerAdUnitIDList = value;
        }
    }
}
