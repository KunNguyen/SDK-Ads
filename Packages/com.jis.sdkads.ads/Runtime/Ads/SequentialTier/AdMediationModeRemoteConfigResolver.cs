using JisSDKAds.Ads.Settings;
using JisSDKAds.Common;
using JisSDKAds.Firebase;

namespace JisSDKAds.Ads.SequentialTier
{
    /// <summary>
    /// Reads fullscreen Single vs Multiple mediation mode from Firebase Remote Config.
    /// Values: "single" (default) | "multiple".
    /// </summary>
    public static class AdMediationModeRemoteConfigResolver
    {
        public static void ApplyMediationModesFromRemoteConfig(JisSDKAdsSettings settings)
        {
            if (settings == null)
                return;

            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
            {
                DebugAds.LogWarning("[RemoteConfig] Mediation mode not ready - using editor defaults.");
                return;
            }

            var globalValue = FirebaseManager.Instance.GetConfigString(Keys.key_remote_fullscreen_mediation_mode);
            var interstitialValue = FirebaseManager.Instance.GetConfigString(Keys.key_remote_interstitial_mediation_mode);
            var rewardedValue = FirebaseManager.Instance.GetConfigString(Keys.key_remote_rewarded_mediation_mode);
            var priorityValue = FirebaseManager.Instance.GetConfigString(Keys.key_remote_fullscreen_mediation_priority);

            if (!string.IsNullOrWhiteSpace(globalValue))
            {
                var globalMode = ParseMediationMode(globalValue, settings.interstitialMediationMode);
                settings.interstitialMediationMode = globalMode;
                settings.rewardedMediationMode = globalMode;
            }

            if (!string.IsNullOrWhiteSpace(interstitialValue))
                settings.interstitialMediationMode = ParseMediationMode(interstitialValue, settings.interstitialMediationMode);

            if (!string.IsNullOrWhiteSpace(rewardedValue))
                settings.rewardedMediationMode = ParseMediationMode(rewardedValue, settings.rewardedMediationMode);

            if (TryParseMediationPriority(priorityValue, out var first, out var second))
            {
                settings.autoShowFirstMediation = first;
                settings.autoShowSecondMediation = second;
                DebugAds.Log($"[RemoteConfig] Fullscreen mediation priority - {first}>{second}");
            }

            settings.singleMediationOnly = !settings.UsesAnyFullscreenMultipleMediation();
            DebugAds.Log(
                $"[RemoteConfig] Mediation mode - interstitial: {settings.interstitialMediationMode}, rewarded: {settings.rewardedMediationMode}");
        }

        static AdFormatMediationMode ParseMediationMode(string value, AdFormatMediationMode fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            switch (value.Trim().ToLowerInvariant())
            {
                case "multiple":
                case "multi":
                case "fallback":
                case "2":
                case "true":
                case "yes":
                    return AdFormatMediationMode.Multiple;
                case "single":
                case "1":
                case "false":
                case "no":
                    return AdFormatMediationMode.Single;
                default:
                    return fallback;
            }
        }

        static bool TryParseMediationPriority(
            string value,
            out AdsMediationType first,
            out AdsMediationType second)
        {
            first = AdsMediationType.NONE;
            second = AdsMediationType.NONE;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            var normalized = value
                .Trim()
                .ToLowerInvariant()
                .Replace("first", string.Empty)
                .Replace("primary", string.Empty)
                .Replace("secondary", string.Empty);
            var tokens = normalized.Split(new[] { ',', '>', '|', '/', ';', ' ', '-', '_' }, System.StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                var mediation = ParseMediation(token);
                if (mediation == AdsMediationType.NONE)
                    continue;

                if (first == AdsMediationType.NONE)
                {
                    first = mediation;
                    continue;
                }

                if (mediation != first)
                {
                    second = mediation;
                    break;
                }
            }

            if (first == AdsMediationType.NONE)
                return false;

            if (second == AdsMediationType.NONE)
                second = first == AdsMediationType.ADMOB ? AdsMediationType.MAX : AdsMediationType.ADMOB;

            return second != AdsMediationType.NONE && second != first;
        }

        static AdsMediationType ParseMediation(string token)
        {
            switch (token)
            {
                case "admob":
                case "google":
                case "ga":
                    return AdsMediationType.ADMOB;
                case "max":
                case "applovin":
                case "applovinmax":
                    return AdsMediationType.MAX;
                default:
                    return AdsMediationType.NONE;
            }
        }
    }
}
