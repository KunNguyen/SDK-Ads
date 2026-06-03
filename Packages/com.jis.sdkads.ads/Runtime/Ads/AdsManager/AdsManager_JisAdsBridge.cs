using JisSDKAds.Common;
using JisSDKAds.Firebase;
using UnityEngine;
using UnityEngine.Events;

namespace JisSDKAds.Ads
{
    /// <summary>
    /// When <see cref="JisAds"/> owns init, legacy unit managers are not bootstrapped but game code
    /// still calls <see cref="AdsManager"/> show APIs — route to Core when available.
    /// </summary>
    public partial class AdsManager
    {
        static JisAds ActiveJisAds => JisAds.Instance;

        static bool ShouldRouteStandardFormatsToJisAds()
        {
            var jis = ActiveJisAds;
            return jis != null && jis.UseCoreForStandardFormats;
        }

        /// <summary>When true, JisAds Core owns startup preload; legacy AutoLoad is skipped.</summary>
        public static bool UsesJisAdsCoreForStandardLoads() => ShouldRouteStandardFormatsToJisAds();

        /// <summary>Called by <see cref="JisAds"/> after Core is ready so legacy IsReady checks pass.</summary>
        public void NotifyJisAdsCoreReady()
        {
            IsReady = true;
            IsUpdateRemoteConfigSuccess = FirebaseManager.Instance != null &&
                                          FirebaseManager.Instance.IsRemoteConfigReady;

            if (RewardAdManager != null)
                RewardAdManager.IsReady = true;
            if (BannerAdManager != null)
                BannerAdManager.IsReady = true;
            if (InterstitialAdManager != null)
                InterstitialAdManager.IsReady = true;

            DebugAds.LogSdkInit("AdsManager", "Legacy bridge ready (JisAds Core)", true, null);
        }

        bool TryShowRewardVideoViaJisAds(
            string rewardedPlacement,
            UnityAction successCallback,
            UnityAction<bool> closedCallback,
            UnityAction failedCallback)
        {
            if (!ShouldRouteStandardFormatsToJisAds())
                return false;

            ActiveJisAds.ShowRewardVideo(rewardedPlacement, successCallback, closedCallback, failedCallback);
            return true;
        }

        bool TryShowInterstitialViaJisAds(
            string interstitialPlacement,
            UnityAction closedCallback,
            UnityAction showSuccessCallback,
            UnityAction showFailCallback)
        {
            if (!ShouldRouteStandardFormatsToJisAds())
                return false;

            ActiveJisAds.ShowInterstitial(
                interstitialPlacement,
                closedCallback,
                showSuccessCallback,
                showFailCallback);
            return true;
        }

        bool TryRequestBannerViaJisAds()
        {
            if (!ShouldRouteStandardFormatsToJisAds())
                return false;

            ActiveJisAds.ShowBannerAds();
            return true;
        }

        bool TryQueryRewardedLoadedFromJisAds(out bool loaded)
        {
            loaded = false;
            if (!ShouldRouteStandardFormatsToJisAds())
                return false;

            loaded = ActiveJisAds.IsRewardedVideoLoaded();
            return true;
        }

        bool TryQueryInterstitialLoadedFromJisAds(out bool loaded)
        {
            loaded = false;
            if (!ShouldRouteStandardFormatsToJisAds())
                return false;

            loaded = ActiveJisAds.IsInterstitialAdLoaded();
            return true;
        }

        bool TryQueryBannerLoadedFromJisAds(out bool loaded)
        {
            loaded = false;
            if (!ShouldRouteStandardFormatsToJisAds())
                return false;

            loaded = ActiveJisAds.IsBannerAdLoaded();
            return true;
        }
    }
}
