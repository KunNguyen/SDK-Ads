using System.Collections.Generic;
#if UNITY_AD_ADMOB
using GoogleMobileAds.Api;
#endif
using Sirenix.OdinInspector;
using UnityEngine;

namespace SDK
{
     public partial class SDKSetup
    {
        [BoxGroup("COLLAPSIBLE BANNER"),PropertyOrder(5)] 
        public AdsMediationType collapsibleBannerAdsMediationType;

#if UNITY_AD_ADMOB
        [BoxGroup("COLLAPSIBLE BANNER"), PropertyOrder(5)]
        [ShowInInspector, ShowIf("@collapsibleBannerAdsMediationType != AdsMediationType.NONE")]
        public AdPosition adsPositionCollapsibleBanner;
#else
        [BoxGroup("COLLAPSIBLE BANNER"), PropertyOrder(5), HideInInspector]
        public int adsPositionCollapsibleBannerFallback = 8; // BottomCenter when AdMob
#endif

        [BoxGroup("COLLAPSIBLE BANNER"), PropertyOrder(5)]
        [ShowInInspector, ShowIf("@collapsibleBannerAdsMediationType != AdsMediationType.NONE")]
        public bool isShowingOnStartCollapsibleBanner = false;

        [BoxGroup("COLLAPSIBLE BANNER"), PropertyOrder(5)]
        [ShowInInspector, ShowIf("@collapsibleBannerAdsMediationType != AdsMediationType.NONE")]
        public bool isAutoRefreshCollapsibleBanner = false;

        [BoxGroup("COLLAPSIBLE BANNER"), PropertyOrder(5)]
        [ShowInInspector,
         ShowIf("@collapsibleBannerAdsMediationType != AdsMediationType.NONE && isAutoRefreshCollapsibleBanner")]
        public bool isAutoRefreshExtendCollapsibleBanner = false;

        [BoxGroup("COLLAPSIBLE BANNER"), PropertyOrder(5)]
        [ShowInInspector,
         ShowIf("@collapsibleBannerAdsMediationType != AdsMediationType.NONE && isAutoRefreshCollapsibleBanner")]
        [Range(20f, 60f)]
        public float autoRefreshTime = 30;

        [BoxGroup("COLLAPSIBLE BANNER"), PropertyOrder(5)]
        [ShowInInspector, ShowIf("@collapsibleBannerAdsMediationType != AdsMediationType.NONE")]
        public bool isAutoCloseCollapsibleBanner = false;

        [BoxGroup("COLLAPSIBLE BANNER"), PropertyOrder(5)]
        [ShowInInspector,
         ShowIf("@collapsibleBannerAdsMediationType != AdsMediationType.NONE && isAutoCloseCollapsibleBanner")]
        [Range(20f, 60f)]
        public float autoCloseTime = 30;

        [BoxGroup("COLLAPSIBLE BANNER"), PropertyOrder(5)]
        [ShowInInspector, ShowIf("@collapsibleBannerAdsMediationType == AdsMediationType.MAX")]
        public string collapsibleBannerAdUnitID_MAX
        {
            get => maxAdsSetup.CollapsibleBannerAdUnitID;
            set => maxAdsSetup.CollapsibleBannerAdUnitID = value;
        }

        [BoxGroup("COLLAPSIBLE BANNER"), PropertyOrder(5)]
        [ShowInInspector, ShowIf("@collapsibleBannerAdsMediationType == AdsMediationType.ADMOB")]
        public List<string> collapsibleBannerAdUnitID_ADMOB
        {
            get => admobAdsSetup.CollapsibleBannerAdUnitIDList;
            set => admobAdsSetup.CollapsibleBannerAdUnitIDList = value;
        }
    }
}