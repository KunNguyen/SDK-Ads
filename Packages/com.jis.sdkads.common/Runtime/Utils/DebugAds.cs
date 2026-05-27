using UnityEngine;

namespace JisSDKAds.Common
{
    /// <summary>
    /// Gated ads SDK logging. Enable via JisSDKAdsSettings.enableAdsDebugLogging.
    /// </summary>
    public static class DebugAds
    {
        static bool _configured;
        static bool _enabled;

        /// <summary>Apply from <see cref="JisSDKAds.Ads.Settings.JisSDKAdsSettings"/> (runtime or Apply to Scene).</summary>
        public static void Configure(bool enabled)
        {
            _enabled = enabled;
            _configured = true;
        }

        public static bool IsEnabled => IsDebugMode();

        public static void Log(string message)
        {
            if (!IsDebugMode())
                return;
            Debug.Log(message);
        }

        public static void LogError(string message)
        {
            if (!IsDebugMode())
                return;
            Debug.LogError(message);
        }

        public static void LogWarning(string message)
        {
            if (!IsDebugMode())
                return;
            Debug.LogWarning(message);
        }

        public static void LogException(string message)
        {
            if (!IsDebugMode())
                return;
            Debug.LogException(new System.Exception(message));
        }

        public static void LogException(System.Exception exception)
        {
            if (!IsDebugMode())
                return;
            Debug.LogException(exception);
        }

        public static void LogException(string message, System.Exception exception)
        {
            if (!IsDebugMode())
                return;
            Debug.LogException(new System.Exception(message, exception));
        }

        public static void LogFormat(string format, params object[] args)
        {
            if (!IsDebugMode())
                return;
            Debug.LogFormat(format, args);
        }

        /// <summary>SDK / mediation init step (success or failure + detail).</summary>
        public static void LogSdkInit(string source, string step, bool success, string detail = null)
        {
            if (!IsDebugMode())
                return;

            var status = success ? "OK" : "FAIL";
            var msg = $"[JIS Ads][Init][{status}] {source} → {step}";
            if (!string.IsNullOrEmpty(detail))
                msg += $" | {detail}";

            if (success)
                Debug.Log(msg);
            else
                Debug.LogError(msg);
        }

        /// <summary>Ad load / show lifecycle with unit id and optional error.</summary>
        public static void LogAdEvent(
            string format,
            string phase,
            string unitId = null,
            string tierOrMediation = null,
            string error = null)
        {
            if (!IsDebugMode())
                return;

            var msg = $"[JIS Ads][{format}][{phase}]";
            if (!string.IsNullOrEmpty(unitId))
                msg += $" unitId={unitId}";
            if (!string.IsNullOrEmpty(tierOrMediation))
                msg += $" {tierOrMediation}";
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(msg + $" | error={error}");
                return;
            }

            Debug.Log(msg);
        }

        static bool IsDebugMode()
        {
            if (_configured)
                return _enabled;

#if UNITY_EDITOR
            return true;
#elif DEBUG_ADS
            return true;
#else
            return false;
#endif
        }
    }
}
