using JisSDKAds.Ads.Settings;
using JisSDKAds.Core.Tiered.Config;
using JisSDKAds.Core.Tiered.Models;

namespace JisSDKAds.Ads.Tiered
{
    /// <summary>
    /// Populates tier unit IDs from SDKSetup when TieredAdsConfig fields are empty.
    /// </summary>
    public static class TieredAdsConfigFactory
    {
        public static void ApplyLegacyFallbackFromSdkSetup(PlatformAdsProfile profile, TieredAdsConfig config)
        {
            if (profile?.sdkSetup == null || config == null)
                return;

            ApplyFromSdkSetup(profile, config);
        }

        static void ApplyFromSdkSetup(PlatformAdsProfile profile, TieredAdsConfig config)
        {
            switch (profile.mediation)
            {
#if UNITY_AD_MAX
                case AdsMediationType.MAX:
                    ApplyMax(profile.sdkSetup.maxAdsSetup, config);
                    break;
#endif
#if UNITY_AD_ADMOB
                case AdsMediationType.ADMOB:
                    ApplyAdMob(profile.sdkSetup.admobAdsSetup, config);
                    break;
#endif
            }
        }

#if UNITY_AD_MAX
        static void ApplyMax(MaxAdSetup setup, TieredAdsConfig config)
        {
            if (setup == null) return;

            if (string.IsNullOrEmpty(config.LegacyInterstitial.UnitId))
                config.LegacyInterstitial.UnitId = setup.InterstitialAdUnitID;
            if (string.IsNullOrEmpty(config.LegacyRewarded.UnitId))
                config.LegacyRewarded.UnitId = setup.RewardedAdUnitID;

            if (string.IsNullOrEmpty(config.Interstitial.High))
                config.Interstitial.High = setup.InterstitialAdUnitID;
            if (string.IsNullOrEmpty(config.Interstitial.Mid) && !string.IsNullOrEmpty(setup.InterstitialAdUnitID))
                config.Interstitial.Mid = setup.InterstitialAdUnitID + "_mid";
            if (string.IsNullOrEmpty(config.Interstitial.Low) && !string.IsNullOrEmpty(setup.InterstitialAdUnitID))
                config.Interstitial.Low = setup.InterstitialAdUnitID + "_low";

            if (string.IsNullOrEmpty(config.Rewarded.High))
                config.Rewarded.High = setup.RewardedAdUnitID;
            if (string.IsNullOrEmpty(config.Rewarded.Mid) && !string.IsNullOrEmpty(setup.RewardedAdUnitID))
                config.Rewarded.Mid = setup.RewardedAdUnitID + "_mid";
            if (string.IsNullOrEmpty(config.Rewarded.Low) && !string.IsNullOrEmpty(setup.RewardedAdUnitID))
                config.Rewarded.Low = setup.RewardedAdUnitID + "_low";
        }
#endif

#if UNITY_AD_ADMOB
        static void ApplyAdMob(AdmobAdSetup setup, TieredAdsConfig config)
        {
            if (setup == null) return;

            var inter = setup.InterstitialAdUnitIDList;
            var reward = setup.RewardedAdUnitIDList;

            if (string.IsNullOrEmpty(config.LegacyInterstitial.UnitId) && inter != null && inter.Count > 0)
                config.LegacyInterstitial.UnitId = inter[0];
            if (string.IsNullOrEmpty(config.LegacyRewarded.UnitId) && reward != null && reward.Count > 0)
                config.LegacyRewarded.UnitId = reward[0];

            AssignListToTiers(inter, config.Interstitial);
            AssignListToTiers(reward, config.Rewarded);
        }

        static void AssignListToTiers(System.Collections.Generic.List<string> ids, TierUnit tierUnit)
        {
            if (ids == null || ids.Count == 0) return;
            if (string.IsNullOrEmpty(tierUnit.High) && ids.Count > 0)
                tierUnit.High = ids[0];
            if (string.IsNullOrEmpty(tierUnit.Mid) && ids.Count > 1)
                tierUnit.Mid = ids[1];
            else if (string.IsNullOrEmpty(tierUnit.Mid) && ids.Count == 1)
                tierUnit.Mid = ids[0];
            if (string.IsNullOrEmpty(tierUnit.Low) && ids.Count > 2)
                tierUnit.Low = ids[2];
            else if (string.IsNullOrEmpty(tierUnit.Low) && ids.Count > 0)
                tierUnit.Low = ids[ids.Count - 1];
        }
#endif
    }
}
