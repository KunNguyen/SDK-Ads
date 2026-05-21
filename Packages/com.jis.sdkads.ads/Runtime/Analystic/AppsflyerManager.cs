using System;

namespace JisSDKAds.Ads.Tracking
{
    /// <summary>
    /// Static facade for AppsFlyer tracking. Implementation is wired at runtime by
    /// com.jis.sdkads.analytics.appsflyer when that package is installed.
    /// </summary>
    public static class AppsflyerManager
    {
        static Action<int> trackInterstitial_ShowCount;
        static Action trackRewarded_ClickShowButton;
        static Action trackRewarded_Displayed;
        static Action trackRewarded_LoadedSuccess;
        static Action trackInterstitial_LoadedSuccess;
        static Action trackInterstitial_Displayed;
        static Action trackInterstitial_ClickShowButton;
        static Action<ImpressionData> trackAppsflyerAdRevenue;

        public static void RegisterTracking(
            Action<int> trackInterstitialShowCount,
            Action trackRewardedClickShowButton,
            Action trackRewardedDisplayed,
            Action trackRewardedLoadedSuccess,
            Action trackInterstitialLoadedSuccess,
            Action trackInterstitialDisplayed,
            Action trackInterstitialClickShowButton,
            Action<ImpressionData> trackAppsflyerAdRevenueImpl)
        {
            trackInterstitial_ShowCount = trackInterstitialShowCount;
            trackRewarded_ClickShowButton = trackRewardedClickShowButton;
            trackRewarded_Displayed = trackRewardedDisplayed;
            trackRewarded_LoadedSuccess = trackRewardedLoadedSuccess;
            trackInterstitial_LoadedSuccess = trackInterstitialLoadedSuccess;
            trackInterstitial_Displayed = trackInterstitialDisplayed;
            trackInterstitial_ClickShowButton = trackInterstitialClickShowButton;
            trackAppsflyerAdRevenue = trackAppsflyerAdRevenueImpl;
        }

        public static void TrackInterstitial_ShowCount(int total) =>
            trackInterstitial_ShowCount?.Invoke(total);

        public static void TrackRewarded_ClickShowButton() =>
            trackRewarded_ClickShowButton?.Invoke();

        public static void TrackRewarded_Displayed() =>
            trackRewarded_Displayed?.Invoke();

        public static void TrackRewarded_LoadedSuccess() =>
            trackRewarded_LoadedSuccess?.Invoke();

        public static void TrackInterstitial_LoadedSuccess() =>
            trackInterstitial_LoadedSuccess?.Invoke();

        public static void TrackInterstitial_Displayed() =>
            trackInterstitial_Displayed?.Invoke();

        public static void TrackInterstitial_ClickShowButton() =>
            trackInterstitial_ClickShowButton?.Invoke();

        public static void TrackAppsflyerAdRevenue(ImpressionData impressionData) =>
            trackAppsflyerAdRevenue?.Invoke(impressionData);
    }
}
