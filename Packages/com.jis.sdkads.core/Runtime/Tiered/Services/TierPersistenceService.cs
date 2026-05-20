using System;
using JisSDKAds.Core.Tiered.Interfaces;
using JisSDKAds.Core.Tiered.Logging;
using JisSDKAds.Core.Tiered.Models;
using UnityEngine;

namespace JisSDKAds.Core.Tiered.Services
{
    public class TierPersistenceService : ITierPersistenceService
    {
        const string Prefix = "jis_tier_";

        public void SaveAll(TierInventory interstitial, TierInventory rewarded)
        {
            Save(interstitial);
            Save(rewarded);
            PlayerPrefs.Save();
        }

        public void LoadAll(TierInventory interstitial, TierInventory rewarded)
        {
            Load(interstitial);
            Load(rewarded);
        }

        public void Save(TierInventory inventory)
        {
            var key = GetKey(inventory.AdsType);
            PlayerPrefs.SetInt($"{key}_primary", (int)inventory.CurrentPrimaryTier);
            PlayerPrefs.SetInt($"{key}_last_success", (int)inventory.LastSuccessfulTier);
            PlayerPrefs.SetString($"{key}_lock_until", inventory.PromotionLockUntil.ToBinary().ToString());

            SaveUnit(inventory.AdsType, inventory.High);
            SaveUnit(inventory.AdsType, inventory.Mid);
            SaveUnit(inventory.AdsType, inventory.Low);
        }

        public void Load(TierInventory inventory)
        {
            var key = GetKey(inventory.AdsType);
            inventory.CurrentPrimaryTier = (AdTier)PlayerPrefs.GetInt($"{key}_primary", (int)AdTier.High);
            inventory.LastSuccessfulTier = (AdTier)PlayerPrefs.GetInt($"{key}_last_success", (int)AdTier.High);

            var lockBinary = PlayerPrefs.GetString($"{key}_lock_until", string.Empty);
            if (!string.IsNullOrEmpty(lockBinary) && long.TryParse(lockBinary, out var binary))
                inventory.PromotionLockUntil = DateTime.FromBinary(binary);

            LoadUnit(inventory.AdsType, inventory.High);
            LoadUnit(inventory.AdsType, inventory.Mid);
            LoadUnit(inventory.AdsType, inventory.Low);

            TieredAdsLogger.LogVerbose(
                $"Restored {inventory.AdsType}: primary={inventory.CurrentPrimaryTier}, lastSuccess={inventory.LastSuccessfulTier}");
        }

        static void SaveUnit(AdsFormatType format, TierAdUnit unit)
        {
            var key = $"{GetKey(format)}_{unit.Tier}";
            PlayerPrefs.SetInt($"{key}_fail", unit.FailCount);
            PlayerPrefs.SetInt($"{key}_success", unit.SuccessCount);
            PlayerPrefs.SetFloat($"{key}_fill", unit.FillRate);
            PlayerPrefs.SetString($"{key}_disabled_until", unit.TemporaryDisabledUntil.ToBinary().ToString());
            PlayerPrefs.SetInt($"{key}_disabled", unit.IsTemporarilyDisabled ? 1 : 0);
        }

        static void LoadUnit(AdsFormatType format, TierAdUnit unit)
        {
            var key = $"{GetKey(format)}_{unit.Tier}";
            unit.FailCount = PlayerPrefs.GetInt($"{key}_fail", 0);
            unit.SuccessCount = PlayerPrefs.GetInt($"{key}_success", 0);
            unit.FillRate = PlayerPrefs.GetFloat($"{key}_fill", 0f);
            unit.IsTemporarilyDisabled = PlayerPrefs.GetInt($"{key}_disabled", 0) == 1;

            var disabledUntil = PlayerPrefs.GetString($"{key}_disabled_until", string.Empty);
            if (!string.IsNullOrEmpty(disabledUntil) && long.TryParse(disabledUntil, out var binary))
                unit.TemporaryDisabledUntil = DateTime.FromBinary(binary);

            if (unit.IsTemporarilyDisabled && DateTime.UtcNow >= unit.TemporaryDisabledUntil)
                unit.ClearTemporaryDisable();
        }

        static string GetKey(AdsFormatType format) => Prefix + format.ToString().ToLowerInvariant();
    }
}
