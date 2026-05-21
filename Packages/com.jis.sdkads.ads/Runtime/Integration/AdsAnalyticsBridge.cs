using System;

namespace JisSDKAds.Ads.Integration
{
    /// <summary>
    /// Optional analytics hooks (SolarEngine, etc.) without referencing provider assemblies from Ads.
    /// </summary>
    public static class AdsAnalyticsBridge
    {
        static Action<ImpressionData> _adImpression;

        public static event Action<ImpressionData> AdImpressionTracked
        {
            add => _adImpression += value;
            remove => _adImpression -= value;
        }

        public static void PublishAdImpression(ImpressionData impressionData) =>
            _adImpression?.Invoke(impressionData);
    }
}
