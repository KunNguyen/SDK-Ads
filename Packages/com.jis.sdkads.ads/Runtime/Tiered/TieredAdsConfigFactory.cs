using System.Collections.Generic;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Common;
using JisSDKAds.Core.Tiered.Config;
using JisSDKAds.Core.Tiered.Models;
using JisSDKAds.Firebase;
using UnityEngine;
using CoreAdTier = JisSDKAds.Core.Tiered.Models.AdTier;
using SequentialAdTier = JisSDKAds.Ads.SequentialTier.AdTier;

namespace JisSDKAds.Ads.Tiered
{
    /// <summary>
    /// Populates tier unit IDs from SDKSetup when TieredAdsConfig fields are empty.
    /// Remote Config: read tier IDs high→low; empty RC slots inherit the nearest non-empty tier above;
    /// settings fallback unit ID is used only when every RC tier value is empty.
    /// </summary>
    public static class TieredAdsConfigFactory
    {
        static readonly SequentialAdTier[] SequentialTierOrder =
        {
            SequentialAdTier.Premium, SequentialAdTier.High, SequentialAdTier.Mid,
            SequentialAdTier.Low, SequentialAdTier.Fill
        };

        static readonly CoreAdTier[] CoreTierOrder = { CoreAdTier.High, CoreAdTier.Mid, CoreAdTier.Low };
        public static void ApplyLegacyFallbackFromSdkSetup(PlatformAdsProfile profile, TieredAdsConfig config)
        {
            if (profile?.sdkSetup == null || config == null)
                return;

            ApplyFromSdkSetup(profile, config);
        }

        /// <summary>
        /// After Remote Config is ready: use RC values when set; otherwise keep or fill from <see cref="JisSDKAdsSettings"/> / SDKSetup.
        /// </summary>
        public static void ApplyRemoteTierIdsWithFallback(PlatformAdsProfile profile, TieredAdsConfig config)
        {
            if (profile == null || config == null) return;
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
                return;

            if (TryBuildCoreTierIds(AdsFormatType.Interstitial, profile, config, out var interIds))
                ApplyCoreTierIds(config.Interstitial, interIds);

            if (TryBuildCoreTierIds(AdsFormatType.Rewarded, profile, config, out var rewardIds))
                ApplyCoreTierIds(config.Rewarded, rewardIds);
        }

        static void ApplyCoreTierIds(TierUnit tierUnit, IReadOnlyDictionary<CoreAdTier, string> tierIds)
        {
            if (tierUnit == null || tierIds == null) return;
            foreach (var pair in tierIds)
                SetCoreTierUnitId(tierUnit, pair.Key, pair.Value);
        }

        /// <summary>
        /// Reads RC tier keys high→low; cascades empty slots from the last non-empty tier above.
        /// Uses <see cref="ResolveDefaultFallbackUnitId"/> only when all RC values are empty.
        /// </summary>
        public static bool TryBuildSequentialTierIds(
            SequentialTierAdFormat format,
            PlatformAdsProfile profile,
            out Dictionary<SequentialAdTier, string> tierIds)
        {
            tierIds = new Dictionary<SequentialAdTier, string>();
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
                return false;

            var anyRc = false;
            string cascadeId = null;

            foreach (var tier in SequentialTierOrder)
            {
                var key = SequentialTierRemoteConfigResolver.GetRemoteConfigKey(format, tier);
                if (string.IsNullOrEmpty(key)) continue;

                var rcValue = FirebaseManager.Instance.GetConfigString(key);
                if (!string.IsNullOrWhiteSpace(rcValue))
                {
                    cascadeId = rcValue.Trim();
                    tierIds[tier] = cascadeId;
                    anyRc = true;
                }
                else if (!string.IsNullOrEmpty(cascadeId))
                {
                    tierIds[tier] = cascadeId;
                }
            }

            if (anyRc) return tierIds.Count > 0;

            var fallback = ResolveDefaultFallbackUnitId(format, profile);
            if (string.IsNullOrEmpty(fallback)) return false;

            foreach (var tier in SequentialTierOrder)
                tierIds[tier] = fallback;

            DebugAds.Log($"[RemoteConfig] {format} all RC tiers empty → fallback {fallback}");
            return true;
        }

