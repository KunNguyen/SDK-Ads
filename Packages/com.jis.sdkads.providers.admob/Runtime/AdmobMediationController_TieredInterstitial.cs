#if UNITY_AD_ADMOB
using GoogleMobileAds.Api;
using JisSDKAds.Ads.SequentialTier;
using static JisSDKAds.Ads.SequentialTier.AdLoadPipeline;
using JisSDKAds.Common;
using JisSDKAds.Providers.AdMob.SequentialTier;
using UnityEngine;

namespace JisSDKAds.Ads
{
    public partial class AdmobMediationController
    {
        SequentialTierLoader _interstitialTierLoader;

        SequentialTierConfig InterstitialTierConfig =>
            m_AdmobAdSetup?.InterstitialTierConfig;

        bool UseSequentialInterstitial =>
            InterstitialTierConfig != null && InterstitialTierConfig.enableSequentialLadder;

        internal void ResetInterstitialTierLoader()
        {
            _interstitialTierLoader?.Destroy();
            _interstitialTierLoader = null;
        }

        public void ResetSequentialTierLoadersAfterRemoteConfig()
        {
            ResetInterstitialTierLoader();
            ResetRewardedTierLoader();
        }

        void EnsureInterstitialTierLoader()
        {
            if (AdsManager.UsesJisAdsCoreForStandardLoads())
                return;
            if (_interstitialTierLoader != null || InterstitialTierConfig == null) return;

            _interstitialTierLoader = new SequentialTierLoader(
                this,
                "int",
                "interstitial",
                AdLoadFormat.Interstitial,
                InterstitialTierConfig,
                () => new AdMobInterstitialAdapter());

            _interstitialTierLoader.SetCallbacks(
                OnAdInterstitialSuccessToLoad,
                OnAdInterstitialFailedToLoad,
                new SequentialTierShowHooks
                {
                    onClosed = () =>
                    {
                        OnCloseInterstitialAd();
                        _interstitialTierLoader?.Load();
                    },
                    onOpened = () =>
                    {
                        if (_interstitialTierLoader?.ReadyCache != null)
                        {
                            var c = _interstitialTierLoader.ReadyCache;
                            SequentialTierAnalytics.LogShowSuccess("interstitial", c.AdUnitId, c.Tier);
                        }

                        OnAdInterstitialOpening();
                    },
                    onFailed = err => OnAdInterstitialFailToShow(null),
                    onPaid = _ => { /* paid handled via OnAdInterstitialPaid registered on AdMob ad object */ }
                });
        }

        void RequestInterstitialLegacy() =>
            RunInterstitialLoad(ExecuteRequestInterstitialLegacy);

        void ExecuteRequestInterstitialLegacy()
        {
            DebugAds.Log("Request interstitial ads (legacy single unit)");

            if (InterstitialAds != null)
            {
                InterstitialAds.Destroy();
                InterstitialAds = null;
            }

            var adRequest = new AdRequest();

            var adUnitId = GetLegacyInterstitialAdUnit();
            if (string.IsNullOrEmpty(adUnitId))
            {
                NotifyInterstitialFinished();
                DebugAds.LogAdEvent("interstitial", "load_fail", null, "legacy", "no ad unit id configured");
                OnAdInterstitialFailedToLoad();
                return;
            }

            DebugAds.LogAdEvent("interstitial", "load_start", adUnitId, "legacy");
            InterstitialAd.Load(adUnitId, adRequest, (ad, error) =>
            {
                NotifyInterstitialFinished();
                if (error != null || ad == null)
                {
                    DebugAds.LogAdEvent("interstitial", "load_fail", adUnitId, "legacy", error?.ToString() ?? "null ad");
                    OnAdInterstitialFailedToLoad();
                    return;
                }

                InterstitialAds = ad;
                RegisterInterstitialAd(ad);
                DebugAds.LogAdEvent("interstitial", "load_success", adUnitId, "legacy");
                OnAdInterstitialSuccessToLoad();
            });
        }

        string GetLegacyInterstitialAdUnit()
        {
            var fromTier = InterstitialTierConfig?.ResolveDefaultAdUnitId();
            if (!string.IsNullOrEmpty(fromTier))
                return fromTier;
            return GetInterstitialAdUnit();
        }

        void OnAdInterstitialPaid(AdValue adValue)
        {
            if (UseSequentialInterstitial)
            {
                var cache = _interstitialTierLoader?.ReadyCache;
                var adapter = cache?.Adapter as AdMobInterstitialAdapter;
                if (adapter?.AdObject == null) return;
                SequentialTierAnalytics.LogPaid(
                    "interstitial",
                    cache.AdUnitId,
                    cache.Tier,
                    adValue.Value / 1_000_000d,
                    adValue.CurrencyCode,
                    (int)adValue.Precision);
                HandleAdPaidEvent("interstitial", adValue, adapter.AdObject.GetResponseInfo());
                return;
            }

            if (InterstitialAds == null) return;
            HandleAdPaidEvent("interstitial", adValue, InterstitialAds.GetResponseInfo());
        }
    }
}
#endif
