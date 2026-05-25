using System.Text;
using JisSDKAds.Ads.InterstitialTier;
using JisSDKAds.Common;

namespace JisSDKAds.Providers.AdMob.InterstitialTier
{
    internal static class InterstitialTierAnalytics
    {
        public static void LogLoadStart(string adUnitId, AdTier tier) =>
            Log("interstitial_load_start", adUnitId, tier);

        public static void LogLoadSuccess(string adUnitId, AdTier tier, long loadDurationMs) =>
            Log("interstitial_load_success", adUnitId, tier, loadDurationMs: loadDurationMs);

        public static void LogLoadFail(string adUnitId, AdTier tier, int errorCode, string errorMessage,
            long loadDurationMs) =>
            Log("interstitial_load_fail", adUnitId, tier, errorCode, errorMessage, loadDurationMs);

        public static void LogLoadTimeout(string adUnitId, AdTier tier, long loadDurationMs) =>
            Log("interstitial_load_timeout", adUnitId, tier, loadDurationMs: loadDurationMs);

        public static void LogShowStart(string adUnitId, AdTier tier) =>
            Log("interstitial_show_start", adUnitId, tier);

        public static void LogShowSuccess(string adUnitId, AdTier tier) =>
            Log("interstitial_show_success", adUnitId, tier);

        public static void LogShowFail(string adUnitId, AdTier tier, int errorCode, string errorMessage) =>
            Log("interstitial_show_fail", adUnitId, tier, errorCode, errorMessage);

        public static void LogPaid(string adUnitId, AdTier tier, double revenue, string currency, int precision) =>
            Log("interstitial_paid_event", adUnitId, tier, revenue: revenue, currency: currency, precision: precision);

        static void Log(
            string eventName,
            string adUnitId,
            AdTier tier,
            int errorCode = 0,
            string errorMessage = null,
            long loadDurationMs = 0,
            double revenue = 0,
            string currency = null,
            int precision = 0)
        {
            var sb = new StringBuilder(128);
            sb.Append("[JIS Ads] ").Append(eventName);
            sb.Append(" adUnitId=").Append(adUnitId ?? "");
            sb.Append(" tier=").Append(tier);
            if (loadDurationMs > 0)
                sb.Append(" loadDurationMs=").Append(loadDurationMs);
            if (errorCode != 0 || !string.IsNullOrEmpty(errorMessage))
            {
                sb.Append(" errorCode=").Append(errorCode);
                sb.Append(" errorMessage=").Append(errorMessage ?? "");
            }

            if (revenue > 0 || !string.IsNullOrEmpty(currency))
            {
                sb.Append(" revenue=").Append(revenue);
                sb.Append(" currency=").Append(currency ?? "");
                sb.Append(" precision=").Append(precision);
            }

            DebugAds.Log(sb.ToString());
        }
    }
}
