using System;

namespace JisSDKAds.Core.Interfaces
{
    /// <summary>
    /// Root interface for an ad network provider.
    /// Implementations: MaxAdProvider, AdMobProvider.
    /// </summary>
    public interface IAdService
    {
        string ProviderId { get; }
        bool IsInitialized { get; }

        void Initialize(Action onSuccess, Action<string> onFailure);
        void SetConsent(bool hasConsent);

        IInterstitialAd Interstitial { get; }
        IRewardedAd Rewarded { get; }
        IBannerAd Banner { get; }
        IAppOpenAd AppOpen { get; }
    }
}
