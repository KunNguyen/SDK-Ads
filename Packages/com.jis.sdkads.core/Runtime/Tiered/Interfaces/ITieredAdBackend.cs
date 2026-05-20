using JisSDKAds.Core.Interfaces;

namespace JisSDKAds.Core.Tiered.Interfaces
{
    /// <summary>
    /// Creates network-specific ad instances for a given unit ID.
    /// Implemented by MAX / AdMob provider packages.
    /// </summary>
    public interface ITieredAdBackend
    {
        string ProviderName { get; }
        IInterstitialAd CreateInterstitial(string unitId);
        IRewardedAd CreateRewarded(string unitId);
    }
}
