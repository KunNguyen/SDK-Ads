#if UNITY_SOLAR_ENGINE
using JisSDKAds.Ads.Integration;
using UnityEngine;

namespace JisSDKAds.Analytics.SolarEngine
{
    static class SolarEngineAdsIntegration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() =>
            AdsAnalyticsBridge.AdImpressionTracked += impression =>
                SolarEngineManager.Instance?.TrackAdImpression(impression);
    }
}
#endif
