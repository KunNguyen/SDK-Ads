using UnityEngine;

namespace JisSDKAds.Core.Tiered.Logging
{
    public static class TieredAdsLogger
    {
        const string Tag = "[TieredAds]";

        public static bool Verbose;

        public static void Log(string message)
        {
            Debug.Log($"{Tag} {message}");
        }

        public static void LogVerbose(string message)
        {
            if (Verbose)
                Debug.Log($"{Tag} {message}");
        }

        public static void Warn(string message)
        {
            Debug.LogWarning($"{Tag} {message}");
        }

        public static void Error(string message)
        {
            Debug.LogError($"{Tag} {message}");
        }
    }
}
