using JisSDKAds.Ads.Settings;
using JisSDKAds.Core.Interfaces;
using UnityEngine;
#if UNITY_AD_MAX
using JisSDKAds.Providers.Max;
#endif
#if UNITY_AD_ADMOB
using JisSDKAds.Providers.AdMob;
#endif

namespace JisSDKAds.Ads
{
    /// <summary>
    /// Builds Core provider configs from legacy <see cref="SDKSetup"/> when separate MaxAdConfig assets are not assigned.
    /// </summary>
    public static class ProviderConfigFactory
    {
        public static IAdProviderConfig CreateFromSdkSetup(PlatformAdsProfile profile)
        {
            if (profile?.sdkSetup == null) return null;

            var existing = profile.GetProviderConfig();
            if (existing != null)
                return existing;

            return profile.mediation switch
            {
#if UNITY_AD_MAX
                AdsMediationType.MAX => CreateMaxConfig(profile.sdkSetup),
#endif
#if UNITY_AD_ADMOB
                AdsMediationType.ADMOB => CreateAdMobConfig(profile.sdkSetup),
#endif
                _ => null
            };
        }

#if UNITY_AD_MAX
        static MaxAdConfig CreateMaxConfig(SDKSetup setup)
        {
            var config = ScriptableObject.CreateInstance<MaxAdConfig>();
            config.sdkKey = setup.maxAdsSetup.SDKKey;
            config.interstitialAdUnitId = setup.maxAdsSetup.InterstitialAdUnitID;
            config.rewardedAdUnitId = setup.maxAdsSetup.RewardedAdUnitID;
            config.bannerAdUnitId = setup.maxAdsSetup.BannerAdUnitID;
            config.appOpenAdUnitId = setup.maxAdsSetup.AppOpenAdUnitID;
            return config;
        }
#endif

#if UNITY_AD_ADMOB
        static AdMobConfig CreateAdMobConfig(SDKSetup setup)
        {
            var config = ScriptableObject.CreateInstance<AdMobConfig>();
            config.appId = "";
            var inter = setup.admobAdsSetup?.InterstitialAdUnitIDList;
            var reward = setup.admobAdsSetup?.RewardedAdUnitIDList;
            var banner = setup.admobAdsSetup?.BannerAdUnitIDList;
            config.interstitialAdUnitId = inter != null && inter.Count > 0 ? inter[0] : "";
            config.rewardedAdUnitId = reward != null && reward.Count > 0 ? reward[0] : "";
            config.bannerAdUnitId = banner != null && banner.Count > 0 ? banner[0] : "";
            return config;
        }
#endif
    }
}
