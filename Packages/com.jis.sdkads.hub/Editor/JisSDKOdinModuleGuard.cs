#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.Hub
{
    /// <summary>
    /// Prevents Odin from auto-activating optional modules (e.g. Unity.Mathematics) when a broken duplicate Sirenix tree exists.
    /// </summary>
    [InitializeOnLoad]
    internal static class JisSDKOdinModuleGuard
    {
        const string MathematicsModuleId = "Unity.Mathematics";
        const string AddressablesModuleId = "Unity.Addressables";

        static JisSDKOdinModuleGuard()
        {
            EditorApplication.delayCall += RunOnceAfterLoad;
        }

        static void RunOnceAfterLoad()
        {
            WarnIfCoreStillBundlesSirenix();
            DisableOptionalOdinModuleIfDataMissing(MathematicsModuleId, FindOdinModuleDataPath(MathematicsModuleId));
            DisableOptionalOdinModuleIfDataMissing(AddressablesModuleId, FindOdinModuleDataPath(AddressablesModuleId));
        }

        static void WarnIfCoreStillBundlesSirenix()
        {
            var hits = new System.Collections.Generic.List<string>();
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
                return;

            var embeddedCore = Path.Combine(JisSDKHubManifest.PackagesRoot, "com.jis.sdkads.core", "Plugins", "Sirenix");
            if (Directory.Exists(embeddedCore))
                hits.Add(embeddedCore);

            var assetsOverlay = Path.Combine(Application.dataPath, "Packages", "com.jis.sdkads.core", "Plugins", "Sirenix");
            if (Directory.Exists(assetsOverlay))
                hits.Add(assetsOverlay);

            var cacheRoot = Path.Combine(projectRoot, "Library", "PackageCache");
            if (Directory.Exists(cacheRoot))
            {
                foreach (var dir in Directory.GetDirectories(cacheRoot, "com.jis.sdkads.core@*"))
                {
                    var cached = Path.Combine(dir, "Plugins", "Sirenix");
                    if (Directory.Exists(cached))
                        hits.Add(cached);
                }
            }

            if (hits.Count == 0)
                return;

            var lines = string.Join("\n", hits.ConvertAll(p => "• " + p));
            Debug.LogError(
                "[JIS SDK] Old Odin layout detected inside com.jis.sdkads.core (≥ 5.0 uses com.jis.sdkads.odin only).\n" +
                "Fix:\n" +
                "1) Delete Assets/Packages/com.jis.sdkads.core (if present)\n" +
                "2) Hub → Flush PackageCache → Resolve (pulls core without Sirenix)\n" +
                "3) Ensure manifest includes com.jis.sdkads.odin (via Hub Firebase/Editor import)\n" +
                "4) Do not keep a local copy of core under Packages/ with Plugins/Sirenix\n\n" +
                lines);
        }

        static void DisableOptionalOdinModuleIfDataMissing(string moduleId, string dataPath)
        {
            if (!string.IsNullOrEmpty(dataPath) && File.Exists(dataPath))
                return;

            if (!TryDisableOdinModule(moduleId, out var configPath))
                return;

            Debug.LogWarning(
                $"[JIS SDK Hub] Disabled Odin module '{moduleId}' (missing {moduleId}.data in com.jis.sdkads.odin). " +
                "Install full Odin from Asset Store to enable, or keep disabled if you only need Addressables at runtime.");
        }

        static bool TryDisableOdinModule(string moduleId, out string configPath)
        {
            configPath = null;
            var assets = AssetDatabase.FindAssets("t:OdinModuleConfig");
            var changed = false;

            foreach (var guid in assets)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Replace('\\', '/').Contains("com.jis.sdkads.odin"))
                    continue;

                var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (config == null)
                    continue;

                configPath = path;
                var so = new SerializedObject(config);
                var configurations = so.FindProperty("configurations");
                if (configurations == null)
                    continue;

                for (var i = 0; i < configurations.arraySize; i++)
                {
                    var entry = configurations.GetArrayElementAtIndex(i);
                    var id = entry.FindPropertyRelative("ID");
                    if (id == null || id.stringValue != moduleId)
                        continue;

                    var activation = entry.FindPropertyRelative("ActivationSettings");
                    if (activation != null && activation.intValue != 2)
                    {
                        activation.intValue = 2;
                        changed = true;
                    }
                }

                var moduleUpdate = so.FindProperty("ModuleUpdateSettings");
                if (moduleUpdate != null && moduleUpdate.intValue != 0)
                {
                    moduleUpdate.intValue = 0;
                    changed = true;
                }

                if (changed)
                    so.ApplyModifiedPropertiesWithoutUndo();
            }

            return changed;
        }

        static string FindOdinModuleDataPath(string moduleId) =>
            FindOdinModuleDataFile($"{moduleId}.data");

        static string FindOdinModuleDataFile(string fileName)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
                return null;

            var direct = Path.Combine(projectRoot, "Packages", "com.jis.sdkads.odin", "Plugins", "Sirenix",
                "Odin Inspector", "Modules", fileName);
            if (File.Exists(direct))
                return direct;

            var cache = Path.Combine(projectRoot, "Library", "PackageCache");
            if (!Directory.Exists(cache))
                return null;

            foreach (var dir in Directory.GetDirectories(cache, "com.jis.sdkads.odin@*"))
            {
                var path = Path.Combine(dir, "Plugins", "Sirenix", "Odin Inspector", "Modules", fileName);
                if (File.Exists(path))
                    return path;
            }

            return null;
        }
    }
}
#endif
