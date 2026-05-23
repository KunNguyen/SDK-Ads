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

        static JisSDKOdinModuleGuard()
        {
            EditorApplication.delayCall += RunOnceAfterLoad;
        }

        static void RunOnceAfterLoad()
        {
            WarnIfCoreStillBundlesSirenix();
            DisableMathematicsModuleIfMisconfigured();
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

        static void DisableMathematicsModuleIfMisconfigured()
        {
            var dataPath = FindOdinMathematicsDataPath();
            if (!string.IsNullOrEmpty(dataPath) && File.Exists(dataPath))
                return;

            // Broken install: module folder without .data — tell Odin not to auto-activate.
            var type = System.Type.GetType(
                "Sirenix.OdinInspector.Editor.Modules.OdinModuleConfig, Sirenix.OdinInspector.Editor");
            if (type == null)
                return;

            var assets = AssetDatabase.FindAssets("t:OdinModuleConfig");
            foreach (var guid in assets)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Replace('\\', '/').Contains("com.jis.sdkads.odin"))
                    continue;

                var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (config == null)
                    continue;

                // Serialized: configurations[].ActivationSettings — 2 = Disabled in Odin 3.x
                var so = new SerializedObject(config);
                var configurations = so.FindProperty("configurations");
                if (configurations == null)
                    continue;

                var changed = false;
                for (var i = 0; i < configurations.arraySize; i++)
                {
                    var entry = configurations.GetArrayElementAtIndex(i);
                    var id = entry.FindPropertyRelative("ID");
                    if (id == null || id.stringValue != MathematicsModuleId)
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
                {
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.LogWarning(
                        "[JIS SDK Hub] Disabled Odin Unity.Mathematics auto-activation (module data missing or duplicate Sirenix).");
                }
            }
        }

        static string FindOdinMathematicsDataPath()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrEmpty(projectRoot))
                return null;

            var candidates = new[]
            {
                Path.Combine(projectRoot, "Packages", "com.jis.sdkads.odin", "Plugins", "Sirenix",
                    "Odin Inspector", "Modules", "Unity.Mathematics.data"),
                Path.Combine(projectRoot, "Library", "PackageCache")
            };

            var direct = candidates[0];
            if (File.Exists(direct))
                return direct;

            var cache = candidates[1];
            if (!Directory.Exists(cache))
                return null;

            foreach (var dir in Directory.GetDirectories(cache, "com.jis.sdkads.odin@*"))
            {
                var path = Path.Combine(dir, "Plugins", "Sirenix", "Odin Inspector", "Modules",
                    "Unity.Mathematics.data");
                if (File.Exists(path))
                    return path;
            }

            return null;
        }
    }
}
#endif
