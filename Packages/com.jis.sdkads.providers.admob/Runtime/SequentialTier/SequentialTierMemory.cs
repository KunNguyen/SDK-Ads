using System;
using JisSDKAds.Ads.SequentialTier;
using UnityEngine;

namespace JisSDKAds.Providers.AdMob.SequentialTier
{
    internal sealed class SequentialTierMemory
    {
        readonly string _keyLastTier;
        readonly string _keyLastSuccessUtc;
        readonly string _keyLadderFailStreak;

        public AdTier LastSuccessTier { get; private set; } = AdTier.Premium;
        public DateTime LastSuccessUtc { get; private set; }
        public int LadderFailStreak { get; private set; }

        public SequentialTierMemory(string formatKey)
        {
            _keyLastTier = $"jis.{formatKey}.tier.last";
            _keyLastSuccessUtc = $"jis.{formatKey}.tier.last_utc";
            _keyLadderFailStreak = $"jis.{formatKey}.tier.fail_streak";
        }

        public void Load()
        {
            LastSuccessTier = (AdTier)Mathf.Clamp(PlayerPrefs.GetInt(_keyLastTier, (int)AdTier.Premium), 0, (int)AdTier.Fill);
            var ticks = long.Parse(PlayerPrefs.GetString(_keyLastSuccessUtc, "0"));
            LastSuccessUtc = ticks > 0 ? new DateTime(ticks, DateTimeKind.Utc) : DateTime.MinValue;
            LadderFailStreak = PlayerPrefs.GetInt(_keyLadderFailStreak, 0);
        }

        public void RecordSuccess(AdTier tier)
        {
            LastSuccessTier = tier;
            LastSuccessUtc = DateTime.UtcNow;
            LadderFailStreak = 0;
            PlayerPrefs.SetInt(_keyLastTier, (int)LastSuccessTier);
            PlayerPrefs.SetString(_keyLastSuccessUtc, LastSuccessUtc.Ticks.ToString());
            PlayerPrefs.SetInt(_keyLadderFailStreak, 0);
            PlayerPrefs.Save();
        }

        public void RecordLadderFailure()
        {
            LadderFailStreak++;
            PlayerPrefs.SetInt(_keyLadderFailStreak, LadderFailStreak);
            PlayerPrefs.Save();
        }

        public AdTier ResolveStartTier(SequentialTierConfig config)
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
    }
}
