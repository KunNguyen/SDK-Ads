using System.Collections.Generic;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Common;
using JisSDKAds.Firebase;
using UnityEngine;

namespace JisSDKAds.Ads.SequentialTier
{
    /// <summary>
    /// Reads sequential tier AdMob unit IDs from Firebase Remote Config and applies them to tier entries.
    /// RC tiers are resolved high→low with cascade; settings fallback is used only when all RC tiers are empty.
    /// </summary>
    public static class SequentialTierRemoteConfigResolver
    {
        static readonly AdTier[] TierOrder =
        {
            AdTier.Premium, AdTier.High, AdTier.Mid, AdTier.Low, AdTier.Fill
        };

        public static bool RequiresFetchBeforeAds(SDKSetup setup) =>
            AdInventoryRemoteConfigResolver.RequiresFetchBeforeAds(setup);

        public static bool TryReadTierIds(
            SequentialTierAdFormat format,
            out Dictionary<AdTier, string> tierIds) =>
            TryReadTierIds(format, profile: null, out tierIds);

        public static bool TryReadTierIds(
            SequentialTierAdFormat format,
            PlatformAdsProfile profile,
            out Dictionary<AdTier, string> tierIds)
            => TryReadTierIds(format, profile, profile?.mediation ?? AdsMediationType.NONE, out tierIds);

