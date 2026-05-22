#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.Hub
{
    /// <summary>
    /// Keeps a single Odin (Sirenix) source: com.jis.sdkads.odin. Disables duplicate plugin importers on import/resolve.
    /// </summary>
    [InitializeOnLoad]
    internal static class JisSDKHubOdinConflictResolver
    {
        const string PreferredPackageId = "com.jis.sdkads.odin";
        const string AttributesDll = "Sirenix.OdinInspector.Attributes.dll";

        static JisSDKHubOdinConflictResolver()
        {
            EditorApplication.delayCall += TryResolveDuplicates;
        }

        public static void TryResolveDuplicates()
        {
            var paths = JisSDKHubPackageHealth.FindOdinAttributesDllPaths();
            if (paths.Count <= 1)
                return;

            var preferred = paths.FirstOrDefault(IsPreferredPath);
            if (string.IsNullOrEmpty(preferred))
                preferred = paths[0];

            var disabled = 0;
            foreach (var dllPath in paths)
            {
                if (string.Equals(Path.GetFullPath(dllPath), Path.GetFullPath(preferred), StringComparison.OrdinalIgnoreCase))
                    continue;

                var meta = dllPath + ".meta";
                if (!File.Exists(meta))
                    continue;

                var importer = AssetImporter.GetAtPath(ToAssetPath(dllPath)) as PluginImporter;
                if (importer == null)
                    continue;

                if (!importer.GetCompatibleWithAnyPlatform())
                    continue;

                importer.SetCompatibleWithAnyPlatform(false);
                importer.SaveAndReimport();
                disabled++;
            }

            if (disabled > 0)
                Debug.LogWarning(
                    $"[JIS SDK Hub] Disabled {disabled} duplicate Odin DLL import(s). Kept: {preferred}\n" +
                    "See docs/ODIN_CONFLICT.md.");
        }

        static bool IsPreferredPath(string fullPath)
        {
            var normalized = fullPath.Replace('\\', '/');
            return normalized.Contains($"/{PreferredPackageId}@") ||
                   normalized.Contains($"/Packages/{PreferredPackageId}/");
        }

        static string ToAssetPath(string fullPath)
        {
            var dataPath = Application.dataPath.Replace('\\', '/');
            var projectRoot = Path.GetDirectoryName(dataPath)?.Replace('\\', '/');
            var normalized = Path.GetFullPath(fullPath).Replace('\\', '/');

            var cacheMarker = "/Library/PackageCache/";
            var idx = normalized.IndexOf(cacheMarker, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var tail = normalized.Substring(idx + cacheMarker.Length);
                var slash = tail.IndexOf('/');
                if (slash > 0)
                {
                    var folder = tail.Substring(0, slash);
                    var at = folder.IndexOf('@');
                    var packageFolder = at > 0 ? folder.Substring(0, at) : folder;
                    var rest = tail.Substring(slash + 1);
                    return $"Packages/{packageFolder}/{rest}";
                }
            }

            if (normalized.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
                return "Assets" + normalized.Substring(dataPath.Length);

            if (!string.IsNullOrEmpty(projectRoot) &&
                normalized.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                return normalized.Substring(projectRoot.Length + 1);

            return fullPath;
        }
    }
}
#endif
