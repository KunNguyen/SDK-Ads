using System;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Common;

namespace JisSDKAds.Ads.Integration
{
    internal static class MaxSdkEarlyInitBridge
    {
        const string InitializerTypeName =
            "JisSDKAds.Providers.Max.MaxSdkInitializer, JisSDKAds.Providers.Max";

        public static void TryWarmUpFromSettings(JisSDKAdsSettings settings)
        {
            var profile = settings?.GetActiveProfile();
            var setup = profile?.sdkSetup;
            var sdkKey = setup?.maxAdsSetup?.SDKKey;
            if (string.IsNullOrWhiteSpace(sdkKey))
                return;

            if (!ShouldWarmUpMax(settings, profile))
                return;

            var type = Type.GetType(InitializerTypeName);
            if (type == null)
            {
                DebugAds.LogWarning(
                    "[JisAds] MAX provider not loaded - early MaxSdk.InitializeSdk skipped (UNITY_AD_MAX / recompile).");
                return;
            }

            var method = type.GetMethod(
                "EnsureInitializedEarly",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method == null)
            {
                DebugAds.LogWarning("[JisAds] MaxSdkInitializer.EnsureInitializedEarly not found.");
                return;
            }

            DebugAds.LogSdkInit("JisAds", "MAX early SDK warm-up", true,
                "parallel with Firebase Remote Config");
            method.Invoke(null, new object[] { sdkKey });
        }

        static bool ShouldWarmUpMax(JisSDKAdsSettings settings, PlatformAdsProfile profile)
        {
            if (settings == null || profile == null)
                return false;

            if (profile.mediation == AdsMediationType.MAX)
                return true;

            var setup = profile.sdkSetup;
            if (setup != null)
            {
                if (setup.GetAdsMediationType(AdsType.BANNER) == AdsMediationType.MAX
                    || setup.GetAdsMediationType(AdsType.INTERSTITIAL) == AdsMediationType.MAX
                    || setup.GetAdsMediationType(AdsType.REWARDED) == AdsMediationType.MAX
                    || setup.GetAdsMediationType(AdsType.APP_OPEN) == AdsMediationType.MAX)
                    return true;
            }

            return settings.autoShowFirstMediation == AdsMediationType.MAX
                   || settings.autoShowSecondMediation == AdsMediationType.MAX;
        }
    }
}
