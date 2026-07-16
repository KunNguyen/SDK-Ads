using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Common;

namespace JisSDKAds.Ads
{
    public partial class MaxMediationController
    {
#if UNITY_AD_MAX
        SequentialTierLoader _rewardedTierLoader;

        SequentialTierConfig RewardedTierConfig =>
            m_MaxAdConfig?.RewardedTierConfig;

        bool UseSequentialRewarded =>
            RewardedTierConfig != null && RewardedTierConfig.enableSequentialLadder;

        internal void ResetRewardedTierLoader()
        {
            _rewardedTierLoader?.Destroy();
            _rewardedTierLoader = null;
        }

        void EnsureRewardedTierLoader()
        {
            if (AdsManager.UsesJisAdsCoreForStandardLoads())
                return;
            if (_rewardedTierLoader != null || RewardedTierConfig == null) return;

            _rewardedTierLoader = new SequentialTierLoader(
                this,
                "max_reward",
                "rewarded",
                AdLoadFormat.Rewarded,
                RewardedTierConfig,
                () => new JisSDKAds.Providers.Max.SequentialTier.MaxRewardedAdapter());

            _rewardedTierLoader.SetCallbacks(
                OnRewardBasedVideoLoaded,
                OnRewardBasedVideoFailedToLoad,
                new SequentialTierShowHooks
                {
                    onClosed = () =>
                    {
                        OnRewardBasedVideoClosed();
                        // Reload for the next impression, but not sooner than the minimum gap after close.
                        AdReloadScheduler.Schedule("max_tier_rew_close", () => _rewardedTierLoader?.Load());
                    },
                    onOpened = OnRewardBasedVideoOpened,
                    onFailed = OnRewardedAdFailedToShow,
                    onRewardGranted = OnRewardBasedVideoRewarded,
                    onPaid = paid =>
                    {
                        SequentialTierAnalytics.LogPaid(
                            "rewarded",
                            paid.AdUnitId,
                            _rewardedTierLoader?.ReadyCache?.Tier ?? AdTier.Fill,
                            paid.Revenue,
                            paid.Currency,
                            paid.Precision);
                        var impression = new ImpressionData
                        {
                            ad_mediation = AdsMediationType.MAX,
                            ad_source = paid.AdSource,
                            ad_unit_name = paid.AdUnitId,
                            ad_format = "REWARDED",
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
