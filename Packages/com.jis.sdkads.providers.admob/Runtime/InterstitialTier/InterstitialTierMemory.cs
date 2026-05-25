using System;
using JisSDKAds.Ads.InterstitialTier;
using UnityEngine;

namespace JisSDKAds.Providers.AdMob.InterstitialTier
{
    /// <summary>Persists last successful tier and ladder failure streak for preload start tier.</summary>
    internal sealed class InterstitialTierMemory
    {
        private const string KeyLastTier = "jis.int.tier.last";
        private const string KeyLastSuccessUtc = "jis.int.tier.last_utc";
        private const string KeyLadderFailStreak = "jis.int.tier.fail_streak";

        public AdTier LastSuccessTier { get; private set; } = AdTier.Premium;
        public DateTime LastSuccessUtc { get; private set; }
        public int LadderFailStreak { get; private set; }

        public void Load()
        {
            LastSuccessTier = (AdTier)Mathf.Clamp(PlayerPrefs.GetInt(KeyLastTier, (int)AdTier.Premium), 0, (int)AdTier.Fill);
            var ticks = long.Parse(PlayerPrefs.GetString(KeyLastSuccessUtc, "0"));
            LastSuccessUtc = ticks > 0 ? new DateTime(ticks, DateTimeKind.Utc) : DateTime.MinValue;
            LadderFailStreak = PlayerPrefs.GetInt(KeyLadderFailStreak, 0);
        }

        public void RecordSuccess(AdTier tier)
        {
            LastSuccessTier = tier;
            LastSuccessUtc = DateTime.UtcNow;
            LadderFailStreak = 0;
            Save();
        }

        public void RecordLadderFailure()
        {
            LadderFailStreak++;
            PlayerPrefs.SetInt(KeyLadderFailStreak, LadderFailStreak);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Tier memory + cooldown: within cooldown start at last success; after cooldown restart at Premium.
        /// Applies downgrade steps after consecutive full-ladder failures.
        /// </summary>
        public AdTier ResolveStartTier(InterstitialTierConfig config)
        {
            if (!config.enableTierMemoryCooldown)
                return AdTier.Premium;

            var start = AdTier.Premium;
            if (LastSuccessUtc != DateTime.MinValue)
            {
                var elapsed = DateTime.UtcNow - LastSuccessUtc;
                if (elapsed.TotalMinutes < config.premiumRetryCooldownMinutes)
                    start = LastSuccessTier;
            }

            var downgrade = Mathf.Min(
                LadderFailStreak / Mathf.Max(1, config.consecutiveFailuresBeforeDowngrade),
                (int)start);
            return (AdTier)Mathf.Max((int)AdTier.Premium, (int)start - downgrade);
        }

        private void Save()
        {
            PlayerPrefs.SetInt(KeyLastTier, (int)LastSuccessTier);
            PlayerPrefs.SetString(KeyLastSuccessUtc, LastSuccessUtc.Ticks.ToString());
            PlayerPrefs.SetInt(KeyLadderFailStreak, 0);
            PlayerPrefs.Save();
        }
    }
}
