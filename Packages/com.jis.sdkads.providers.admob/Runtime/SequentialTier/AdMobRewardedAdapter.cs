using System;
using GoogleMobileAds.Api;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Common;

namespace JisSDKAds.Providers.AdMob.SequentialTier
{
    internal sealed class AdMobRewardedAdapter : ISequentialTierAdAdapter
    {
        string _adUnitId;
        RewardedAd _ad;
        SequentialTierShowHooks _hooks;
        readonly RewardedShowCompletionTracker _showCompletion = new RewardedShowCompletionTracker();

        public bool IsReady => _ad != null && _ad.CanShowAd();
        public RewardedAd AdObject => _ad;

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
            request.Keywords.Add("unity-admob-sample");

            RewardedAd.Load(adUnitId, request, (ad, error) =>
            {
                if (loadGeneration != expectedGeneration)
                {
                    ad?.Destroy();
                    return;
                }

                if (error != null || ad == null)
                {
                    onFail?.Invoke(error?.GetCode() ?? 0, error?.GetMessage());
                    return;
                }

                _ad = ad;
                onSuccess?.Invoke();
            });
        }

        public void RegisterShowCallbacks(SequentialTierShowHooks hooks)
        {
            if (_ad == null) return;
            UnregisterEvents(_ad);
            _hooks = hooks;
            _showCompletion.Reset();
            _ad.OnAdFullScreenContentClosed += HandleClosed;
            _ad.OnAdFullScreenContentOpened += HandleOpened;
            _ad.OnAdFullScreenContentFailed += HandleFailed;
            _ad.OnAdPaid += HandlePaid;
        }

        public bool TryShow()
        {
            if (!IsReady) return false;
            _showCompletion.Reset();
            _ad.Show(_ => _showCompletion.NotifyRewardGranted(() => _hooks.onRewardGranted?.Invoke()));
            return true;
        }

        void HandleClosed() =>
            _showCompletion.NotifyFullscreenClosed(() =>
            {
                if (!_showCompletion.RewardGranted)
                    DebugAds.Log("[AdMob][Rewarded][Tier] show_closed_without_reward");
                _hooks.onClosed?.Invoke();
            });
        void HandleOpened() => _hooks.onOpened?.Invoke();
        void HandleFailed(AdError e) => _hooks.onFailed?.Invoke(
            new SequentialTierShowError { Code = e.GetCode(), Message = e.GetMessage() });
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

        void UnregisterEvents(RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed -= HandleClosed;
            ad.OnAdFullScreenContentOpened -= HandleOpened;
            ad.OnAdFullScreenContentFailed -= HandleFailed;
            ad.OnAdPaid -= HandlePaid;
        }
    }
}
