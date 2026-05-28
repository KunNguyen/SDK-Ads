using System;
using System.Reflection;
using JisSDKAds.Ads.Tiered;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Common;
using JisSDKAds.Core.Interfaces;
using UnityEngine;
#if UNITY_AD_ADMOB
using GoogleMobileAds.Api;
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
                    var admob = profile.sdkSetup.admobAdsSetup;
                    var inter = admob?.InterstitialAdUnitIDList;
                    var reward = admob?.RewardedAdUnitIDList;
                    var banner = admob?.BannerAdUnitIDList;
                    var appOpen = admob?.AppOpenAdUnitIDList;

                    // IMPORTANT: Core AdMob provider cannot load with an empty/invalid unit id.
                    // Prefer:
                    // - legacy list[0] (if populated)
                    // - Remote Config applied to SequentialTierConfig (Premium first; then High)
                    // - SequentialTier default fallback from settings
                    // This ensures Core preload uses the same RC-driven inventory as SequentialTier loader.
                    var fallbackInter = TieredAdsConfigFactory.ResolveDefaultFallbackUnitId(SequentialTierAdFormat.Interstitial, profile);
                    var fallbackReward = TieredAdsConfigFactory.ResolveDefaultFallbackUnitId(SequentialTierAdFormat.Rewarded, profile);

                    static string FirstNonEmpty(params string[] values)
                    {
                        if (values == null) return null;
                        foreach (var v in values)
                        {
                            if (!string.IsNullOrWhiteSpace(v))
                                return v.Trim();
                        }
                        return null;
                    }

                    string interList0 = inter != null && inter.Count > 0 ? inter[0] : null;
                    string rewardList0 = reward != null && reward.Count > 0 ? reward[0] : null;

                    // Pull the RC-resolved unit id from SequentialTierConfig entries (set via ApplyResolvedIdsToAdmobSetup).
                    string interTierPremium = admob?.InterstitialTierConfig?.GetEntry(AdTier.Premium)?.ResolveAdUnitId();
                    string interTierHigh = admob?.InterstitialTierConfig?.GetEntry(AdTier.High)?.ResolveAdUnitId();
                    string rewardTierPremium = admob?.RewardedTierConfig?.GetEntry(AdTier.Premium)?.ResolveAdUnitId();
                    string rewardTierHigh = admob?.RewardedTierConfig?.GetEntry(AdTier.High)?.ResolveAdUnitId();

                    var interId = FirstNonEmpty(interList0, interTierPremium, interTierHigh, fallbackInter) ?? "";
                    var rewardId = FirstNonEmpty(rewardList0, rewardTierPremium, rewardTierHigh, fallbackReward) ?? "";

                    admob?.EnsureInitialized();
                    string bannerList0 = banner != null && banner.Count > 0 ? banner[0] : null;
                    string bannerScheduleId = admob?.BannerAdUnitID?.ID;
                    var bannerId = FirstNonEmpty(bannerList0, bannerScheduleId) ?? "";

                    SetField(setup, "interstitialAdUnitId", interId);
                    SetField(setup, "rewardedAdUnitId", rewardId);
                    SetField(setup, "bannerAdUnitId", bannerId);
                    SetField(setup, "appOpenAdUnitId", appOpen != null && appOpen.Count > 0 ? appOpen[0] : "");
#if UNITY_AD_ADMOB
                    SetField(setup, "bannerPosition", profile.sdkSetup.admobBannerAdsPosition);
#endif
                    DebugAds.Log($"[ProviderConfig] AdMob banner unitId={(string.IsNullOrEmpty(bannerId) ? "(empty)" : bannerId)}");
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
