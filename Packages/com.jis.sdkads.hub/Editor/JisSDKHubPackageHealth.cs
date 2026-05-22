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

        public static bool OdinAttributesAssemblyLoaded() =>
            Type.GetType("Sirenix.OdinInspector.ShowInInspectorAttribute, Sirenix.OdinInspector.Attributes") != null;

        public static bool HasLegacyAssetsOdinFolder()
        {
            var assetsPlugins = Path.Combine(Application.dataPath, "Plugins", "Sirenix");
            return Directory.Exists(assetsPlugins);
        }

        public static void DrawOdinHealthWarning()
        {
            if (!JisSDKHubManifest.HasDependency("com.jis.sdkads.core"))
                return;

            if (HasLegacyAssetsOdinFolder())
            {
                EditorGUILayout.HelpBox(
                    "Found Assets/Plugins/Sirenix — conflicts with Odin in com.jis.sdkads.core.\n" +
                    "Delete Assets/Plugins/Sirenix (keep package core). See docs/MIGRATION_GUID_CONFLICT.md.",
                    MessageType.Warning);
            }

            if (OdinAttributesAssemblyLoaded())
                return;

            EditorGUILayout.HelpBox(
                "Odin (Sirenix) assemblies are missing — com.tw.* / SDK inspector code will not compile.\n" +
                "1) Hub → Fix com.jis.sdkads.* revisions (core ≥ 4.0.1)\n" +
                "2) Flush PackageCache → Resolve\n" +
                "3) Remove duplicate Assets/Plugins/Sirenix if present\n" +
                "4) Or install Odin Inspector from Asset Store into Assets",
                MessageType.Error);
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
