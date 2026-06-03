using System;
using UnityEngine;

namespace JisSDKAds.Ads.SequentialTier
{
    public sealed class SequentialTierMemory
    {
        readonly string _keyLastTier;
        readonly string _keyLastSuccessUtc;
        readonly string _keyLadderFailStreak;
        readonly string _keyLastShowTier;
        readonly string _keyLastShowUtc;

        public AdTier LastSuccessTier { get; private set; } = AdTier.Premium;
        public DateTime LastSuccessUtc { get; private set; }
        public int LadderFailStreak { get; private set; }

        public AdTier LastShowSuccessTier { get; private set; } = AdTier.Premium;
        public DateTime LastShowSuccessUtc { get; private set; }

        public SequentialTierMemory(string formatKey)
        {
            _keyLastTier = $"jis.{formatKey}.tier.last";
            _keyLastSuccessUtc = $"jis.{formatKey}.tier.last_utc";
            _keyLadderFailStreak = $"jis.{formatKey}.tier.fail_streak";
            _keyLastShowTier = $"jis.{formatKey}.tier.last_show";
            _keyLastShowUtc = $"jis.{formatKey}.tier.last_show_utc";
        }

        public void Load()
        {
            LastSuccessTier = (AdTier)Mathf.Clamp(PlayerPrefs.GetInt(_keyLastTier, (int)AdTier.Premium), 0, (int)AdTier.Fill);
            var ticks = long.Parse(PlayerPrefs.GetString(_keyLastSuccessUtc, "0"));
            LastSuccessUtc = ticks > 0 ? new DateTime(ticks, DateTimeKind.Utc) : DateTime.MinValue;
            LadderFailStreak = PlayerPrefs.GetInt(_keyLadderFailStreak, 0);

            LastShowSuccessTier = (AdTier)Mathf.Clamp(PlayerPrefs.GetInt(_keyLastShowTier, (int)AdTier.Premium), 0, (int)AdTier.Fill);
            var showTicks = long.Parse(PlayerPrefs.GetString(_keyLastShowUtc, "0"));
            LastShowSuccessUtc = showTicks > 0 ? new DateTime(showTicks, DateTimeKind.Utc) : DateTime.MinValue;
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

        public void RecordShowSuccess(AdTier tier)
        {
            LastShowSuccessTier = tier;
            LastShowSuccessUtc = DateTime.UtcNow;
            PlayerPrefs.SetInt(_keyLastShowTier, (int)LastShowSuccessTier);
            PlayerPrefs.SetString(_keyLastShowUtc, LastShowSuccessUtc.Ticks.ToString());
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

            if (config.returnToPremiumAfterNonPremiumShowMinutes > 0f
                && LastShowSuccessUtc != DateTime.MinValue
                && LastShowSuccessTier != AdTier.Premium)
            {
                var sinceShow = DateTime.UtcNow - LastShowSuccessUtc;
                if (sinceShow.TotalMinutes >= config.returnToPremiumAfterNonPremiumShowMinutes)
                    return AdTier.Premium;
            }

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
