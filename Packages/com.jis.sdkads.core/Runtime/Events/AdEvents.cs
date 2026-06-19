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
        public static event Action<AdFailureInfo> OnInterstitialFailed;
        public static event Action<AdFormat, int> OnInterstitialShown;
        public static event Action<AdFormat> OnInterstitialClosed;

        public static void RaiseInterstitialLoaded(AdFormat format, string providerId) =>
            OnInterstitialLoaded?.Invoke(format, providerId);

        public static void RaiseInterstitialFailed(AdFailureInfo failure) =>
            OnInterstitialFailed?.Invoke(failure);

        public static void RaiseInterstitialFailed(AdFormat format, string providerId, string error) =>
            RaiseInterstitialFailed(AdFailureInfo.ForAd(format, providerId, error));

        public static void RaiseInterstitialShown(AdFormat format, int showAttemptId = 0) =>
            OnInterstitialShown?.Invoke(format, showAttemptId);

        public static void RaiseInterstitialClosed(AdFormat format) =>
            OnInterstitialClosed?.Invoke(format);

        #endregion

        #region Rewarded

        public static event Action<AdFormat, string> OnRewardedLoaded;
        public static event Action<AdFailureInfo> OnRewardedFailed;
        public static event Action<AdFormat> OnRewardedShown;
        public static event Action<AdFormat> OnRewardEarned;
        public static event Action<AdFormat> OnRewardedClosed;

        public static void RaiseRewardedLoaded(AdFormat format, string providerId) =>
            OnRewardedLoaded?.Invoke(format, providerId);

        public static void RaiseRewardedFailed(AdFailureInfo failure) =>
            OnRewardedFailed?.Invoke(failure);

        public static void RaiseRewardedFailed(AdFormat format, string providerId, string error) =>
            RaiseRewardedFailed(AdFailureInfo.ForAd(format, providerId, error));

        public static void RaiseRewardedShown(AdFormat format) =>
            OnRewardedShown?.Invoke(format);

        public static void RaiseRewardEarned(AdFormat format) =>
            OnRewardEarned?.Invoke(format);

        public static void RaiseRewardedClosed(AdFormat format) =>
            OnRewardedClosed?.Invoke(format);

        #endregion

        #region Banner

        public static event Action<AdFormat, string> OnBannerLoaded;
        public static event Action<AdFailureInfo> OnBannerFailed;
        public static event Action<AdFormat> OnBannerShown;
        public static event Action<AdFormat> OnBannerHidden;

        public static void RaiseBannerLoaded(AdFormat format, string providerId) =>
            OnBannerLoaded?.Invoke(format, providerId);

        public static void RaiseBannerFailed(AdFailureInfo failure) =>
            OnBannerFailed?.Invoke(failure);

        public static void RaiseBannerFailed(AdFormat format, string providerId, string error) =>
            RaiseBannerFailed(AdFailureInfo.ForAd(format, providerId, error));

        public static void RaiseBannerShown(AdFormat format) =>
            OnBannerShown?.Invoke(format);

        public static void RaiseBannerHidden(AdFormat format) =>
            OnBannerHidden?.Invoke(format);

        #endregion

        #region General

        public static event Action<string> OnProviderInitialized;
        public static event Action<AdFailureInfo> OnProviderFailed;

        public static void RaiseProviderInitialized(string providerId) =>
            OnProviderInitialized?.Invoke(providerId);

        public static void RaiseProviderFailed(AdFailureInfo failure) =>
            OnProviderFailed?.Invoke(failure);

        public static void RaiseProviderFailed(string providerId, string error) =>
            RaiseProviderFailed(AdFailureInfo.ForProvider(providerId, error));

        #endregion
    }
}
