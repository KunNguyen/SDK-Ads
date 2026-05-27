using System.Collections.Generic;
using JisSDKAds.Core.Interfaces;
using JisSDKAds.Core.Models;
using UnityEngine;

namespace JisSDKAds.Core
{
    /// <summary>
    /// Registers IAdService providers from configs and initializes AdManager.
    /// For full ads (App Open, Resume, …) use SdkAdsBootstrap + JisSDKAdsSettings in the ads package.
    /// </summary>
    public class AdManagerBootstrap : MonoBehaviour
    {
        [SerializeField] private List<ScriptableObject> providerConfigs = new List<ScriptableObject>();
        [SerializeField] private AdProviderId primaryProvider = AdProviderId.Max;
        [SerializeField] private AdProviderId fallbackProvider = AdProviderId.None;
        [SerializeField] private bool singleMediationOnly = true;

        private void Awake()
        {
            var adManager = FindFirstObjectByType<AdManager>();
            if (adManager == null)
            {
                var go = new GameObject("AdManager");
                adManager = go.AddComponent<AdManager>();
                DontDestroyOnLoad(go);
            }

            adManager.ConfigureSingleMediation(primaryProvider, singleMediationOnly);
            if (!singleMediationOnly)
                adManager.SetProviderPriority(primaryProvider, fallbackProvider);

            foreach (var obj in providerConfigs)
            {
                if (obj is IAdProviderConfig config)
                {
                    var service = config.CreateProvider();
                    adManager.RegisterProvider(config.ProviderId, service);
                }
                else if (obj != null)
                {
                    Debug.LogWarning($"[JisSDKAds] Skipping {obj.name} - does not implement IAdProviderConfig");
                }
            }

            adManager.Initialize(
                onSuccess: () => Debug.Log("[JisSDKAds] Initialized successfully"),
                onFailure: err => Debug.LogError($"[JisSDKAds] Init failed: {err}"));
        }
    }
}
