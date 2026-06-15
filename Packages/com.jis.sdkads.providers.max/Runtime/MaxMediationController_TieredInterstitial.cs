using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Common;

namespace JisSDKAds.Ads
{
    public partial class MaxMediationController
    {
#if UNITY_AD_MAX
        SequentialTierLoader _interstitialTierLoader;

        SequentialTierConfig InterstitialTierConfig =>
            m_MaxAdConfig?.InterstitialTierConfig;

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
                "max_int",
                "interstitial",
                AdLoadFormat.Interstitial,
                InterstitialTierConfig,
                () => new JisSDKAds.Providers.Max.SequentialTier.MaxInterstitialAdapter());

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
                    onFailed = OnAdInterstitialFailToShow,
                    onPaid = paid =>
                    {
                        SequentialTierAnalytics.LogPaid(
                            "interstitial",
                            paid.AdUnitId,
                            _interstitialTierLoader?.ReadyCache?.Tier ?? AdTier.Fill,
                            paid.Revenue,
                            paid.Currency,
                            paid.Precision);
                        var impression = new ImpressionData
                        {
                            ad_mediation = AdsMediationType.MAX,
                            ad_source = paid.AdSource,
                            ad_unit_name = paid.AdUnitId,
                            ad_format = "INTERSTITIAL",
                            ad_revenue = paid.Revenue,
                            ad_currency = paid.Currency
                        };
                        AdRevenuePaidCallback?.Invoke(impression);
                    }
                });
        }
#endif
    }
}
