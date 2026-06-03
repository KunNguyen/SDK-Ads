using System;
using System.Collections.Generic;
using JisSDKAds.Common;
using JisSDKAds.Firebase;
using UnityEngine;

namespace JisSDKAds.Ads.SequentialTier
{
    /// <summary>
    /// Global pipeline: at most one interstitial or rewarded AdMob load request at a time.
    /// The next request starts only after the previous reports a result (success, fail, or ladder complete).
    /// </summary>
    public sealed class AdLoadCoordinator
    {
        public static AdLoadCoordinator Instance { get; } = new AdLoadCoordinator();

        const float InterstitialDeferBufferBeforeCappingSec = 30f;

        MonoBehaviour _host;
        bool _nextRewardedLoadIsUrgent;

        AdLoadFormat? _activeFormat;
        SequentialTierLoader _activeTierLoader;
        Action _activeGenericBegin;

        readonly Dictionary<AdLoadFormat, SequentialTierLoader> _registeredTierLoaders = new();
        readonly List<PendingLoad> _queue = new();

        public event Action PipelineBecameIdle;

        public bool IsPipelineIdle => _activeFormat == null && _queue.Count == 0;

        struct PendingLoad
        {
            public AdLoadFormat Format;
            public SequentialTierLoader TierLoader;
            public Action GenericBegin;
            public bool ForceReload;
            public bool Urgent;

            public bool IsTier => TierLoader != null;
        }

        public void Configure(MonoBehaviour host) => _host = host;

        public void PrepareUrgentRewarded() => _nextRewardedLoadIsUrgent = true;

        public void RegisterLoader(SequentialTierLoader loader, AdLoadFormat format)
        {
            if (loader == null) return;
            _registeredTierLoaders[format] = loader;
        }

        public void UnregisterLoader(SequentialTierLoader loader, AdLoadFormat format)
        {
            if (_registeredTierLoaders.TryGetValue(format, out var existing) && existing == loader)
                _registeredTierLoaders.Remove(format);

            if (_activeTierLoader == loader)
                CompleteActiveSession();
        }

        public bool HoldsSessionFor(SequentialTierLoader loader) =>
            loader != null && _activeTierLoader == loader && _activeFormat != null;

        public float GetMaxDeferredInterstitialPreloadSeconds()
        {
            var capping = GetCappingFromAppOpenSeconds();
            if (capping <= 0f)
                return 0f;

            return Mathf.Max(0f, capping - InterstitialDeferBufferBeforeCappingSec);
        }

        public void RequestTierLoad(SequentialTierLoader loader, AdLoadFormat format, bool forceReload, bool urgent = false)
        {
            if (loader == null)
                return;

            if (!urgent && format == AdLoadFormat.Rewarded && _nextRewardedLoadIsUrgent)
            {
                urgent = true;
                _nextRewardedLoadIsUrgent = false;
            }

            if (urgent && format == AdLoadFormat.Rewarded)
            {
                PreemptInterstitialForRewarded(loader, forceReload);
                return;
            }

            EnqueueOrStart(new PendingLoad
            {
                Format = format,
                TierLoader = loader,
                ForceReload = forceReload,
                Urgent = urgent
            });
        }

        /// <summary>Single-unit (non-tier) interstitial/rewarded load.</summary>
        public void RequestGenericLoad(AdLoadFormat format, Action beginLoad, bool urgent = false)
        {
            if (beginLoad == null)
                return;

            if (!urgent && format == AdLoadFormat.Rewarded && _nextRewardedLoadIsUrgent)
            {
                urgent = true;
                _nextRewardedLoadIsUrgent = false;
            }

            if (urgent && format == AdLoadFormat.Rewarded && _activeFormat == AdLoadFormat.Interstitial)
            {
                DebugAds.Log("[AdLoadCoordinator] Urgent rewarded — completing interstitial slot before rewarded.");
                CompleteActiveSession();
            }

            EnqueueOrStart(new PendingLoad
            {
                Format = format,
                GenericBegin = beginLoad,
                Urgent = urgent
            });
        }

        public void NotifyTierSessionEnded(SequentialTierLoader loader)
        {
            if (_activeTierLoader != loader)
                return;

            CompleteActiveSession();
        }

