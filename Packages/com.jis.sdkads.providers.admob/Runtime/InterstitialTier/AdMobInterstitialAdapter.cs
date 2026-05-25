#if UNITY_AD_ADMOB
using System;
using GoogleMobileAds.Api;

namespace JisSDKAds.Providers.AdMob.InterstitialTier
{
    /// <summary>Single AdMob interstitial instance with explicit event unsubscribe.</summary>
    internal sealed class AdMobInterstitialAdapter
    {
        InterstitialAd _ad;
        Action _onClosed;
        Action _onOpened;
        Action<AdError> _onFailedToShow;
        Action<AdValue> _onPaid;

        public bool IsReady => _ad != null && _ad.CanShowAd();
        public InterstitialAd Ad => _ad;

        public void Destroy()
        {
            if (_ad == null) return;
            UnregisterEvents(_ad);
            _ad.Destroy();
            _ad = null;
        }

        public void Load(string adUnitId, int loadGeneration, int expectedGeneration,
            Action<InterstitialAd> onSuccess, Action<LoadAdError> onFail)
        {
            Destroy();

            var request = new AdRequest();
            request.Keywords.Add("unity-admob-sample");

            InterstitialAd.Load(adUnitId, request, (ad, error) =>
            {
                // Ignore late callback after timeout / tier advance.
                if (loadGeneration != expectedGeneration)
                {
                    ad?.Destroy();
                    return;
                }

                if (error != null || ad == null)
                {
                    onFail?.Invoke(error);
                    return;
                }

                _ad = ad;
                onSuccess?.Invoke(ad);
            });
        }

        public void RegisterShowCallbacks(
            Action onClosed,
            Action onOpened,
            Action<AdError> onFailedToShow,
            Action<AdValue> onPaid)
        {
            if (_ad == null) return;

            UnregisterEvents(_ad);
            _onClosed = onClosed;
            _onOpened = onOpened;
            _onFailedToShow = onFailedToShow;
            _onPaid = onPaid;

            _ad.OnAdFullScreenContentClosed += HandleClosed;
            _ad.OnAdFullScreenContentOpened += HandleOpened;
            _ad.OnAdFullScreenContentFailed += HandleFailedToShow;
            _ad.OnAdPaid += HandlePaid;
        }

        public bool TryShow()
        {
            if (!IsReady) return false;
            _ad.Show();
            return true;
        }

        void HandleClosed() => _onClosed?.Invoke();
        void HandleOpened() => _onOpened?.Invoke();
        void HandleFailedToShow(AdError error) => _onFailedToShow?.Invoke(error);
        void HandlePaid(AdValue value) => _onPaid?.Invoke(value);

        void UnregisterEvents(InterstitialAd ad)
        {
            if (ad == null) return;
            if (_onClosed != null) ad.OnAdFullScreenContentClosed -= HandleClosed;
            if (_onOpened != null) ad.OnAdFullScreenContentOpened -= HandleOpened;
            if (_onFailedToShow != null) ad.OnAdFullScreenContentFailed -= HandleFailedToShow;
            if (_onPaid != null) ad.OnAdPaid -= HandlePaid;
            _onClosed = null;
            _onOpened = null;
            _onFailedToShow = null;
            _onPaid = null;
        }
    }
}
#endif