        static bool TryBuildCoreTierIds(
            AdsFormatType format,
            PlatformAdsProfile profile,
            TieredAdsConfig config,
            out Dictionary<CoreAdTier, string> tierIds)
        {
            tierIds = new Dictionary<CoreAdTier, string>();
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
                return false;

            var anyRc = false;
            string cascadeId = null;

            foreach (var tier in CoreTierOrder)
            {
                var key = GetCoreRemoteConfigKey(format, tier);
                if (string.IsNullOrEmpty(key)) continue;

                var rcValue = FirebaseManager.Instance.GetConfigString(key);
                if (!string.IsNullOrWhiteSpace(rcValue))
                {
                    cascadeId = rcValue.Trim();
                    tierIds[tier] = cascadeId;
                    anyRc = true;
                }
                else if (!string.IsNullOrEmpty(cascadeId))
                {
                    tierIds[tier] = cascadeId;
                }
            }

            if (anyRc) return tierIds.Count > 0;

            var fallback = ResolveDefaultFallbackUnitId(
                format == AdsFormatType.Interstitial
                    ? SequentialTierAdFormat.Interstitial
                    : SequentialTierAdFormat.Rewarded,
                profile);

            if (string.IsNullOrEmpty(fallback))
            {
                fallback = config?.GetLegacyUnit(format)?.UnitId;
            }

            if (string.IsNullOrEmpty(fallback)) return false;

            foreach (var tier in CoreTierOrder)
                tierIds[tier] = fallback;

            DebugAds.Log($"[RemoteConfig] Core {format} all RC tiers empty → fallback {fallback}");
            return true;
        }

        static string GetCoreRemoteConfigKey(AdsFormatType format, CoreAdTier tier)
        {
            var isInter = format == AdsFormatType.Interstitial;
            return tier switch
            {
                CoreAdTier.High => isInter ? Keys.key_remote_inter_high_id : Keys.key_remote_reward_high_id,
                CoreAdTier.Mid => isInter ? Keys.key_remote_inter_mid_id : Keys.key_remote_reward_mid_id,
                CoreAdTier.Low => isInter ? Keys.key_remote_inter_low_id : Keys.key_remote_reward_low_id,
                _ => null
            };
        }

        static void SetCoreTierUnitId(TierUnit tierUnit, CoreAdTier tier, string unitId)
        {
            switch (tier)
            {
                case CoreAdTier.High: tierUnit.High = unitId; break;
                case CoreAdTier.Mid: tierUnit.Mid = unitId; break;
                case CoreAdTier.Low: tierUnit.Low = unitId; break;
            }
        }

        /// <summary>Single fallback unit ID from JisSDKAdsSettings when every RC tier is empty.</summary>
        public static string ResolveDefaultFallbackUnitId(
            SequentialTierAdFormat format,
            PlatformAdsProfile profile)
        {
            if (profile == null) return null;

#if UNITY_AD_ADMOB
            var admob = profile.sdkSetup?.admobAdsSetup;
            if (admob != null)
            {
                var seqConfig = format == SequentialTierAdFormat.Interstitial
                    ? admob.InterstitialTierConfig
                    : admob.RewardedTierConfig;

                var fromDefault = seqConfig?.ResolveDefaultAdUnitId();
                if (!string.IsNullOrEmpty(fromDefault)) return fromDefault;

                var list = format == SequentialTierAdFormat.Interstitial
                    ? admob.InterstitialAdUnitIDList
                    : admob.RewardedAdUnitIDList;
                if (list != null && list.Count > 0 && !string.IsNullOrWhiteSpace(list[0]))
                    return list[0].Trim();
            }
#endif
#if UNITY_AD_MAX
            if (profile.mediation == AdsMediationType.MAX && profile.sdkSetup?.maxAdsSetup != null)
            {
                var max = profile.sdkSetup.maxAdsSetup;
                var primary = format == SequentialTierAdFormat.Interstitial
                    ? max.InterstitialAdUnitID
                    : max.RewardedAdUnitID;
                if (!string.IsNullOrEmpty(primary)) return primary;
            }
#endif
            var tiered = profile.tieredAdsConfig;
            if (tiered != null)
            {
                var legacy = format == SequentialTierAdFormat.Interstitial
                    ? tiered.LegacyInterstitial.UnitId
                    : tiered.LegacyRewarded.UnitId;
                if (!string.IsNullOrEmpty(legacy)) return legacy;
            }

            return null;
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
