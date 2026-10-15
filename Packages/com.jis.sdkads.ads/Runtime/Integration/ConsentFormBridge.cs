using System;
using System.Reflection;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Common;

namespace JisSDKAds.Ads.Integration
{
    /// <summary>
    /// Opens the privacy-options (consent) form of the active mediation without a compile-time
    /// reference to the provider assemblies. MAX profile → MAX CMP; otherwise AdMob UMP.
    /// </summary>
    internal static class ConsentFormBridge
    {
        const string MaxTypeName = "JisSDKAds.Providers.Max.MaxConsentForm, JisSDKAds.Providers.Max";
        const string AdMobTypeName = "JisSDKAds.Ads.AdmobUmpConsent, JisSDKAds.Providers.AdMob";

        public static void Show(JisSDKAdsSettings settings, Action<string> onError, Action onDismiss)
        {
            var useMax = settings?.GetActiveProfile()?.mediation == AdsMediationType.MAX;
            if (useMax && TryInvoke(MaxTypeName, "ShowForExistingUser", onError, onDismiss))
                return;

            if (TryInvoke(AdMobTypeName, "ShowPrivacyOptionsForm", onError, onDismiss))
                return;

            DebugAds.LogWarning("[JisAds] No consent form provider available (MAX / AdMob assembly not loaded).");
            onError?.Invoke("No consent form provider available.");
        }

        static bool TryInvoke(string typeName, string methodName, Action<string> onError, Action onDismiss)
        {
            var method = Type.GetType(typeName)?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                return false;

            try
            {
                method.Invoke(null, new object[] { onError, onDismiss });
                return true;
            }
            catch (Exception e)
            {
                DebugAds.LogWarning($"[JisAds] Consent form call failed: {e.GetBaseException().Message}");
                onError?.Invoke(e.GetBaseException().Message);
                return true;
            }
        }
    }
}
