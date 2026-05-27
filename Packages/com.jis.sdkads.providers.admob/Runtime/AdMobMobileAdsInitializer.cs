#if UNITY_AD_ADMOB
using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using JisSDKAds.Ads;
using JisSDKAds.Common;
using UnityEngine;

namespace JisSDKAds.Providers.AdMob
{
    /// <summary>
    /// Single entry point for <see cref="MobileAds.Initialize"/> shared by legacy mediation and Core <see cref="AdMobProvider"/>.
    /// Can be started before Firebase Remote Config; ad unit loads should wait until RC is applied.
    /// </summary>
    public static class AdMobMobileAdsInitializer
    {
        const float InitTimeoutSeconds = 90f;

        enum InitState
        {
            NotStarted,
            Initializing,
            Ready,
            Failed
        }

        static InitState _state = InitState.NotStarted;
        static readonly List<Action<bool>> _pendingCallbacks = new List<Action<bool>>();

        public static bool IsReady => _state == InitState.Ready;
        public static bool IsFailed => _state == InitState.Failed;
        public static bool IsInitializing => _state == InitState.Initializing;

        public static void EnsureInitializedEarly(bool requestConsent) =>
            EnsureInitialized(requestConsent, null);

        public static void EnsureInitialized(bool requestConsent, Action<bool> onComplete)
        {
            if (onComplete != null)
                _pendingCallbacks.Add(onComplete);

            if (_state == InitState.Ready)
            {
                FlushCallbacks(true);
                return;
            }

            if (_state == InitState.Failed)
            {
                FlushCallbacks(false);
                return;
            }

            if (_state == InitState.Initializing)
                return;

            _state = InitState.Initializing;
            AdMobInitRunner.Instance.StartCoroutine(CoInitialize(requestConsent));
        }

        static IEnumerator CoInitialize(bool requestConsent)
        {
            ApplyDefaultRequestConfiguration();
            AdMobInitRunner.Instance.StartCoroutine(CoWatchTimeout());

            if (requestConsent && AdmobUmpConsent.IsAvailable)
            {
                var consentDone = false;

                AdmobUmpConsent.RequestConsentAndInitialize(
                    () => consentDone = true,
                    msg =>
                    {
                        DebugAds.LogError($"[AdMob] UMP consent failed: {msg}. Falling back to MobileAds.Initialize.");
                        consentDone = true;
                    });

                while (!consentDone)
                    yield return null;
            }
            else if (requestConsent && !AdmobUmpConsent.IsAvailable)
            {
                DebugAds.LogWarning($"[AdMob] UMP not available. {AdmobUmpConsent.PluginHint}");
            }

            var initFinished = false;
            var initSuccess = false;

            if (_state != InitState.Initializing)
                yield break;

            MobileAds.Initialize(initStatus =>
            {
                initFinished = true;
                if (initStatus == null)
                {
                    DebugAds.LogSdkInit("AdMob", "MobileAds.Initialize", false, "initStatus is null");
                    return;
                }

                var adapterStatusMap = initStatus.getAdapterStatusMap();
                if (adapterStatusMap == null)
                {
                    DebugAds.LogSdkInit("AdMob", "MobileAds.Initialize", false, "adapterStatusMap is null");
                    return;
                }

                foreach (var keyValuePair in adapterStatusMap)
                {
                    var className = keyValuePair.Key;
                    var status = keyValuePair.Value;
                    switch (status.InitializationState)
                    {
                        case AdapterState.NotReady:
                            DebugAds.Log($"[AdMob] Adapter not ready: {className}");
                            break;
                        case AdapterState.Ready:
                            DebugAds.Log($"[AdMob] Adapter ready: {className}");
                            break;
                    }
                }

                DebugAds.LogSdkInit("AdMob", "MobileAds.Initialize", true,
                    $"adapters={adapterStatusMap.Count}");
                initSuccess = true;
            });

            while (!initFinished && _state == InitState.Initializing)
                yield return null;

            if (_state != InitState.Initializing)
                yield break;

            if (initSuccess)
                MarkReady();
            else
                MarkFailed("MobileAds.Initialize callback reported failure");
        }

        static IEnumerator CoWatchTimeout()
        {
            var elapsed = 0f;
            while (_state == InitState.Initializing && elapsed < InitTimeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_state != InitState.Initializing)
                yield break;

            MarkFailed(
                $"timed out after {InitTimeoutSeconds}s — verify AdMob App ID, Google Play Services, and network. " +
                $"UMP available: {AdmobUmpConsent.IsAvailable}");
        }

        static void ApplyDefaultRequestConfiguration()
        {
            var requestConfiguration = new RequestConfiguration();
            requestConfiguration.TestDeviceIds.Add("F0DE51766DB7C0740DEF1633ACCB3755");
            requestConfiguration.TestDeviceIds.Add("4849D928DCE8B9A471CE3BAB5E57E08A");
            MobileAds.SetRequestConfiguration(requestConfiguration);
        }

        static void MarkReady()
        {
            if (_state != InitState.Initializing)
                return;

            _state = InitState.Ready;
            FlushCallbacks(true);
        }

        static void MarkFailed(string reason)
        {
            if (_state == InitState.Ready)
                return;

            _state = InitState.Failed;
            DebugAds.LogSdkInit("AdMob", "MobileAds.Initialize", false, reason);
            FlushCallbacks(false);
        }

        static void FlushCallbacks(bool success)
        {
            if (_pendingCallbacks.Count == 0)
                return;

            var copy = _pendingCallbacks.ToArray();
            _pendingCallbacks.Clear();
            foreach (var cb in copy)
                cb?.Invoke(success);
        }
    }

    internal sealed class AdMobInitRunner : MonoBehaviour
    {
        static AdMobInitRunner _instance;

        public static AdMobInitRunner Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                var go = new GameObject("JisSDKAds_AdMobInitRunner");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<AdMobInitRunner>();
                return _instance;
            }
        }
    }
}
#endif
