#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.Hub
{
    internal static class JisSDKHubPackageHealth
    {
        public static bool CommonAssemblyHasIapTypes() =>
            Type.GetType("JisSDKAds.Common.IapProductKind, JisSDKAds.Common") != null;

        public static void DrawIapCommonMismatchWarning()
        {
            if (!JisSDKHubManifest.HasDependency("com.jis.sdkads.iap"))
                return;

            if (CommonAssemblyHasIapTypes())
                return;

            EditorGUILayout.HelpBox(
                "com.jis.sdkads.common is older than IAP (missing IapProductKind / IapPurchaseNotification).\n" +
                "1) Hub → Fix com.jis.sdkads.* revisions\n" +
                "2) Flush JIS PackageCache (button below)\n" +
                "3) Package Manager → Resolve",
                MessageType.Error);
        }

        public static void DrawFlushPackageCacheButton()
        {
            if (GUILayout.Button("Flush Library/PackageCache (com.jis.sdkads.*)"))
                FlushJisPackageCache();
        }

        public static void FlushJisPackageCache()
        {
            var library = Path.GetDirectoryName(Application.dataPath);
            var cacheRoot = Path.Combine(library, "Library", "PackageCache");
            if (!Directory.Exists(cacheRoot))
            {
                EditorUtility.DisplayDialog("JIS SDK Hub", "PackageCache folder not found.", "OK");
                return;
            }

            var removed = 0;
            foreach (var dir in Directory.GetDirectories(cacheRoot, "com.jis.sdkads.*"))
            {
                try
                {
                    Directory.Delete(dir, true);
                    removed++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[JIS SDK Hub] Could not delete {dir}: {ex.Message}");
                }
            }

            EditorUtility.DisplayDialog("JIS SDK Hub",
                removed > 0
                    ? $"Removed {removed} cached package(s). Use Package Manager → Resolve, or restart Unity."
                    : "No com.jis.sdkads.* folders in PackageCache.",
                "OK");
        }
    }
}
#endif
