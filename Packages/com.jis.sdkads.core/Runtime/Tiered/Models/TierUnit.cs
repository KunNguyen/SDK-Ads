using System;
using UnityEngine;

namespace JisSDKAds.Core.Tiered.Models
{
    [Serializable]
    public class TierUnit
    {
        public string High;
        public string Mid;
        public string Low;

        public string GetUnitId(AdTier tier)
        {
            return tier switch
            {
                AdTier.High => High,
                AdTier.Mid => Mid,
                AdTier.Low => Low,
                _ => null
            };
        }

        public bool HasAnyUnitId()
        {
            return !string.IsNullOrEmpty(High)
                   || !string.IsNullOrEmpty(Mid)
                   || !string.IsNullOrEmpty(Low);
        }
    }
}
