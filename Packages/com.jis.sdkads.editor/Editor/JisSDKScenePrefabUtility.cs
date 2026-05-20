#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JisSDKAds.Editor
{
    static class JisSDKScenePrefabUtility
    {
        static readonly string[] PrefabRoots =
        {
            "Assets/JisSDKAds/Prefabs",
            "Packages/com.jis.sdkads.editor/Prefabs",
            "Packages/com.jis.sdkads.ads/Prefabs",
            "Assets/JisSDKConfigs/Prefabs"
        };

        public static void AddPrefabToActiveScene(string prefabName)
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                Debug.LogError("[JIS SDK] Active scene is not valid.");
                return;
            }

            var prefab = ResolvePrefab(prefabName);
            if (prefab != null)
            {
                InstantiatePrefab(prefab, activeScene, prefabName);
                return;
            }

            if (prefabName == "Manager")
            {
                JisSDKSceneSetupBuilder.CreateManagerInScene();
                return;
            }

#if UNITY_IAP_ACTIVE
            if (prefabName == "InAppPurchaser")
            {
                JisSDKSceneSetupBuilder.CreateInAppPurchaserInScene();
                return;
            }
#endif

            Debug.LogError(
                $"[JIS SDK] Cannot find prefab '{prefabName}'. " +
                $"Expected under Assets/JisSDKAds/Prefabs/ or use menu to auto-create.");
        }

        static void InstantiatePrefab(GameObject prefab, Scene scene, string prefabName)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
            {
                Debug.LogError($"[JIS SDK] Failed to instantiate prefab '{prefabName}'.");
                return;
            }

            Undo.RegisterCreatedObjectUndo(instance, $"Add {prefabName} Prefab");
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = instance;
            EditorGUIUtility.PingObject(instance);
        }

        static GameObject ResolvePrefab(string prefabName)
        {
            var fileName = $"{prefabName}.prefab";

            foreach (var root in PrefabRoots)
            {
                var path = $"{root}/{fileName}";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) return prefab;
            }

            var guids = AssetDatabase.FindAssets($"{prefabName} t:Prefab", new[] { "Assets", "Packages" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith($"/{fileName}", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (path.Contains("PlaceholderAds", StringComparison.OrdinalIgnoreCase))
                    continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) return prefab;
            }

            return null;
        }
    }
}
#endif