        public void NotifyGenericLoadEnded(AdLoadFormat format)
        {
            if (_activeFormat != format || _activeTierLoader != null)
                return;

            CompleteActiveSession();
        }

        void EnqueueOrStart(PendingLoad request)
        {
            if (_activeFormat == null)
            {
                StartPending(request);
                return;
            }

            if (request.IsTier && request.TierLoader == _activeTierLoader && request.Format == _activeFormat)
            {
                request.TierLoader.ExecuteLoad(request.ForceReload);
                return;
            }

            _queue.RemoveAll(p => MatchesPending(p, request));
            _queue.Add(request);
            _queue.Sort(ComparePending);
            DebugAds.Log(
                $"[AdLoadCoordinator] Queued format={request.Format} urgent={request.Urgent} depth={_queue.Count} active={_activeFormat}");
        }

        static bool MatchesPending(PendingLoad existing, PendingLoad incoming)
        {
            if (incoming.IsTier)
                return existing.TierLoader == incoming.TierLoader;
            return !existing.IsTier && existing.Format == incoming.Format;
        }

        void PreemptInterstitialForRewarded(SequentialTierLoader rewardedLoader, bool forceReload)
        {
            if (_activeFormat == AdLoadFormat.Interstitial)
            {
                if (_activeTierLoader != null)
                {
                    DebugAds.Log("[AdLoadCoordinator] Preempt tier interstitial load for urgent rewarded.");
                    var interrupted = _activeTierLoader;
                    interrupted.CancelActiveLoad();
                    CompleteActiveSession();
                    if (!interrupted.IsReady)
                    {
                        _queue.RemoveAll(p => p.TierLoader == interrupted);
                        _queue.Add(new PendingLoad
                        {
                            Format = AdLoadFormat.Interstitial,
                            TierLoader = interrupted,
                            ForceReload = false
                        });
                        _queue.Sort(ComparePending);
                    }
                }
                else
                {
                    DebugAds.Log("[AdLoadCoordinator] Preempt generic interstitial load for urgent rewarded.");
                    CompleteActiveSession();
                }
            }

            _queue.RemoveAll(p => p.Format == AdLoadFormat.Rewarded);
            StartPending(new PendingLoad
            {
                Format = AdLoadFormat.Rewarded,
                TierLoader = rewardedLoader,
                ForceReload = forceReload,
                Urgent = true
            });
        }

        void StartPending(PendingLoad request)
        {
            _activeFormat = request.Format;
            _activeTierLoader = request.TierLoader;
            _activeGenericBegin = request.IsTier ? null : request.GenericBegin;

            DebugAds.Log($"[AdLoadCoordinator] Start load format={request.Format} mode={(request.IsTier ? "tier" : "generic")}");

            if (request.IsTier)
                request.TierLoader.ExecuteLoad(request.ForceReload);
            else
                _activeGenericBegin?.Invoke();
        }

        void CompleteActiveSession()
        {
            if (_activeFormat != null)
                DebugAds.Log($"[AdLoadCoordinator] Load complete format={_activeFormat}");

            _activeFormat = null;
            _activeTierLoader = null;
            _activeGenericBegin = null;
            ProcessQueue();
        }

        void NotifyPipelineIdleIfQuiescent()
        {
            if (!IsPipelineIdle)
                return;

            PipelineBecameIdle?.Invoke();
        }

        static int ComparePending(PendingLoad a, PendingLoad b)
        {
            if (a.Urgent != b.Urgent)
                return a.Urgent ? -1 : 1;
            return ((int)a.Format).CompareTo((int)b.Format);
        }

        void ProcessQueue()
        {
            if (_activeFormat != null)
                return;

            if (_queue.Count == 0)
            {
                NotifyPipelineIdleIfQuiescent();
                return;
            }

            var next = _queue[0];
            _queue.RemoveAt(0);
            StartPending(next);
        }

        static float GetCappingFromAppOpenSeconds()
        {
#if UNITY_EDITOR
            return 0f;
#else
            if (FirebaseManager.Instance == null)
                return 0f;
            return (float)FirebaseManager.Instance.GetConfigDouble(
                Keys.key_remote_interstitial_capping_from_app_open_seconds);
#endif
        }
    }
}
