using System;
using System.Collections.Generic;
using UnityEngine;

namespace JisSDKAds.Ads.InterstitialTier
{
    [Serializable]
    public class InterstitialTierConfig
    {
        [Tooltip("When false, uses defaultInterstitialAdUnitId only (legacy single-unit flow).")]
        public bool enableTieredInterstitial;

        [Tooltip("Fallback unit when tier system is off or tier entry has no id.")]
        public string defaultAndroidAdUnitId;

        public string defaultIosAdUnitId;

        [Tooltip("Remember last successful tier and respect cooldown before trying Premium again.")]
        public bool enableTierMemoryCooldown = true;

        [Tooltip("Minutes after last success before ladder restarts at Premium.")]
        [Min(1f)] public float premiumRetryCooldownMinutes = 45f;

        [Tooltip("Consecutive full-ladder failures before lowering the remembered start tier.")]
        [Min(1)] public int consecutiveFailuresBeforeDowngrade = 2;

        [SerializeField] private InterstitialTierEntry[] tiers = CreateDefaultTiers();

        public IReadOnlyList<InterstitialTierEntry> Tiers => tiers ?? Array.Empty<InterstitialTierEntry>();

        public static InterstitialTierEntry[] CreateDefaultTiers() => new[]
        {
            new InterstitialTierEntry { tier = AdTier.Premium, timeoutSeconds = 8f },
            new InterstitialTierEntry { tier = AdTier.High, timeoutSeconds = 6f },
            new InterstitialTierEntry { tier = AdTier.Mid, timeoutSeconds = 4f },
            new InterstitialTierEntry { tier = AdTier.Low, timeoutSeconds = 3f },
            new InterstitialTierEntry { tier = AdTier.Fill, timeoutSeconds = 0f }
        };

        public string ResolveDefaultAdUnitId()
        {
#if UNITY_IOS
            if (!string.IsNullOrWhiteSpace(defaultIosAdUnitId)) return defaultIosAdUnitId.Trim();
            return defaultAndroidAdUnitId?.Trim();
#else
            if (!string.IsNullOrWhiteSpace(defaultAndroidAdUnitId)) return defaultAndroidAdUnitId.Trim();
            return defaultIosAdUnitId?.Trim();
#endif
        }

        public InterstitialTierEntry GetEntry(AdTier tier)
        {
            if (tiers == null) return null;
            foreach (var e in tiers)
            {
                if (e != null && e.tier == tier)
                    return e;
            }

            return null;
        }

        /// <summary>Default per-tier timeout (seconds). FILL returns 0 = no timeout.</summary>
        public float GetDefaultTimeoutSeconds(AdTier tier) => tier switch
        {
            AdTier.Premium => 8f,
            AdTier.High => 6f,
            AdTier.Mid => 4f,
            AdTier.Low => 3f,
            AdTier.Fill => 0f,
            _ => 5f
        };

        public float GetTimeoutSeconds(AdTier tier)
        {
            var entry = GetEntry(tier);
            if (entry != null && entry.timeoutSeconds >= 0f)
                return entry.timeoutSeconds;
            return GetDefaultTimeoutSeconds(tier);
        }

        public IEnumerable<AdTier> GetLadderFrom(AdTier start)
        {
            for (var t = start; t <= AdTier.Fill; t++)
                yield return t;
        }

        public void EnsureDefaultTierSlots()
        {
            if (tiers != null && tiers.Length >= 5) return;
            tiers = CreateDefaultTiers();
        }
    }
}
