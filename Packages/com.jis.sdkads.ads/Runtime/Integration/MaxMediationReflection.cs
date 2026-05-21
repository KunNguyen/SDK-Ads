using System;
using System.Reflection;

namespace JisSDKAds.Ads.Integration
{
    internal static class MaxMediationReflection
    {
        const string BridgeTypeName = "JisSDKAds.Providers.Max.MaxMediationConfigBridge, JisSDKAds.Providers.Max";

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
