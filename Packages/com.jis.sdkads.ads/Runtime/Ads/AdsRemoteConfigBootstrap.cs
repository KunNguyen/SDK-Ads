using System.Collections;
using System.Threading.Tasks;
using JisSDKAds.Common;
using JisSDKAds.Firebase;
using UnityEngine;

namespace JisSDKAds.Ads
{
    /// <summary>
    /// Ensures Firebase Remote Config is fetched before ads mediation/unit managers start loading ads.
    /// </summary>
    public static class AdsRemoteConfigBootstrap
    {
        public static bool IsRemoteConfigReadyForAds { get; private set; }

        public static void ResetSessionGate() => IsRemoteConfigReadyForAds = false;

        public static IEnumerator EnsureFetchedCoroutine(bool fetchWhenFirebasePresent = true)
        {
            IsRemoteConfigReadyForAds = false;

            if (FirebaseManager.Instance == null)
            {
                DebugAds.LogWarning(
                    "[AdsManager] FirebaseManager missing — skipping Remote Config gate (local settings only).");
                IsRemoteConfigReadyForAds = true;
                yield break;
            }

            if (!FirebaseManager.Instance.IsFirebaseReady)
            {
                var initTask = FirebaseManager.Instance.InitAsync();
                while (!initTask.IsCompleted)
                    yield return null;
            }

            while (!FirebaseManager.Instance.IsFirebaseReady)
                yield return new WaitForEndOfFrame();

            if (!fetchWhenFirebasePresent)
            {
                IsRemoteConfigReadyForAds = true;
                yield break;
            }

            if (!FirebaseManager.Instance.IsRemoteConfigReady)
            {
                var fetchTask = FirebaseManager.Instance.FetchRemoteConfigAsync();
                while (!fetchTask.IsCompleted)
                    yield return null;
            }

            IsRemoteConfigReadyForAds = FirebaseManager.Instance.IsRemoteConfigReady;
            if (!IsRemoteConfigReadyForAds)
            {
                DebugAds.LogWarning(
                    "[AdsManager] Remote Config fetch did not complete — ads will use local settings.");
            }
        }

        public static async Task<bool> EnsureFetchedAsync(bool fetchWhenFirebasePresent = true)
        {
            if (FirebaseManager.Instance == null)
            {
                IsRemoteConfigReadyForAds = true;
                return true;
            }

            await FirebaseManager.Instance.InitAsync();

            if (!fetchWhenFirebasePresent)
            {
                IsRemoteConfigReadyForAds = true;
                return true;
            }

            if (!FirebaseManager.Instance.IsRemoteConfigReady)
                await FirebaseManager.Instance.FetchRemoteConfigAsync();

            IsRemoteConfigReadyForAds = FirebaseManager.Instance.IsRemoteConfigReady;
            return IsRemoteConfigReadyForAds;
        }
    }
}
