using UnityEngine;

namespace JisSDKAds.Ads.SequentialTier
{
    /// <summary>Exponential backoff: 2^n seconds per fail count (1-based), capped at 64s.</summary>
    public static class RetryBackoff
    {
        public const float MaxDelaySeconds = 64f;

        public static float GetDelaySeconds(int failCount)
        {
            if (failCount <= 0)
                return 2f;

            return Mathf.Min(MaxDelaySeconds, Mathf.Pow(2f, failCount));
        }
    }
}
