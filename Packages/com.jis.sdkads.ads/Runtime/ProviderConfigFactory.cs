using System;
using System.Reflection;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Core.Interfaces;
using UnityEngine;

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
                AdsMediationType.MAX => CreateProviderConfig("JisSDKAds.Providers.Max.MaxAdConfig, JisSDKAds.Providers.Max", setup =>
                {
                    SetField(setup, "sdkKey", profile.sdkSetup.maxAdsSetup.SDKKey);
                    SetField(setup, "interstitialAdUnitId", profile.sdkSetup.maxAdsSetup.InterstitialAdUnitID);
                    SetField(setup, "rewardedAdUnitId", profile.sdkSetup.maxAdsSetup.RewardedAdUnitID);
                    SetField(setup, "bannerAdUnitId", profile.sdkSetup.maxAdsSetup.BannerAdUnitID);
                    SetField(setup, "appOpenAdUnitId", profile.sdkSetup.maxAdsSetup.AppOpenAdUnitID);
                }),
#endif
#if UNITY_AD_ADMOB
                AdsMediationType.ADMOB => CreateProviderConfig("JisSDKAds.Providers.AdMob.AdMobConfig, JisSDKAds.Providers.AdMob", setup =>
                {
                    SetField(setup, "appId", "");
                    var inter = profile.sdkSetup.admobAdsSetup?.InterstitialAdUnitIDList;
                    var reward = profile.sdkSetup.admobAdsSetup?.RewardedAdUnitIDList;
                    var banner = profile.sdkSetup.admobAdsSetup?.BannerAdUnitIDList;
                    SetField(setup, "interstitialAdUnitId", inter != null && inter.Count > 0 ? inter[0] : "");
                    SetField(setup, "rewardedAdUnitId", reward != null && reward.Count > 0 ? reward[0] : "");
                    SetField(setup, "bannerAdUnitId", banner != null && banner.Count > 0 ? banner[0] : "");
                    var appOpen = profile.sdkSetup.admobAdsSetup?.AppOpenAdUnitIDList;
                    SetField(setup, "appOpenAdUnitId", appOpen != null && appOpen.Count > 0 ? appOpen[0] : "");
                }),
#endif
                _ => null
            };
        }

        static IAdProviderConfig CreateProviderConfig(string typeNameWithAssembly, Action<ScriptableObject> configure)
        {
            var type = Type.GetType(typeNameWithAssembly);
            if (type == null || !typeof(IAdProviderConfig).IsAssignableFrom(type))
                return null;

            var instance = ScriptableObject.CreateInstance(type);
            configure?.Invoke(instance);
            return instance as IAdProviderConfig;
        }

        static void SetField(ScriptableObject target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }
    }
}
