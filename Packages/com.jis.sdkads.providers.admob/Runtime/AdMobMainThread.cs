#if UNITY_AD_ADMOB
using System;
using System.Threading;
using GoogleMobileAds.Common;
using UnityEngine;

namespace JisSDKAds.Providers.AdMob
{
    /// <summary>
    /// Marshals GoogleMobileAds ad-event callbacks onto the Unity main thread.
    /// AdMob raises ad events on a background (JNI) thread; touching Unity engine state from there
    /// corrupts the native DelayedCallManager and crashes with SIGSEGV in __tree_remove. Wrap every
    /// non-revenue callback body with <see cref="Run"/>.
    /// Revenue/ILAR callbacks (OnAdPaid) must NOT be wrapped — keep them immediate to avoid the
    /// reporting discrepancy caused by the Unity Update loop delaying (and dropping on app exit) events.
    /// </summary>
    internal static class AdMobMainThread
    {
        static int _mainThreadId;
        static SynchronizationContext _mainContext;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CaptureMainThread()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _mainContext = SynchronizationContext.Current;
        }

        public static void Run(Action action)
        {
            if (action == null)
                return;

            // Already on the main thread (e.g. editor / synchronous paths): run immediately,
            // avoiding a frame delay. Otherwise marshal onto the Unity main thread.
            if (IsOnMainThread())
            {
                action();
                return;
            }

            MobileAdsEventExecutor.ExecuteInUpdate(action);
        }

        static bool IsOnMainThread()
        {
            // GoogleMobileAds raises ad events on a background JNI thread. IL2CPP attaches that
            // native thread to the runtime and can hand it a ManagedThreadId that collides with the
            // captured main-thread id, so ManagedThreadId alone is NOT a reliable main-thread check
            // (a false positive runs the action on the JNI thread and crashes the native
            // DelayedCallManager in StopCoroutine/StartCoroutine). Unity installs a
            // SynchronizationContext only on the main thread; attached JNI threads never have it, so
            // this is the authoritative signal. Fall back to the id only if the context is unknown.
            if (_mainContext != null)
                return SynchronizationContext.Current == _mainContext;

            return _mainThreadId != 0 && Thread.CurrentThread.ManagedThreadId == _mainThreadId;
        }
    }
}
#endif
