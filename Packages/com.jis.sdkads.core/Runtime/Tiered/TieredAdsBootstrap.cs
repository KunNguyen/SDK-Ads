using JisSDKAds.Core.Tiered.Config;
using JisSDKAds.Core.Tiered.Interfaces;
using JisSDKAds.Core.Tiered.Services;
using UnityEngine;

namespace JisSDKAds.Core.Tiered
{
    /// <summary>
    /// Factory for tiered extension layer. Keeps integration out of AdsManager / mediation controllers.
    /// </summary>
    public static class TieredAdsBootstrap
    {
        public static TieredAdsExtension CreateExtension(
            TieredAdsConfig config,
            ITieredAdBackend backend,
            Transform hostTransform)
        {
            if (config == null || backend == null || !config.EnableTieredInventory)
                return null;

            var hostGo = new GameObject("TieredAdsRuntimeHost");
            hostGo.transform.SetParent(hostTransform, false);
            var host = hostGo.AddComponent<TieredAdsRuntimeHost>();

            var manager = new TieredAdsManager(config, backend, host);
            manager.Initialize();

            return new TieredAdsExtension(config, manager, host);
        }
    }

    public sealed class TieredAdsExtension
    {
        public TieredAdsConfig Config { get; }
        public TieredAdsManager Manager { get; }
        public TieredAdsRuntimeHost Host { get; }

        internal TieredAdsExtension(TieredAdsConfig config, TieredAdsManager manager, TieredAdsRuntimeHost host)
        {
            Config = config;
            Manager = manager;
            Host = host;
        }

        public bool IsTieredForInterstitial => Config.IsTieredEnabledFor(Models.AdsFormatType.Interstitial);
        public bool IsTieredForRewarded => Config.IsTieredEnabledFor(Models.AdsFormatType.Rewarded);

        public void OnApplicationPause(bool paused) => Manager.OnApplicationPause(paused);

        public void Shutdown()
        {
            Manager.Shutdown();
            if (Host != null)
                Object.Destroy(Host.gameObject);
        }
    }
}
