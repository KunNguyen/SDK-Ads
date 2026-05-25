#if UNITY_AD_ADMOB
using GoogleMobileAds.Api;
using JisSDKAds.Ads.InterstitialTier;
using JisSDKAds.Common;
using JisSDKAds.Providers.AdMob.InterstitialTier;
using UnityEngine;

namespace JisSDKAds.Ads
{
    public partial class AdmobMediationController
    {
        TieredInterstitialLoader _tieredInterstitialLoader;

        InterstitialTierConfig TierConfig =>
            m_AdmobAdSetup != null ? m_AdmobAdSetup.InterstitialTierConfig : null;

        bool UseTieredInterstitialLadder =>
            TierConfig != null && TierConfig.enableTieredInterstitial;

        void EnsureTieredLoader()
        {
            if (_tieredInterstitialLoader != null || TierConfig == null) return;

            _tieredInterstitialLoader = new TieredInterstitialLoader(this, TierConfig);
            _tieredInterstitialLoader.SetCallbacks(
                OnAdInterstitialSuccessToLoad,
                OnAdInterstitialFailedToLoad,
                OnCloseInterstitialAd,
                OnAdInterstitialOpening,
                OnAdInterstitialFailToShow,
                OnAdInterstitialPaid);
        }

        void RequestInterstitialLegacy()
        {
            DebugAds.Log("Request interstitial ads (legacy single unit)");

            if (InterstitialAds != null)
            {
                InterstitialAds.Destroy();
                InterstitialAds = null;
            }

            var adRequest = new AdRequest();
            adRequest.Keywords.Add("unity-admob-sample");

            var adUnitId = GetLegacyInterstitialAdUnit();
            if (string.IsNullOrEmpty(adUnitId))
            {
                OnAdInterstitialFailedToLoad();
                return;
            }

            InterstitialAd.Load(adUnitId, adRequest, (ad, error) =>
            {
                if (error != null || ad == null)
                {
                    DebugAds.LogError("interstitial ad failed to load: " + error);
                    OnAdInterstitialFailedToLoad();
                    return;
                }

                DebugAds.Log("Interstitial ad loaded: " + ad.GetResponseInfo());
                InterstitialAds = ad;
                RegisterInterstitialAd(ad);
                OnAdInterstitialSuccessToLoad();
            });
        }

        string GetLegacyInterstitialAdUnit()
        {
            var fromTier = TierConfig?.ResolveDefaultAdUnitId();
            if (!string.IsNullOrEmpty(fromTier))
                return fromTier;
            return GetInterstitialAdUnit();
        }

        void OnAdInterstitialPaid(AdValue adValue)
        {
            var ad = UseTieredInterstitialLadder
                ? _tieredInterstitialLoader?.ReadyCache?.Adapter?.Ad
                : InterstitialAds;
            if (ad == null) return;
            HandleAdPaidEvent("interstitial", adValue, ad.GetResponseInfo());
        }
    }
}
#endif
