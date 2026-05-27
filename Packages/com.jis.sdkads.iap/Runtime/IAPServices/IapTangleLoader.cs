#if UNITY_IAP_ACTIVE
using System;
using System.Reflection;
using UnityEngine;

namespace JisSDKAds.IAP
{
    /// <summary>
    /// Loads obfuscated tangle bytes generated in the host project
    /// (Window &gt; Unity IAP &gt; IAP Receipt Validation Obfuscator).
    /// </summary>
    internal static class IapTangleLoader
    {
        const string GooglePlayTypeName = "GooglePlayTangle";

        public static bool TryGetTangleData(out byte[] googlePlay, out byte[] apple)
        {
            googlePlay = LoadTangleData(GooglePlayTypeName);
#if DEBUG_STOREKIT_TEST
            apple = LoadTangleData("AppleStoreKitTestTangle");
#else
            apple = LoadTangleData("AppleTangle");
#endif
            return googlePlay != null && apple != null;
        }

        static byte[] LoadTangleData(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = null;
                try
                {
                    type = assembly.GetType(typeName, false)
                        ?? assembly.GetType($"UnityEngine.Purchasing.Security.{typeName}", false);
                }
                catch
                {
                    // Ignore broken assembly type loads.
                }

                if (type == null)
                    continue;

                var method = type.GetMethod("Data", BindingFlags.Public | BindingFlags.Static);
                if (method == null || method.ReturnType != typeof(byte[]))
                    continue;

                try
                {
                    return method.Invoke(null, null) as byte[];
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[IAP] Failed to invoke {typeName}.Data(): {ex.Message}");
                    return null;
                }
            }

            return null;
        }
    }
}
#endif
