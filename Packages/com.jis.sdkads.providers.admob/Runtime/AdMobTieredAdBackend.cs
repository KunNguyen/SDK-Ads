#if UNITY_AD_ADMOB
using JisSDKAds.Core.Interfaces;
using JisSDKAds.Core.Tiered.Interfaces;

namespace JisSDKAds.Providers.AdMob
{
    public class AdMobTieredAdBackend : ITieredAdBackend
    {
        public string ProviderName => "AdMob";

        public IInterstitialAd CreateInterstitial(string unitId) => new AdMobInterstitialAdPublic(unitId);

        public IRewardedAd CreateRewarded(string unitId) => new AdMobRewardedAdPublic(unitId);
    }

    public class AdMobInterstitialAdPublic : IInterstitialAd
    {
        readonly AdMobInterstitialAd _inner;

        public AdMobInterstitialAdPublic(string unitId) => _inner = new AdMobInterstitialAd(unitId);

        public bool IsLoaded => _inner.IsLoaded;
        public void Load(System.Action onLoaded = null, System.Action<string> onFailed = null) =>
            _inner.Load(onLoaded, onFailed);

        public void Show(System.Action onShown = null, System.Action onClosed = null, System.Action<string> onFailed = null) =>
            _inner.Show(onShown, onClosed, onFailed);
    }

    public class AdMobRewardedAdPublic : IRewardedAd
    {
        readonly AdMobRewardedAd _inner;

        public AdMobRewardedAdPublic(string unitId) => _inner = new AdMobRewardedAd(unitId);

        public bool IsLoaded => _inner.IsLoaded;
        public void Load(System.Action onLoaded = null, System.Action<string> onFailed = null) =>
            _inner.Load(onLoaded, onFailed);

        public void Show(System.Action onRewardEarned = null, System.Action onClosed = null, System.Action<string> onFailed = null) =>
            _inner.Show(onRewardEarned, onClosed, onFailed);
    }
}
#endif
