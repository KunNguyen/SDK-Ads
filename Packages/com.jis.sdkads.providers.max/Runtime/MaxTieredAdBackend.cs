#if UNITY_AD_MAX
using JisSDKAds.Core.Interfaces;
using JisSDKAds.Core.Tiered.Interfaces;

namespace JisSDKAds.Providers.Max
{
    public class MaxTieredAdBackend : ITieredAdBackend
    {
        public string ProviderName => "Max";

        public IInterstitialAd CreateInterstitial(string unitId) => new MaxInterstitialAdPublic(unitId);

        public IRewardedAd CreateRewarded(string unitId) => new MaxRewardedAdPublic(unitId);
    }

    /// <summary>Public wrapper for tiered inventory (internal MaxInterstitialAd is not accessible cross-assembly).</summary>
    public class MaxInterstitialAdPublic : IInterstitialAd
    {
        readonly MaxInterstitialAd _inner;

        public MaxInterstitialAdPublic(string unitId) => _inner = new MaxInterstitialAd(unitId);

        public bool IsLoaded => _inner.IsLoaded;
        public void Load(System.Action onLoaded = null, System.Action<string> onFailed = null) =>
            _inner.Load(onLoaded, onFailed);

        public void Show(System.Action onShown = null, System.Action onClosed = null, System.Action<string> onFailed = null) =>
            _inner.Show(onShown, onClosed, onFailed);
    }

    public class MaxRewardedAdPublic : IRewardedAd
    {
        readonly MaxRewardedAd _inner;

        public MaxRewardedAdPublic(string unitId) => _inner = new MaxRewardedAd(unitId);

        public bool IsLoaded => _inner.IsLoaded;
        public void Load(System.Action onLoaded = null, System.Action<string> onFailed = null) =>
            _inner.Load(onLoaded, onFailed);

        public void Show(System.Action onRewardEarned = null, System.Action onClosed = null, System.Action<string> onFailed = null) =>
            _inner.Show(onRewardEarned, onClosed, onFailed);
    }
}
#endif
