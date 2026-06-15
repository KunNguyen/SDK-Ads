#if UNITY_AD_MAX
using System;
using System.Collections.Generic;
using JisSDKAds.Common;

namespace JisSDKAds.Providers.Max
{
    public static class MaxSdkInitializer
    {
        enum InitState
        {
            NotStarted,
            Initializing,
            Ready
        }

        static InitState _state = InitState.NotStarted;
        static readonly List<Action<bool>> PendingCallbacks = new List<Action<bool>>();

        public static bool IsReady => _state == InitState.Ready;
        public static bool IsInitializing => _state == InitState.Initializing;

        public static void EnsureInitializedEarly(string sdkKey) =>
            EnsureInitialized(sdkKey, null);

        public static void EnsureInitialized(string sdkKey, Action<bool> onComplete)
        {
            if (onComplete != null)
                PendingCallbacks.Add(onComplete);

            if (_state == InitState.Ready)
            {
                FlushCallbacks(true);
                return;
            }

            if (_state == InitState.Initializing)
                return;

            if (string.IsNullOrWhiteSpace(sdkKey))
            {
                DebugAds.LogWarning("[MAX] SDK key is empty. MAX initialization skipped.");
                FlushCallbacks(false);
                return;
            }

            _state = InitState.Initializing;
            MaxSdk.SetSdkKey(sdkKey);
            MaxSdkCallbacks.OnSdkInitializedEvent -= OnSdkInitialized;
            MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInitialized;
            MaxSdk.InitializeSdk();
        }

        static void OnSdkInitialized(MaxSdkBase.SdkConfiguration _)
        {
            MaxSdkCallbacks.OnSdkInitializedEvent -= OnSdkInitialized;
            _state = InitState.Ready;
            DebugAds.LogSdkInit("MAX", "MaxSdk.InitializeSdk", true);
            FlushCallbacks(true);
        }

        static void FlushCallbacks(bool success)
        {
            if (PendingCallbacks.Count == 0)
                return;

            var callbacks = PendingCallbacks.ToArray();
            PendingCallbacks.Clear();
            foreach (var callback in callbacks)
                callback?.Invoke(success);
        }
    }
}
#endif
