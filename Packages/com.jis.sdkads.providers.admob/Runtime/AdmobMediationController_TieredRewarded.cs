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
        SequentialTierLoader _rewardedTierLoader;

        SequentialTierConfig RewardedTierConfig =>
            m_AdmobAdSetup?.RewardedTierConfig;

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
            if (_rewardedTierLoader != null || RewardedTierConfig == null)
                return;

            _rewardedTierLoader = new SequentialTierLoader(
                this,
                "reward",
                "rewarded",
                AdLoadFormat.Rewarded,
                RewardedTierConfig,
                () => new AdMobRewardedAdapter());

            _rewardedTierLoader.SetCallbacks(
                OnRewardBasedVideoLoaded,
                OnRewardBasedVideoFailedToLoad,
                new SequentialTierShowHooks
                {
                    onClosed = () =>
                    {
                        OnRewardBasedVideoClosed();
                        _rewardedTierLoader?.Load();
                    },
                    onOpened = OnRewardBasedVideoOpened,
                    onFailed = err => OnRewardedAdFailedToShow(null),
                    onPaid = _ => { /* paid handled via OnAdRewardedAdPaid registered on AdMob ad object */ },
                    onRewardGranted = OnRewardBasedVideoRewarded
                });
        }

        void RequestRewardVideoLegacy() =>
            RunRewardedLoad(ExecuteRequestRewardVideoLegacy);

        void ExecuteRequestRewardVideoLegacy()
        {
            if (RewardVideoAds != null)
                DestroyRewardedAd();

            var adUnitId = GetLegacyRewardedAdUnit();
            DebugAds.Log("RewardedVideoAd ADMOB Reload ID " + adUnitId);
            if (string.IsNullOrEmpty(adUnitId))
            {
                NotifyRewardedFinished();
                OnRewardBasedVideoFailedToLoad();
                return;
            }

            var adRequest = new AdRequest();
            RewardedAd.Load(adUnitId, adRequest, (ad, error) =>
            {
                NotifyRewardedFinished();
                if (error != null || ad == null)
                {
                    DebugAds.LogError("Rewarded ad failed to load: " + error);
                    OnRewardBasedVideoFailedToLoad();
                    return;
                }

                RewardVideoAds = ad;
                RegisterRewardAdEvent(ad);
                OnRewardBasedVideoLoaded();
            });
        }

        string GetLegacyRewardedAdUnit()
        {
            var fromTier = RewardedTierConfig?.ResolveDefaultAdUnitId();
            if (!string.IsNullOrEmpty(fromTier))
                return fromTier;
            return GetRewardedAdID();
        }

        void OnAdRewardedAdPaid(AdValue adValue)
        {
            if (UseSequentialRewarded)
            {
                var cache = _rewardedTierLoader?.ReadyCache;
                var adapter = cache?.Adapter as AdMobRewardedAdapter;
                if (adapter?.AdObject == null) return;
                SequentialTierAnalytics.LogPaid(
                    "rewarded",
                    cache.AdUnitId,
                    cache.Tier,
                    adValue.Value / 1_000_000d,
                    adValue.CurrencyCode,
                    (int)adValue.Precision);
                HandleAdPaidEvent("rewarded", adValue, adapter.AdObject.GetResponseInfo());
                return;
            }

            if (RewardVideoAds == null) return;
            HandleAdPaidEvent("rewarded", adValue, RewardVideoAds.GetResponseInfo());
        }
    }
}
#endif
