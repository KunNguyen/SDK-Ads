#if UNITY_AD_ADMOB
using JisSDKAds.Core.Interfaces;

namespace JisSDKAds.Providers.AdMob.SequentialTier
{
    /// <summary>
    /// Replaces single-unit interstitial with sequential tier ladder; other formats unchanged.
    /// </summary>
    public sealed class SequentialTierAdServiceDecorator : IAdService
    {
        readonly IAdService _inner;

        public SequentialTierAdServiceDecorator(IAdService inner, SequentialTierInterstitialAd sequentialInterstitial)
        {
            _inner = inner;
            Interstitial = sequentialInterstitial;
            Rewarded = inner.Rewarded;
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
