#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        /// <summary>
        /// Finds every Sirenix.OdinInspector.Attributes.dll (duplicate copies break all Odin consumers).
        /// </summary>
        public static IReadOnlyList<string> FindOdinAttributesDllPaths()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
                return Array.Empty<string>();

            var roots = new[]
            {
                Path.Combine(projectRoot, "Assets"),
                Path.Combine(projectRoot, "Packages"),
                Path.Combine(projectRoot, "Library", "PackageCache")
            };

            var hits = new List<string>();
            foreach (var root in roots)
            {
                if (!Directory.Exists(root))
                    continue;

                try
                {
                    hits.AddRange(Directory.GetFiles(root, "Sirenix.OdinInspector.Attributes.dll", SearchOption.AllDirectories));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[JIS SDK Hub] Odin scan failed under {root}: {ex.Message}");
                }
            }

            return hits
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static void DrawOdinHealthWarning()
        {
            var odinDlls = FindOdinAttributesDllPaths();
            var hasAssetsOdin = HasLegacyAssetsOdinFolder();
            var hasOdinPkg = JisSDKHubManifest.HasDependency("com.jis.sdkads.odin") ||
                             JisSDKHubManifest.HasDependency("com.jis.sdkads.core");

            if (odinDlls.Count > 1 || (hasAssetsOdin && odinDlls.Count > 0))
            {
                var lines = string.Join("\n", odinDlls.Select(p => "• " + p));
                EditorGUILayout.HelpBox(
                    "Multiple Odin (Sirenix) copies detected — Unity may load none → CS0246 in com.tw.* and SDK.\n" +
                    "Keep exactly ONE source (recommended: com.jis.sdkads.odin ≥ 5.0.0, pulled via core/editor).\n" +
                    "Hub auto-disables duplicate plugin imports when possible.\n" +
                    "Remove other copies (Assets/Plugins/Sirenix or other packages' Plugins/Sirenix).\n" +
                    "See docs/ODIN_CONFLICT.md.\n\n" + lines,
                    MessageType.Error);
            }
            else if (hasAssetsOdin)
            {
                EditorGUILayout.HelpBox(
                    "Found Assets/Plugins/Sirenix — conflicts with com.jis.sdkads.odin.\n" +
                    "Delete Assets/Plugins/Sirenix (keep JIS odin package). See docs/ODIN_CONFLICT.md.",
                    MessageType.Warning);
            }

            if (!hasOdinPkg)
            {
                if (odinDlls.Count == 1 && OdinAttributesAssemblyLoaded())
                    return;

                if (odinDlls.Count == 0)
                    return;

                EditorGUILayout.HelpBox(
                    "Odin DLL found but com.jis.sdkads.odin / core is not in manifest — add via Hub (Firebase or Editor module).",
                    MessageType.Warning);
                return;
            }

            if (OdinAttributesAssemblyLoaded())
                return;

            EditorGUILayout.HelpBox(
                "Odin (Sirenix) assemblies are missing — editor / com.tw.* may not compile.\n" +
                "1) Resolve duplicate Odin copies (see docs/ODIN_CONFLICT.md)\n" +
                "2) Hub → Fix com.jis.sdkads.* revisions (≥ 5.0.0)\n" +
                "3) Flush PackageCache → Resolve\n" +
                "4) Ensure com.jis.sdkads.odin is installed (dependency of core)",
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

            if (removed > 0)
                EditorApplication.delayCall += JisSDKHubOdinConflictResolver.TryResolveDuplicates;
        }

        public static void DrawResolveOdinDuplicatesButton()
        {
            if (GUILayout.Button("Resolve duplicate Odin plugin imports"))
                JisSDKHubOdinConflictResolver.TryResolveDuplicates();
        }
    }
}
#endif
