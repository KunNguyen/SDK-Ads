using JisSDKAds.Core.Tiered.Models;
using UnityEngine;

namespace JisSDKAds.Core.Tiered.Config
{
    [CreateAssetMenu(fileName = "TieredAdsConfig", menuName = "JIS SDK/Tiered Ads Config", order = 2)]
    public class TieredAdsConfig : ScriptableObject
    {
        [Header("Feature Flags")]
        public bool EnableTieredInventory;
        public bool EnableTieredInventoryForInterstitial = true;
        public bool EnableTieredInventoryForRewarded = true;
        public bool EnableDynamicPromotion = true;
        public bool PreferLastSuccessfulTier = true;

        [Header("Scheduler")]
        public float DelayBetweenLoads = 0.75f;
        public int MaxParallelLoads = 1;
        public float TierDisableDuration = 120f;
        public float PromotionLockDuration = 60f;
        public int RollingWindowSize = 20;

        [Header("Tier Unit IDs")]
        public TierUnit Interstitial = new TierUnit();
        public TierUnit Rewarded = new TierUnit();

        [Header("Legacy Single Unit (Mode A fallback)")]
        public SingleUnitConfig LegacyInterstitial = new SingleUnitConfig();
        public SingleUnitConfig LegacyRewarded = new SingleUnitConfig();

        public bool IsTieredEnabledFor(AdsFormatType format)
        {
            if (!EnableTieredInventory) return false;
            return format switch
            {
                AdsFormatType.Interstitial => EnableTieredInventoryForInterstitial,
                AdsFormatType.Rewarded => EnableTieredInventoryForRewarded,
                _ => false
            };
        }

        public TierUnit GetTierUnit(AdsFormatType format)
        {
            return format switch
            {
                AdsFormatType.Interstitial => Interstitial,
                AdsFormatType.Rewarded => Rewarded,
                _ => null
            };
        }

        public SingleUnitConfig GetLegacyUnit(AdsFormatType format)
        {
            return format switch
            {
                AdsFormatType.Interstitial => LegacyInterstitial,
                AdsFormatType.Rewarded => LegacyRewarded,
                _ => null
            };
        }
    }
}
