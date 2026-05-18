using System;

namespace JisSDKAds.Core.Interfaces
{
  /// <summary>
  /// App open ad (cold start / resume). Implemented per provider where supported.
  /// </summary>
  public interface IAppOpenAd
  {
    bool IsLoaded { get; }

    void Load(Action onLoaded = null, Action<string> onFailed = null);
    void Show(Action onShown = null, Action onClosed = null, Action<string> onFailed = null);
  }
}
