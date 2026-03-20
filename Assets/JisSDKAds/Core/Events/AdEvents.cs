using System;
using JisSDKAds.Core.Models;

namespace JisSDKAds.Core.Events
{
    /// <summary>
    /// Centralized ad events. Subscribe to these for unified ad lifecycle handling.
    /// </summary>
    public static class AdEvents
    {
        #region Interstitial

        public static event Action<AdFormat, string> OnInterstitialLoaded;
        public static event Action<AdFormat, string> OnInterstitialFailed;
        public static event Action<AdFormat> OnInterstitialShown;
        public static event Action<AdFormat> OnInterstitialClosed;

        public static void RaiseInterstitialLoaded(AdFormat format, string providerId) => OnInterstitialLoaded?.Invoke(format, providerId);
        public static void RaiseInterstitialFailed(AdFormat format, string error) => OnInterstitialFailed?.Invoke(format, error);
        public static void RaiseInterstitialShown(AdFormat format) => OnInterstitialShown?.Invoke(format);
        public static void RaiseInterstitialClosed(AdFormat format) => OnInterstitialClosed?.Invoke(format);

        #endregion

        #region Rewarded

        public static event Action<AdFormat, string> OnRewardedLoaded;
        public static event Action<AdFormat, string> OnRewardedFailed;
        public static event Action<AdFormat> OnRewardedShown;
        public static event Action<AdFormat> OnRewardEarned;
        public static event Action<AdFormat> OnRewardedClosed;

        public static void RaiseRewardedLoaded(AdFormat format, string providerId) => OnRewardedLoaded?.Invoke(format, providerId);
        public static void RaiseRewardedFailed(AdFormat format, string error) => OnRewardedFailed?.Invoke(format, error);
        public static void RaiseRewardedShown(AdFormat format) => OnRewardedShown?.Invoke(format);
        public static void RaiseRewardEarned(AdFormat format) => OnRewardEarned?.Invoke(format);
        public static void RaiseRewardedClosed(AdFormat format) => OnRewardedClosed?.Invoke(format);

        #endregion

        #region Banner

        public static event Action<AdFormat, string> OnBannerLoaded;
        public static event Action<AdFormat, string> OnBannerFailed;
        public static event Action<AdFormat> OnBannerShown;
        public static event Action<AdFormat> OnBannerHidden;

        public static void RaiseBannerLoaded(AdFormat format, string providerId) => OnBannerLoaded?.Invoke(format, providerId);
        public static void RaiseBannerFailed(AdFormat format, string error) => OnBannerFailed?.Invoke(format, error);
        public static void RaiseBannerShown(AdFormat format) => OnBannerShown?.Invoke(format);
        public static void RaiseBannerHidden(AdFormat format) => OnBannerHidden?.Invoke(format);

        #endregion

        #region General

        public static event Action<string> OnProviderInitialized;
        public static event Action<string, string> OnProviderFailed;

        public static void RaiseProviderInitialized(string providerId) => OnProviderInitialized?.Invoke(providerId);
        public static void RaiseProviderFailed(string providerId, string error) => OnProviderFailed?.Invoke(providerId, error);

        #endregion
    }
}
