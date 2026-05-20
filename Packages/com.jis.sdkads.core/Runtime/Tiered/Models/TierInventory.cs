using System;

namespace JisSDKAds.Core.Tiered.Models
{
    [Serializable]
    public class TierInventory
    {
        public AdsFormatType AdsType;
        public TierAdUnit High = new TierAdUnit { Tier = AdTier.High };
        public TierAdUnit Mid = new TierAdUnit { Tier = AdTier.Mid };
        public TierAdUnit Low = new TierAdUnit { Tier = AdTier.Low };
        public AdTier LastSuccessfulTier = AdTier.High;
        public AdTier CurrentPrimaryTier = AdTier.High;
        public DateTime PromotionLockUntil = DateTime.MinValue;

        public bool IsPromotionLocked => DateTime.UtcNow < PromotionLockUntil;

        public TierAdUnit GetUnit(AdTier tier)
        {
            return tier switch
            {
                AdTier.High => High,
                AdTier.Mid => Mid,
                AdTier.Low => Low,
                _ => null
            };
        }

        public TierAdUnit[] AllUnits => new[] { High, Mid, Low };

        public void SetPromotionLock(float durationSeconds)
        {
            PromotionLockUntil = DateTime.UtcNow.AddSeconds(durationSeconds);
        }
    }

    public enum AdsFormatType
    {
        Interstitial = 0,
        Rewarded = 1
    }
}