        public static bool TryReadTierIds(
            SequentialTierAdFormat format,
            PlatformAdsProfile profile,
            AdsMediationType mediation,
            out Dictionary<AdTier, string> tierIds)
        {
            if (profile != null)
                return SequentialTierUnitIdResolver.TryBuildTierIds(format, profile, mediation, out tierIds);

            tierIds = new Dictionary<AdTier, string>();
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
                return false;

            var anyRc = false;
            string cascadeId = null;

            foreach (var tier in TierOrder)
            {
                var key = GetRemoteConfigKey(format, tier);
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

            return anyRc && tierIds.Count > 0;
        }

        /// <summary>Applies RC + settings fallback to AdMob sequential tier configs (Core / JisAds path without AdsManager).</summary>
        public static void ApplyResolvedIdsToAdmobSetup(PlatformAdsProfile profile)
        {
            ApplyResolvedIdsToMediationSetup(profile, AdsMediationType.ADMOB);
        }

        public static void ApplyResolvedIdsToAllMediationSetups(PlatformAdsProfile profile)
        {
            ApplyResolvedIdsToMediationSetup(profile, AdsMediationType.ADMOB);
            ApplyResolvedIdsToMediationSetup(profile, AdsMediationType.MAX);
        }

        public static void ApplyResolvedIdsToMediationSetup(PlatformAdsProfile profile, AdsMediationType mediation)
        {
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
                return;

            var interstitialConfig = GetTierConfig(profile, mediation, SequentialTierAdFormat.Interstitial);
            var rewardedConfig = GetTierConfig(profile, mediation, SequentialTierAdFormat.Rewarded);

            ApplyResolvedIdsToFormat(
                profile,
                mediation,
                SequentialTierAdFormat.Interstitial,
                interstitialConfig);
            ApplyResolvedIdsToFormat(
                profile,
                mediation,
                SequentialTierAdFormat.Rewarded,
                rewardedConfig);
        }

        static void ApplyResolvedIdsToFormat(
            PlatformAdsProfile profile,
            AdsMediationType mediation,
            SequentialTierAdFormat format,
            SequentialTierConfig config)
        {
            if (config == null)
                return;

            if (!config.enableSequentialLadder)
            {
                ApplyToConfig(config, null);
                return;
            }

            if (TryReadTierIds(format, profile, mediation, out var ids))
            {
                ApplyToConfig(config, ids);
                LogAppliedIds(format, config);
                return;
            }

            ApplyToConfig(config, null);
        }

        public static void ApplyToConfig(
            SequentialTierConfig config,
            IReadOnlyDictionary<AdTier, string> remoteTierIds)
        {
            if (config == null) return;
            config.EnsureDefaultTierSlots();

            foreach (var entry in config.Tiers)
                entry?.ClearRemoteAdUnitId();

            if (remoteTierIds == null || remoteTierIds.Count == 0) return;

            foreach (var pair in remoteTierIds)
            {
                var entry = config.GetEntry(pair.Key);
                if (entry == null || string.IsNullOrWhiteSpace(pair.Value)) continue;
                entry.SetRemoteAdUnitId(pair.Value);
            }
        }

        public static string GetRemoteConfigKey(SequentialTierAdFormat format, AdTier tier) =>
            format switch
            {
                SequentialTierAdFormat.Interstitial => tier switch
                {
                    AdTier.Premium => Keys.key_remote_inter_premium_id,
                    AdTier.High => Keys.key_remote_inter_high_id,
                    AdTier.Mid => Keys.key_remote_inter_mid_id,
                    AdTier.Low => Keys.key_remote_inter_low_id,
                    AdTier.Fill => Keys.key_remote_inter_fill_id,
                    _ => null
                },
                SequentialTierAdFormat.Rewarded => tier switch
                {
                    AdTier.Premium => Keys.key_remote_reward_premium_id,
                    AdTier.High => Keys.key_remote_reward_high_id,
                    AdTier.Mid => Keys.key_remote_reward_mid_id,
                    AdTier.Low => Keys.key_remote_reward_low_id,
                    AdTier.Fill => Keys.key_remote_reward_fill_id,
                    _ => null
                },
                _ => null
            };

        public static string GetRemoteConfigKey(SequentialTierAdFormat format, AdTier tier, AdsMediationType mediation)
        {
            if (mediation == AdsMediationType.MAX)
            {
                return format switch
                {
                    SequentialTierAdFormat.Interstitial => tier switch
                    {
                        AdTier.Premium => Keys.key_remote_max_inter_premium_id,
                        AdTier.High => Keys.key_remote_max_inter_high_id,
                        AdTier.Mid => Keys.key_remote_max_inter_mid_id,
                        AdTier.Low => Keys.key_remote_max_inter_low_id,
                        AdTier.Fill => Keys.key_remote_max_inter_fill_id,
                        _ => null
                    },
                    SequentialTierAdFormat.Rewarded => tier switch
                    {
                        AdTier.Premium => Keys.key_remote_max_reward_premium_id,
                        AdTier.High => Keys.key_remote_max_reward_high_id,
                        AdTier.Mid => Keys.key_remote_max_reward_mid_id,
                        AdTier.Low => Keys.key_remote_max_reward_low_id,
                        AdTier.Fill => Keys.key_remote_max_reward_fill_id,
                        _ => null
                    },
                    _ => null
                };
            }

            if (mediation == AdsMediationType.ADMOB)
            {
                return format switch
                {
                    SequentialTierAdFormat.Interstitial => tier switch
                    {
                        AdTier.Premium => Keys.key_remote_admob_inter_premium_id,
                        AdTier.High => Keys.key_remote_admob_inter_high_id,
                        AdTier.Mid => Keys.key_remote_admob_inter_mid_id,
                        AdTier.Low => Keys.key_remote_admob_inter_low_id,
                        AdTier.Fill => Keys.key_remote_admob_inter_fill_id,
                        _ => null
                    },
                    SequentialTierAdFormat.Rewarded => tier switch
                    {
                        AdTier.Premium => Keys.key_remote_admob_reward_premium_id,
                        AdTier.High => Keys.key_remote_admob_reward_high_id,
                        AdTier.Mid => Keys.key_remote_admob_reward_mid_id,
                        AdTier.Low => Keys.key_remote_admob_reward_low_id,
                        AdTier.Fill => Keys.key_remote_admob_reward_fill_id,
                        _ => null
                    },
                    _ => null
                };
            }

            return GetRemoteConfigKey(format, tier);
        }

        static SequentialTierConfig GetTierConfig(
            PlatformAdsProfile profile,
            AdsMediationType mediation,
            SequentialTierAdFormat format)
        {
            var setup = profile?.sdkSetup;
            if (setup == null)
                return null;

            return mediation switch
            {
                AdsMediationType.ADMOB => format == SequentialTierAdFormat.Interstitial
                    ? setup.admobAdsSetup?.InterstitialTierConfig
                    : setup.admobAdsSetup?.RewardedTierConfig,
                AdsMediationType.MAX => format == SequentialTierAdFormat.Interstitial
                    ? setup.maxAdsSetup?.InterstitialTierConfig
                    : setup.maxAdsSetup?.RewardedTierConfig,
                _ => null
            };
        }

        public static void LogAppliedIds(SequentialTierAdFormat format, SequentialTierConfig config)
        {
            if (config == null) return;
            foreach (var tier in TierOrder)
            {
                var entry = config.GetEntry(tier);
                if (entry == null || !entry.HasUnitId) continue;
                DebugAds.Log($"[RemoteConfig] {format} {tier} → {entry.ResolveAdUnitId()}");
            }
        }
    }
}
