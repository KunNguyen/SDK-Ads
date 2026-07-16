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

        const string LegacyOdinPackageId = "com.jis.sdkads.odin";

        /// <summary>
        /// SDK ≥ 5.1 no longer bundles Odin. Warn when the legacy com.jis.sdkads.odin package
        /// (from SDK ≤ 5.0.x) is still installed and offer to remove it.
        /// </summary>
        public static void DrawOdinMigrationWarning()
        {
            var inManifest = JisSDKHubManifest.HasDependency(LegacyOdinPackageId);
            var embedded = JisSDKHubManifest.HasEmbeddedPackage(LegacyOdinPackageId);
            if (!inManifest && !embedded)
                return;

            EditorGUILayout.HelpBox(
                "Legacy package com.jis.sdkads.odin detected (SDK ≤ 5.0.x bundled Odin Inspector).\n" +
                "SDK ≥ 5.1 no longer uses or ships Odin — remove this package to avoid conflicts with " +
                "your project's own Odin Inspector.\n" +
                "If your own code uses Odin, install Odin Inspector from the Asset Store instead.",
                MessageType.Warning);

            if (GUILayout.Button("Remove legacy com.jis.sdkads.odin"))
            {
                var removed = JisSDKHubManifest.RemoveDependency(LegacyOdinPackageId);
                var flushed = FlushPackageCacheEntry(LegacyOdinPackageId);

                if (embedded)
                {
                    var embeddedPath = Path.Combine(JisSDKHubManifest.PackagesRoot, LegacyOdinPackageId);
                    EditorUtility.DisplayDialog("JIS SDK Hub",
                        $"Embedded copy found at {embeddedPath}.\nDelete that folder manually, then let Unity recompile.",
                        "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("JIS SDK Hub",
                        removed || flushed > 0
                            ? "Removed com.jis.sdkads.odin. Package Manager → Resolve (or restart Unity)."
                            : "Nothing to remove.",
                        "OK");
                }
            }
        }

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

        public static int FlushPackageCacheEntry(string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId)) return 0;

            var cacheRoot = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library", "PackageCache");
            if (!Directory.Exists(cacheRoot)) return 0;

            var removed = 0;
            foreach (var dir in Directory.GetDirectories(cacheRoot, packageId + "@*"))
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

            return removed;
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
