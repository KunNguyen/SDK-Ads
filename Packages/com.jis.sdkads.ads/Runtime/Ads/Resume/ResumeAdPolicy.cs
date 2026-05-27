using JisSDKAds.Common;
using JisSDKAds.Firebase;

namespace JisSDKAds.Ads.Resume
{
    public sealed class ResumeAdPolicy
    {
        public bool IsEnabled { get; private set; } = true;
        public ResumeAdFormat Format { get; private set; } = ResumeAdFormat.AppOpen;
        public float MinPauseSeconds { get; private set; } = 5f;
        public float CappingSeconds { get; private set; } = 10f;

        public void ApplyFromRemoteConfig()
        {
            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
                return;

            IsEnabled = FirebaseManager.Instance.GetConfigBool(Keys.key_remote_ads_resume_ads_active);
            Format = ParseFormat(FirebaseManager.Instance.GetConfigString(Keys.key_remote_ads_resume_ads_type));
            MinPauseSeconds = (float)FirebaseManager.Instance.GetConfigDouble(Keys.key_remote_ads_resume_pause_time);
            CappingSeconds = (float)FirebaseManager.Instance.GetConfigDouble(Keys.key_remote_ads_resume_capping_time);

            DebugAds.Log($"[ResumeAd] policy enabled={IsEnabled} format={Format} pause>={MinPauseSeconds}s capping={CappingSeconds}s");
        }

        public static ResumeAdFormat ParseFormat(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return ResumeAdFormat.AppOpen;

            switch (raw.Trim().ToUpperInvariant())
            {
                case "INTERSTITIAL":
                    return ResumeAdFormat.Interstitial;
                case "APP_OPEN":
                default:
                    if (raw.Trim().ToUpperInvariant() != "APP_OPEN")
                        DebugAds.LogWarning($"[ResumeAd] Unknown ads_resume_type '{raw}', defaulting to APP_OPEN.");
                    return ResumeAdFormat.AppOpen;
            }
        }
    }
}
