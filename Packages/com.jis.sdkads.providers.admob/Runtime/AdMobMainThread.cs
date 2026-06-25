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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CaptureMainThread() => _mainThreadId = Thread.CurrentThread.ManagedThreadId;

        public static void Run(Action action)
        {
            if (action == null)
                return;

            // Already on the main thread (e.g. editor / synchronous paths): run immediately,
            // avoiding a frame delay. GoogleMobileAds 10.x no longer exposes IsOnMainThread(),
            // so compare against the thread captured during Unity runtime initialization.
            if (_mainThreadId != 0 && Thread.CurrentThread.ManagedThreadId == _mainThreadId)
            {
                action();
                return;
            }

            MobileAdsEventExecutor.ExecuteInUpdate(action);
        }
    }
}
#endif
