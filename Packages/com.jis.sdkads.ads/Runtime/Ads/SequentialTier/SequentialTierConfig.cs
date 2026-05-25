using System;
using System.Collections.Generic;
using UnityEngine;

namespace JisSDKAds.Ads.SequentialTier
{
    [Serializable]
    public class SequentialTierConfig
    {
        [Tooltip("When true: sequential Premium→Fill ladder. When false: single default unit only.")]
        public bool enableSequentialLadder;

        [Tooltip("Fallback unit when ladder is off or a tier entry has no id.")]
        public string defaultAndroidAdUnitId;

        public string defaultIosAdUnitId;

        [Tooltip("Remember last successful tier; retry Premium after cooldown.")]
        public bool enableTierMemoryCooldown = true;

        [Min(1f)] public float premiumRetryCooldownMinutes = 45f;
        [Min(1)] public int consecutiveFailuresBeforeDowngrade = 2;

        [SerializeField] private SequentialTierEntry[] tiers = CreateDefaultTiers();

        /// <summary>Backward-compatible alias used by earlier interstitial-only builds.</summary>
        public bool enableTieredInterstitial
        {
            get => enableSequentialLadder;
            set => enableSequentialLadder = value;
        }

        public IReadOnlyList<SequentialTierEntry> Tiers => tiers ?? Array.Empty<SequentialTierEntry>();

        public static SequentialTierEntry[] CreateDefaultTiers() => new[]
        {
            new SequentialTierEntry { tier = AdTier.Premium, timeoutSeconds = 8f },
            new SequentialTierEntry { tier = AdTier.High, timeoutSeconds = 6f },
            new SequentialTierEntry { tier = AdTier.Mid, timeoutSeconds = 4f },
            new SequentialTierEntry { tier = AdTier.Low, timeoutSeconds = 3f },
            new SequentialTierEntry { tier = AdTier.Fill, timeoutSeconds = 0f }
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

        public SequentialTierEntry GetEntry(AdTier tier)
        {
            if (tiers == null) return null;
            foreach (var e in tiers)
            {
                if (e != null && e.tier == tier)
                    return e;
            }

            return null;
        }

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

        public void EnsureDefaultTierSlots()
        {
            if (tiers != null && tiers.Length >= 5) return;
            tiers = CreateDefaultTiers();
        }
    }
}
