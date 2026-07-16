using System;
using System.Collections;
using System.Collections.Generic;
using JisSDKAds.Common;
using UnityEngine;

namespace JisSDKAds.Ads.SequentialTier
{
    /// <summary>
    /// Defers post-show / post-fail warm reloads so a new interstitial/rewarded load never starts
    /// sooner than <see cref="MinDelaySeconds"/> after the previous ad closed or failed.
    /// One pending reload per tag — duplicate requests inside the window are coalesced.
    /// </summary>
    public static class AdReloadScheduler
    {
        public const float MinDelaySeconds = 2f;

        static AdReloadSchedulerHost _host;
        static readonly HashSet<string> Pending = new HashSet<string>();

        public static void Schedule(string tag, Action reload)
        {
            if (reload == null)
                return;

            var host = EnsureHost();
            if (host == null)
            {
                // Edit mode / teardown: no coroutine host available, keep old immediate behavior.
                reload();
                return;
            }

            if (!Pending.Add(tag))
            {
                DebugAds.Log($"[AdReloadScheduler] '{tag}' already pending — coalesced.");
                return;
            }

            DebugAds.Log($"[AdReloadScheduler] '{tag}' reload in {MinDelaySeconds:0.#}s.");
            host.StartCoroutine(CoRun(tag, reload));
        }

        static IEnumerator CoRun(string tag, Action reload)
        {
            yield return new WaitForSecondsRealtime(MinDelaySeconds);
            Pending.Remove(tag);
            reload();
        }

        static AdReloadSchedulerHost EnsureHost()
        {
            if (_host != null)
                return _host;
            if (!Application.isPlaying)
                return null;

            var go = new GameObject("[JisAds] AdReloadScheduler")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            UnityEngine.Object.DontDestroyOnLoad(go);
            _host = go.AddComponent<AdReloadSchedulerHost>();
            return _host;
        }

        sealed class AdReloadSchedulerHost : MonoBehaviour
        {
            void OnDestroy()
            {
                if (_host != this)
                    return;
                _host = null;
                Pending.Clear();
            }
        }
    }
}
