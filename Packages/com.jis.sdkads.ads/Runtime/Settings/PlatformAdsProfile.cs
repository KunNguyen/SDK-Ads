using JisSDKAds.Core.Interfaces;
using JisSDKAds.Core.Models;
using UnityEngine;
using JisSDKAds.Ads;

namespace JisSDKAds.Ads.Settings
{
    [System.Serializable]
    public class PlatformAdsProfile
    {
        public AdsMediationType mediation = AdsMediationType.MAX;

        [HideInInspector]
        public SDKSetup sdkSetup;

        public ScriptableObject maxProviderConfig;

        public ScriptableObject admobProviderConfig;

        public AdProviderId ProviderId => mediation switch
        {
            AdsMediationType.MAX => AdProviderId.Max,
            AdsMediationType.ADMOB => AdProviderId.AdMob,
            _ => AdProviderId.None
        };

        public IAdProviderConfig GetProviderConfig() => GetProviderConfig(mediation);

        public IAdProviderConfig GetProviderConfig(AdsMediationType providerMediation)
        {
            return providerMediation switch
            {
                AdsMediationType.MAX => maxProviderConfig as IAdProviderConfig,
                AdsMediationType.ADMOB => admobProviderConfig as IAdProviderConfig,
                _ => null
            };
        }
    }
}
