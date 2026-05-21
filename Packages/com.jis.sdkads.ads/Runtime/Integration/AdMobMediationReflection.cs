using System;
using System.Reflection;
using JisSDKAds.Ads.Settings;

namespace JisSDKAds.Ads.Integration
{
    /// <summary>
    /// Applies AdMob setup from <see cref="SDKSetup"/> via provider assembly (no compile-time reference).
    /// </summary>
    internal static class AdMobMediationReflection
    {
        const string BridgeTypeName = "JisSDKAds.Providers.AdMob.AdmobMediationConfigBridge, JisSDKAds.Providers.AdMob";

        public static void ApplySdkSetup(AdsManager manager, SDKSetup setup)
        {
            if (manager == null || setup == null) return;

            var bridge = Type.GetType(BridgeTypeName);
            var method = bridge?.GetMethod("ApplyFromSdkSetup", BindingFlags.Public | BindingFlags.Static);
            if (method == null) return;

            method.Invoke(null, new object[] { manager, setup });
        }
    }
}
