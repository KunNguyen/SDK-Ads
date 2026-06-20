using System;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Common;

namespace JisSDKAds.Providers.Max.SequentialTier
{
    internal sealed class MaxInterstitialAdapter : ISequentialTierAdAdapter
    {
        string _adUnitId;
        bool _isReady;
        SequentialTierShowHooks _hooks;

        public bool IsReady => _isReady && !string.IsNullOrEmpty(_adUnitId) && MaxSdk.IsInterstitialReady(_adUnitId);

        public void Destroy()
        {
            _isReady = false;
            _adUnitId = null;
        }

        public void Load(string adUnitId, int loadGeneration, int expectedGeneration,
            Action onSuccess, Action<int, string> onFail)
        {
            _isReady = false;
            _adUnitId = adUnitId;

            Action<string, MaxSdkBase.AdInfo> onLoaded = null;
            Action<string, MaxSdkBase.ErrorInfo> onFailed = null;

            onLoaded = (id, info) =>
            {
                if (id != adUnitId || loadGeneration != expectedGeneration) return;
                MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= onLoaded;
                MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= onFailed;
                _isReady = true;
                onSuccess?.Invoke();
            };

            onFailed = (id, error) =>
            {
                if (id != adUnitId || loadGeneration != expectedGeneration) return;
                MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= onLoaded;
                MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= onFailed;
                onFail?.Invoke((int)error.Code, error.Message);
            };

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += onLoaded;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += onFailed;
            MaxSdk.LoadInterstitial(adUnitId);
        }

        public void RegisterShowCallbacks(SequentialTierShowHooks hooks)
        {
            _hooks = hooks;
        }

        public bool TryShow()
        {
            if (!IsReady) return false;

            Action<string, MaxSdkBase.AdInfo> onDisplayed = null;
            Action<string, MaxSdkBase.ErrorInfo, MaxSdkBase.AdInfo> onDisplayFailed = null;
            Action<string, MaxSdkBase.AdInfo> onHidden = null;
            Action<string, MaxSdkBase.AdInfo> onPaid = null;

            var hooks = _hooks;
            var adUnitId = _adUnitId;
            var opened = false;
            var completed = false;

            void Unsubscribe()
            {
                MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= onDisplayed;
                MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= onDisplayFailed;
                MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= onHidden;
                MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= onPaid;
            }

            onDisplayed = (id, info) =>
            {
                if (id != adUnitId || completed || opened) return;
                opened = true;
                // Only unsubscribe onDisplayed here; the rest stay until closed/failed.
                MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= onDisplayed;
                hooks.onOpened?.Invoke();
            };
            onDisplayFailed = (id, error, info) =>
            {
                if (id != adUnitId || completed) return;
                if (opened)
                {
                    // MAX should not report display failure after a successful display.
                    // Ignore that invalid sequence and keep waiting for the hidden event.
                    MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= onDisplayFailed;
                    return;
                }

                completed = true;
                Unsubscribe();
                _isReady = false;
                hooks.onFailed?.Invoke(new SequentialTierShowError { Code = (int)error.Code, Message = error.Message });
            };
            onHidden = (id, info) =>
            {
                if (id != adUnitId || completed) return;
                completed = true;
                Unsubscribe();
                hooks.onClosed?.Invoke();
            };
            onPaid = (id, info) =>
            {
                if (id != adUnitId) return;
                hooks.onPaid?.Invoke(new SequentialTierPaidEvent
                {
                    Revenue = info.Revenue,
                    Currency = "USD",
                    AdUnitId = id,
                    AdSource = info.NetworkName
                });
            };

            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += onDisplayed;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += onDisplayFailed;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += onHidden;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += onPaid;

            _isReady = false;
            MaxSdk.ShowInterstitial(adUnitId);
            return true;
        }
    }
}
