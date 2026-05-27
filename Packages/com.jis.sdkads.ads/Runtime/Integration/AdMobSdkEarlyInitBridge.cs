using System;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Common;
using UnityEngine;

namespace JisSDKAds.Ads.Integration
{
    /// <summary>
    /// Starts <c>MobileAds.Initialize</c> via the AdMob provider assembly when active profile uses AdMob,
    /// without a compile-time reference to <c>JisSDKAds.Providers.AdMob</c>.
    /// </summary>
    internal static class AdMobSdkEarlyInitBridge
    {
        const string InitializerTypeName =
            "JisSDKAds.Providers.AdMob.AdMobMobileAdsInitializer, JisSDKAds.Providers.AdMob";

        public static void TryWarmUpFromSettings(JisSDKAdsSettings settings)
        {
            if (settings == null || settings.GetActiveMediation() != AdsMediationType.ADMOB)
                return;

            var requestConsent = ShouldRequestConsent();
            var type = Type.GetType(InitializerTypeName);
            if (type == null)
            {
                DebugAds.LogWarning(
                    "[JisAds] AdMob provider not loaded — early MobileAds.Initialize skipped (UNITY_AD_ADMOB / recompile).");
                return;
            }

            var method = type.GetMethod(
                "EnsureInitializedEarly",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method == null)
            {
                DebugAds.LogWarning("[JisAds] AdMobMobileAdsInitializer.EnsureInitializedEarly not found.");
                return;
            }

            DebugAds.LogSdkInit("JisAds", "AdMob early SDK warm-up", true,
                $"requestConsent={requestConsent} (parallel with Firebase Remote Config)");
            method.Invoke(null, new object[] { requestConsent });
        }

        static bool ShouldRequestConsent()
        {
#if UNITY_EDITOR
            return false;
#else
            return true;
#endif
        }
    }
}
