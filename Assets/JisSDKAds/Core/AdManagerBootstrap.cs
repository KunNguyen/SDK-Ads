using System.Collections.Generic;
using JisSDKAds.Core.Interfaces;
using JisSDKAds.Core.Models;
using UnityEngine;

namespace JisSDKAds.Core
{
    /// <summary>
    /// Bootstrap: registers providers from configs and initializes AdManager.
    /// Add to a GameObject in your first scene.
    /// Assign provider configs (MaxAdConfig, AdMobConfig, etc.) in the inspector.
    /// </summary>
    public class AdManagerBootstrap : MonoBehaviour
    {
        [SerializeField] private List<ScriptableObject> providerConfigs = new List<ScriptableObject>();
        [SerializeField] private AdProviderId primaryProvider = AdProviderId.Max;
        [SerializeField] private AdProviderId fallbackProvider = AdProviderId.AdMob;

        private void Awake()
        {
            var adManager = FindFirstObjectByType<AdManager>();
            if (adManager == null)
            {
                var go = new GameObject("AdManager");
                adManager = go.AddComponent<AdManager>();
                DontDestroyOnLoad(go);
            }

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
                onFailure: err => Debug.LogError($"[JisSDKAds] Init failed: {err}")
            );
        }
    }
}
