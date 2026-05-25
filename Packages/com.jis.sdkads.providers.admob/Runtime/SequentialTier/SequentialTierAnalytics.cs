using System.Text;
using JisSDKAds.Ads.SequentialTier;
using JisSDKAds.Common;

namespace JisSDKAds.Providers.AdMob.SequentialTier
{
    internal static class SequentialTierAnalytics
    {
        public static void LogLoadStart(string format, string adUnitId, AdTier tier) =>
            Log(format, $"{format}_load_start", adUnitId, tier);

        public static void LogLoadSuccess(string format, string adUnitId, AdTier tier, long loadDurationMs) =>
            Log(format, $"{format}_load_success", adUnitId, tier, loadDurationMs: loadDurationMs);

        public static void LogLoadFail(string format, string adUnitId, AdTier tier, int errorCode,
            string errorMessage, long loadDurationMs) =>
            Log(format, $"{format}_load_fail", adUnitId, tier, errorCode, errorMessage, loadDurationMs);

        public static void LogLoadTimeout(string format, string adUnitId, AdTier tier, long loadDurationMs) =>
            Log(format, $"{format}_load_timeout", adUnitId, tier, loadDurationMs: loadDurationMs);

        public static void LogShowStart(string format, string adUnitId, AdTier tier) =>
            Log(format, $"{format}_show_start", adUnitId, tier);

        public static void LogShowSuccess(string format, string adUnitId, AdTier tier) =>
            Log(format, $"{format}_show_success", adUnitId, tier);

        public static void LogShowFail(string format, string adUnitId, AdTier tier, int errorCode, string errorMessage) =>
            Log(format, $"{format}_show_fail", adUnitId, tier, errorCode, errorMessage);

        public static void LogPaid(string format, string adUnitId, AdTier tier, double revenue, string currency,
            int precision) =>
            Log(format, $"{format}_paid_event", adUnitId, tier, revenue: revenue, currency: currency, precision: precision);

        static void Log(
            string format,
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
            sb.Append(" format=").Append(format);
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
