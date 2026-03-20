using System;

namespace JisSDKAds.Core.Interfaces
{
    /// <summary>
    /// Interface for rewarded video ads.
    /// </summary>
    public interface IRewardedAd
    {
        bool IsLoaded { get; }

        void Load(Action onLoaded = null, Action<string> onFailed = null);
        void Show(Action onRewardEarned = null, Action onClosed = null, Action<string> onFailed = null);
    }
}
