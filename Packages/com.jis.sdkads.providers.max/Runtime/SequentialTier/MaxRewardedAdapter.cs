using System;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Common;

namespace JisSDKAds.Providers.Max.SequentialTier
{
    internal sealed class MaxRewardedAdapter : ISequentialTierAdAdapter
    {
        string _adUnitId;
        bool _isReady;
        SequentialTierShowHooks _hooks;
        readonly RewardedShowCompletionTracker _showCompletion = new RewardedShowCompletionTracker();

        public bool IsReady => _isReady && !string.IsNullOrEmpty(_adUnitId) && MaxSdk.IsRewardedAdReady(_adUnitId);

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
                MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= onLoaded;
                MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= onFailed;
                _isReady = true;
                onSuccess?.Invoke();
            };

            onFailed = (id, error) =>
            {
                if (id != adUnitId || loadGeneration != expectedGeneration) return;
                MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= onLoaded;
                MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= onFailed;
                onFail?.Invoke((int)error.Code, error.Message);
            };

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += onLoaded;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += onFailed;
            MaxSdk.LoadRewardedAd(adUnitId);
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
            Action<string, MaxSdkBase.Reward, MaxSdkBase.AdInfo> onRewarded = null;
            Action<string, MaxSdkBase.AdInfo> onPaid = null;

            var hooks = _hooks;
            _showCompletion.Reset();

            onDisplayed = (id, info) =>
            {
                if (id != _adUnitId) return;
                hooks.onOpened?.Invoke();
            };
            onDisplayFailed = (id, error, info) =>
            {
                if (id != _adUnitId) return;
                Unsubscribe();
                _isReady = false;
                hooks.onFailed?.Invoke(new SequentialTierShowError { Code = (int)error.Code, Message = error.Message });
            };
            onRewarded = (id, reward, info) =>
            {
                if (id != _adUnitId) return;
                _showCompletion.NotifyRewardGranted(() => hooks.onRewardGranted?.Invoke());
            };
            onHidden = (id, info) =>
            {
                if (id != _adUnitId) return;
                UnsubscribeExceptReward();
                _showCompletion.NotifyFullscreenClosed(() =>
                {
                    if (!_showCompletion.RewardGranted)
                        DebugAds.Log("[MAX][Rewarded][Tier] show_closed_without_reward");
                    Unsubscribe();
                    hooks.onClosed?.Invoke();
                });
            };
            onPaid = (id, info) =>
            {
                if (id != _adUnitId) return;
                hooks.onPaid?.Invoke(new SequentialTierPaidEvent
                {
                    Revenue = info.Revenue,
                    Currency = "USD",
                    AdUnitId = id,
                    AdSource = info.NetworkName
                });
            };

            void Unsubscribe()
            {
                MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= onDisplayed;
                MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= onDisplayFailed;
                MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= onHidden;
                MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= onRewarded;
                MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= onPaid;
            }

            void UnsubscribeExceptReward()
            {
                MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= onDisplayed;
                MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= onDisplayFailed;
                MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= onHidden;
                MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= onPaid;
            }

            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += onDisplayed;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += onDisplayFailed;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += onHidden;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += onRewarded;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += onPaid;

            _isReady = false;
            MaxSdk.ShowRewardedAd(_adUnitId);
            return true;
        }
    }
}
