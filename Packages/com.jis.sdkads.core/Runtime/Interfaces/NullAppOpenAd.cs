using System;

namespace JisSDKAds.Core.Interfaces
{
  /// <summary>Stub when provider does not support app open ads.</summary>
  public sealed class NullAppOpenAd : IAppOpenAd
  {
    public static readonly NullAppOpenAd Instance = new NullAppOpenAd();

    public bool IsLoaded => false;

    public void Load(Action onLoaded = null, Action<string> onFailed = null) =>
      onFailed?.Invoke("App open not supported by this provider");

    public void Show(Action onShown = null, Action onClosed = null, Action<string> onFailed = null) =>
      onFailed?.Invoke("App open not supported by this provider");
  }
}
