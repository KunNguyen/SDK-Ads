#if UNITY_AD_ADMOB
using JisSDKAds.Core.Interfaces;

namespace JisSDKAds.Providers.AdMob.SequentialTier
{
    /// <summary>
    /// Replaces single-unit interstitial/rewarded with sequential tier ladders when enabled in profile config.
    /// </summary>
    public sealed class SequentialTierAdServiceDecorator : IAdService
    {
        readonly IAdService _inner;

        public SequentialTierAdServiceDecorator(
            IAdService inner,
            IInterstitialAd sequentialInterstitial,
            IRewardedAd sequentialRewarded)
        {
            _inner = inner;
            Interstitial = sequentialInterstitial ?? inner.Interstitial;
            Rewarded = sequentialRewarded ?? inner.Rewarded;
            Banner = inner.Banner;
            AppOpen = inner.AppOpen;
        }

        public string ProviderId => _inner.ProviderId;
        public bool IsInitialized => _inner.IsInitialized;

        public IInterstitialAd Interstitial { get; }
        public IRewardedAd Rewarded { get; }
        public IBannerAd Banner { get; }
        public IAppOpenAd AppOpen { get; }

        public void Initialize(System.Action onSuccess, System.Action<string> onFailure) =>
            _inner.Initialize(onSuccess, onFailure);

        public void SetConsent(bool hasConsent) => _inner.SetConsent(hasConsent);
    }
}
#endif
