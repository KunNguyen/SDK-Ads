using System;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Core.Tiered.Interfaces;

namespace JisSDKAds.Ads.Tiered
{
    /// <summary>
    /// Resolves ITieredAdBackend from platform profile mediation type (reflection — no hard provider reference).
    /// </summary>
    public static class TieredAdBackendFactory
    {
        public static ITieredAdBackend Create(PlatformAdsProfile profile)
        {
            if (profile == null) return null;

            return profile.mediation switch
            {
#if UNITY_AD_MAX
                AdsMediationType.MAX => CreateBackend("JisSDKAds.Providers.Max.MaxTieredAdBackend, JisSDKAds.Providers.Max"),
#endif
#if UNITY_AD_ADMOB
                AdsMediationType.ADMOB => CreateBackend("JisSDKAds.Providers.AdMob.AdMobTieredAdBackend, JisSDKAds.Providers.AdMob"),
#endif
                _ => null
            };
        }

        static ITieredAdBackend CreateBackend(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type == null || !typeof(ITieredAdBackend).IsAssignableFrom(type))
                return null;

            return Activator.CreateInstance(type) as ITieredAdBackend;
        }
    }
}
