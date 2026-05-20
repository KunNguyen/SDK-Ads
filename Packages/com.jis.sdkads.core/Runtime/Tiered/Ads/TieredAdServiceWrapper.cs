using JisSDKAds.Core.Interfaces;
using JisSDKAds.Core.Tiered.Config;
using JisSDKAds.Core.Tiered.Services;

namespace JisSDKAds.Core.Tiered.Ads
{
    /// <summary>
    /// Decorates IAdService with tiered interstitial/rewarded when enabled. Banner/AppOpen unchanged.
    /// </summary>
    public class TieredAdServiceWrapper : IAdService
    {
        readonly IAdService _inner;
        readonly TieredAdsConfig _config;
        readonly TieredAdsManager _tieredManager;

        public TieredAdServiceWrapper(IAdService inner, TieredAdsConfig config, TieredAdsManager tieredManager)
        {
            _inner = inner;
            _config = config;
            _tieredManager = tieredManager;

            Interstitial = _config.IsTieredEnabledFor(Models.AdsFormatType.Interstitial)
                ? new TieredInterstitialAd(_tieredManager)
                : inner.Interstitial;

            Rewarded = _config.IsTieredEnabledFor(Models.AdsFormatType.Rewarded)
                ? new TieredRewardedAd(_tieredManager)
                : inner.Rewarded;

            Banner = inner.Banner;
            AppOpen = inner.AppOpen;
        }

        public string ProviderId => _inner.ProviderId;
        public bool IsInitialized => _inner.IsInitialized;

        public IInterstitialAd Interstitial { get; }
        public IRewardedAd Rewarded { get; }
        public IBannerAd Banner { get; }
        public IAppOpenAd AppOpen { get; }

        public void Initialize(System.Action onSuccess, System.Action<string> onFailure)
        {
            _inner.Initialize(
                () =>
                {
                    _tieredManager.SetProviderReady(true);
                    onSuccess?.Invoke();
                },
                err =>
                {
                    _tieredManager.SetProviderReady(false);
                    onFailure?.Invoke(err);
                });
        }

        public void SetConsent(bool hasConsent) => _inner.SetConsent(hasConsent);
    }
}
