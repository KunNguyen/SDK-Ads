using System;
using JisSDKAds.Common;

namespace JisSDKAds.Ads.SequentialTier
{
    /// <summary>
    /// Click-out ads (Play Store / browser) may deliver the reward callback after fullscreen close.
    /// This tracker holds completion until close is known and a short grace window ends.
    /// </summary>
    public sealed class RewardedShowCompletionTracker
    {
        const float LateRewardGraceSeconds = 1.5f;

        bool _fullscreenClosed;
        bool _rewardGranted;
        bool _finished;
        bool _graceScheduled;
        Action _pendingDeliverClosed;

        public bool RewardGranted => _rewardGranted;

        public void Reset()
        {
            _fullscreenClosed = false;
            _rewardGranted = false;
            _finished = false;
            _graceScheduled = false;
            _pendingDeliverClosed = null;
        }

        public void NotifyRewardGranted(Action deliverReward)
        {
            if (_finished || _rewardGranted)
                return;

            _rewardGranted = true;
            deliverReward?.Invoke();
            TryCompleteShow();
        }

        public void NotifyFullscreenClosed(Action deliverClosed)
        {
            if (_finished)
                return;

            _fullscreenClosed = true;
            if (deliverClosed != null)
                _pendingDeliverClosed = deliverClosed;
            TryCompleteShow(deliverClosed);
        }

        void TryCompleteShow(Action deliverClosed = null)
        {
            if (_finished || !_fullscreenClosed)
                return;

            if (_rewardGranted)
            {
                CompleteShow(deliverClosed ?? _pendingDeliverClosed);
                return;
            }

            if (_graceScheduled)
                return;

            _graceScheduled = true;
            EventManager.InvokeDelayed(() =>
            {
                _graceScheduled = false;
                if (_finished)
                    return;

                CompleteShow(deliverClosed);
            }, LateRewardGraceSeconds);
        }

        void CompleteShow(Action deliverClosed)
        {
            if (_finished)
                return;

            _finished = true;
            deliverClosed?.Invoke();
        }
    }
}
