using JisSDKAds.Ads.Tracking;
using UnityEngine;

namespace JisSDKAds.Analytics.AppsFlyer{
#if UNITY_APPSFLYER
    static class AppsflyerManagerRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            AppsflyerManager.RegisterTracking(
                AppsflyerTracking.TrackInterstitial_ShowCount,
                AppsflyerTracking.TrackRewarded_ClickShowButton,
                AppsflyerTracking.TrackRewarded_Displayed,
                AppsflyerTracking.TrackRewarded_LoadedSuccess,
                AppsflyerTracking.TrackInterstitial_LoadedSuccess,
                AppsflyerTracking.TrackInterstitial_Displayed,
                AppsflyerTracking.TrackInterstitial_ClickShowButton,
                AppsflyerTracking.TrackAppsflyerAdRevenue);
        }
    }
#endif
}
