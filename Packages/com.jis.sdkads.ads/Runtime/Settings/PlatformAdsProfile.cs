using JisSDKAds.Core.Interfaces;
using JisSDKAds.Core.Models;
using JisSDKAds.Core.Tiered.Config;
using Sirenix.OdinInspector;
using UnityEngine;
using JisSDKAds.Ads;

namespace JisSDKAds.Ads.Settings
{
    [System.Serializable]
    public class PlatformAdsProfile
    {
        [BoxGroup("Mediation"), LabelText("Primary mediation")]
        public AdsMediationType mediation = AdsMediationType.MAX;

        [BoxGroup("Mediation"), LabelText("SDK setup (formats, RC, cooldowns)")]
        [Required, HideInInspector]
        public SDKSetup sdkSetup;

        [BoxGroup("Provider configs (Core AdManager)"), LabelText("MAX")]
        [ShowIf(nameof(mediation), AdsMediationType.MAX)]
        public ScriptableObject maxProviderConfig;

        [BoxGroup("Provider configs (Core AdManager)"), LabelText("AdMob")]
        [ShowIf(nameof(mediation), AdsMediationType.ADMOB)]
        public ScriptableObject admobProviderConfig;

        [BoxGroup("Tiered inventory (optional)"), LabelText("Tiered Ads Config")]
        [Tooltip("When assigned and EnableTieredInventory=true, interstitial/rewarded use tiered inventory.")]
        [HideInInspector]
        public TieredAdsConfig tieredAdsConfig;

        public AdProviderId ProviderId => mediation switch
        {
            AdsMediationType.MAX => AdProviderId.Max,
            AdsMediationType.ADMOB => AdProviderId.AdMob,
            _ => AdProviderId.None
        };

        public IAdProviderConfig GetProviderConfig()
        {
            return mediation switch
            {
                AdsMediationType.MAX => maxProviderConfig as IAdProviderConfig,
                AdsMediationType.ADMOB => admobProviderConfig as IAdProviderConfig,
                _ => null
            };
        }
    }
}
