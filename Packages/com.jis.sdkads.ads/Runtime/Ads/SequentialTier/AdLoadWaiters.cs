using System;
using System.Collections.Generic;

namespace JisSDKAds.Ads.SequentialTier
{
    /// <summary>
    /// Load callbacks of every caller waiting on one in-flight interstitial/rewarded load.
    /// A Load() that joins a running load must still get its result, never be dropped.
    /// </summary>
    public sealed class AdLoadWaiters
    {
        readonly List<Action> _onLoaded = new List<Action>();
        readonly List<Action<string>> _onFailed = new List<Action<string>>();

        public void Add(Action onLoaded, Action<string> onFailed)
        {
            if (onLoaded != null)
                _onLoaded.Add(onLoaded);
            if (onFailed != null)
                _onFailed.Add(onFailed);
        }

        public void NotifyLoaded()
        {
            var callbacks = _onLoaded.ToArray();
            Clear();
            foreach (var cb in callbacks)
                cb();
        }

        public void NotifyFailed(string error)
        {
            var callbacks = _onFailed.ToArray();
            Clear();
            foreach (var cb in callbacks)
                cb(error);
        }

        void Clear()
        {
            _onLoaded.Clear();
            _onFailed.Clear();
        }
    }
}
