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
            if (method == null)
            {
#if UNITY_EDITOR
                UnityEngine.Debug.LogWarning(
                    "[JIS SDK] MAX provider bridge not available (UNITY_AD_MAX undefined or recompile pending). " +
                    "Click Apply again after Unity finishes recompiling.");
#endif
                return;
            }

            method.Invoke(null, new object[] { manager, setup });
        }
    }
}
