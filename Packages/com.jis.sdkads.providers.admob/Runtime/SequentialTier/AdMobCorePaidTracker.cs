#if UNITY_AD_ADMOB
using JisSDKAds.Ads;
using JisSDKAds.Ads.Integration;
using JisSDKAds.Ads.SequentialTier;
#if UNITY_APPSFLYER
using JisSDKAds.Ads.Tracking;
#endif

namespace JisSDKAds.Providers.AdMob.SequentialTier
{
    internal static class AdMobCorePaidTracker
    {
        public static void Track(string adFormat, SequentialTierPaidEvent paid)
        {
            var impression = new ImpressionData
            {
                ad_mediation = AdsMediationType.ADMOB,
                ad_source = paid.AdSource,
                ad_sourceID = paid.AdSourceId,
                ad_unit_name = string.IsNullOrEmpty(paid.AdSourceInstanceId)
                    ? paid.AdUnitId
                    : paid.AdSourceInstanceId,
                ad_format = adFormat,
                ad_currency = string.IsNullOrEmpty(paid.Currency) ? "USD" : paid.Currency,
                ad_revenue = paid.Revenue,
                ad_type = adFormat
            };

            var setup = JisAds.Instance?.Settings?.GetActiveProfile()?.sdkSetup;
            AdsTracker.TrackAdImpression(
                impression,
                setup == null || setup.IsActiveAdImpressionTracking,
                setup != null && setup.IsActiveCustomAdImpressionTracking,
                setup?.CustomAdImpressionEventName ?? "");

#if UNITY_APPSFLYER
            AppsflyerManager.TrackAppsflyerAdRevenue(impression);
#endif
            AdsAnalyticsBridge.PublishAdImpression(impression);
        }
    }
}
#endif
