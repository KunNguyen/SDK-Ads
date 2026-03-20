using System;

namespace JisSDKAds.Core.Interfaces
{
    /// <summary>
    /// Interface for interstitial (full-screen) ads.
    /// </summary>
    public interface IInterstitialAd
    {
        bool IsLoaded { get; }

        void Load(Action onLoaded = null, Action<string> onFailed = null);
        void Show(Action onShown = null, Action onClosed = null, Action<string> onFailed = null);
    }
}
