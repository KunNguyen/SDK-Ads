using System;
using GoogleMobileAds.Api;
using JisSDKAds.Ads.SequentialTier;

namespace JisSDKAds.Providers.AdMob.SequentialTier
{
    internal sealed class AdMobInterstitialAdapter : ISequentialTierAdAdapter
    {
        string _adUnitId;
        InterstitialAd _ad;
        SequentialTierShowHooks _hooks;

        public bool IsReady => _ad != null && _ad.CanShowAd();
        public InterstitialAd AdObject => _ad;

        public void Destroy()
        {
            if (_ad == null) return;
            UnregisterEvents(_ad);
            _ad.Destroy();
            _ad = null;
        }

        public void Load(string adUnitId, int loadGeneration, int expectedGeneration,
            Action onSuccess, Action<int, string> onFail)
        {
            Destroy();
            _adUnitId = adUnitId;
            var request = new AdRequest();

            InterstitialAd.Load(adUnitId, request, (ad, error) =>
            {
                var code = error?.GetCode() ?? 0;
                var message = error?.GetMessage();
                AdMobMainThread.Run(() =>
                {
                    if (loadGeneration != expectedGeneration)
                    {
                        ad?.Destroy();
                        return;
                    }

                    if (error != null || ad == null)
                    {
                        onFail?.Invoke(code, message);
                        return;
                    }

                    _ad = ad;
                    onSuccess?.Invoke();
                });
            });
        }

        public void RegisterShowCallbacks(SequentialTierShowHooks hooks)
        {
            if (_ad == null) return;
            UnregisterEvents(_ad);
            _hooks = hooks;
            _ad.OnAdFullScreenContentClosed += HandleClosed;
            _ad.OnAdFullScreenContentOpened += HandleOpened;
            _ad.OnAdFullScreenContentFailed += HandleFailed;
            _ad.OnAdPaid += HandlePaid;
        }

        public bool TryShow()
        {
            if (!IsReady) return false;
            _ad.Show();
            return true;
        }

        void HandleClosed() => AdMobMainThread.Run(() => _hooks.onClosed?.Invoke());
        void HandleOpened() => AdMobMainThread.Run(() => _hooks.onOpened?.Invoke());
        void HandleFailed(AdError e)
        {
            var error = new SequentialTierShowError { Code = e.GetCode(), Message = e.GetMessage() };
            AdMobMainThread.Run(() => _hooks.onFailed?.Invoke(error));
        }
        // Revenue (ILAR) — keep immediate, do NOT marshal to the Unity main thread.
        void HandlePaid(AdValue v)
        {
            var adapter = _ad?.GetResponseInfo()?.GetLoadedAdapterResponseInfo();
            _hooks.onPaid?.Invoke(new SequentialTierPaidEvent
            {
                Revenue = (double)v.Value / 1_000_000,
                Currency = v.CurrencyCode,
                Precision = (int)v.Precision,
                AdUnitId = _adUnitId,
                AdSource = adapter?.AdSourceName ?? "",
                AdSourceId = adapter?.AdSourceId ?? "",
                AdSourceInstanceId = adapter?.AdSourceInstanceId ?? ""
            });
        }

        void UnregisterEvents(InterstitialAd ad)
        {
            ad.OnAdFullScreenContentClosed -= HandleClosed;
            ad.OnAdFullScreenContentOpened -= HandleOpened;
            ad.OnAdFullScreenContentFailed -= HandleFailed;
            ad.OnAdPaid -= HandlePaid;
        }
    }
}
