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
            var coreSirenix = Path.Combine(
                JisSDKHubManifest.PackagesRoot,
                "com.jis.sdkads.core",
                "Plugins",
                "Sirenix");

            if (!Directory.Exists(coreSirenix))
                return;

            Debug.LogError(
                "[JIS SDK] com.jis.sdkads.core must NOT contain Plugins/Sirenix (use com.jis.sdkads.odin only). " +
                "Delete Packages/com.jis.sdkads.core/Plugins/Sirenix and Assets/Packages/com.jis.sdkads.core if present.");
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
