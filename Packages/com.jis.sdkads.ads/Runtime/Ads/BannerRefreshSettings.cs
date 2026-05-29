using JisSDKAds.Common;
using JisSDKAds.Firebase;

namespace JisSDKAds.Ads
{
    /// <summary>
    /// Banner uses a single ad unit only (no tiered inventory). Auto-refresh is optional via local setup
    /// and Firebase Remote Config (<see cref="Keys.key_remote_banner_auto_refresh"/>).
    /// </summary>
    public static class BannerRefreshSettings
    {
        public const float DefaultIntervalSeconds = 15f;

        public static void Resolve(SDKSetup setup, out bool autoRefreshEnabled, out float intervalSeconds)
        {
            autoRefreshEnabled = setup != null && setup.isAutoRefreshBannerByCode;
            intervalSeconds = setup != null && setup.bannerAutoRefreshIntervalSeconds > 0f
                ? setup.bannerAutoRefreshIntervalSeconds
                : DefaultIntervalSeconds;

            if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsRemoteConfigReady)
                return;

            autoRefreshEnabled = FirebaseManager.Instance
                .GetConfigValue(Keys.key_remote_banner_auto_refresh).BooleanValue;
            var rcSeconds = FirebaseManager.Instance
                .GetConfigValue(Keys.key_remote_banner_auto_refresh_time).DoubleValue;
            if (rcSeconds > 0)
                intervalSeconds = (float)rcSeconds;
        }

        /// <summary>First non-empty AdMob banner unit id for the active platform (single slot).</summary>
        public static string ResolveSingleBannerUnitId(SDKSetup setup)
        {
            if (setup?.admobAdsSetup == null)
                return string.Empty;

            setup.admobAdsSetup.EnsureInitialized();
            var list = setup.admobAdsSetup.BannerAdUnitIDList;
            if (list == null || list.Count == 0)
                return string.Empty;

            foreach (var id in list)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    return id.Trim();
            }

            return string.Empty;
        }
    }
}
