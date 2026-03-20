using System;

namespace JisSDKAds.Core.Interfaces
{
    /// <summary>
    /// Interface for banner ads.
    /// </summary>
    public interface IBannerAd
    {
        bool IsLoaded { get; }
        bool IsVisible { get; }

        void Load(Action onLoaded = null, Action<string> onFailed = null);
        void Show(Action onShown = null, Action<string> onFailed = null);
        void Hide();
        void Destroy();
    }
}
