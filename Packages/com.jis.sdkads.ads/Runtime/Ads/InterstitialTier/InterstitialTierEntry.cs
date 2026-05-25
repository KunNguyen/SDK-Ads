using System;
using UnityEngine;

namespace JisSDKAds.Ads.InterstitialTier
{
    [Serializable]
    public class InterstitialTierEntry
    {
        public AdTier tier = AdTier.Premium;

        [Tooltip("Android AdMob interstitial unit id for this tier.")]
        public string androidAdUnitId;

        [Tooltip("iOS AdMob interstitial unit id for this tier.")]
        public string iosAdUnitId;

        [Tooltip("Load timeout in seconds. Use -1 for package default; FILL uses 0 = no timeout.")]
        public float timeoutSeconds = -1f;

        public bool HasUnitId =>
            !string.IsNullOrWhiteSpace(androidAdUnitId) || !string.IsNullOrWhiteSpace(iosAdUnitId);

        public string ResolveAdUnitId()
        {
#if UNITY_IOS
            if (!string.IsNullOrWhiteSpace(iosAdUnitId)) return iosAdUnitId.Trim();
            return androidAdUnitId?.Trim();
#else
            if (!string.IsNullOrWhiteSpace(androidAdUnitId)) return androidAdUnitId.Trim();
            return iosAdUnitId?.Trim();
#endif
        }
    }
}
