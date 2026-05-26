#if UNITY_EDITOR
using System;
using JisSDKAds.Ads;
using JisSDKAds.Common;
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

            if (IsManagerPrefabName(prefabName))
            {
                if (TryFocusExistingManager(out _))
                {
                    TryApplyDefaultSettings("Existing manager");
                    return;
                }

                var managerPrefab = ResolveManagerPrefab();
                if (managerPrefab != null)
                {
                    InstantiateManagerPrefab(managerPrefab, activeScene, prefabName);
                    return;
                }

                JisSDKSceneSetupBuilder.CreateManagerInScene();
                return;
            }

#if UNITY_IAP_ACTIVE
            if (IsIapPrefabName(prefabName))
            {
                if (TryFocusExistingInAppPurchaser(out _))
                    return;

                var iapPrefab = ResolveIapPrefab();
                if (iapPrefab != null)
                {
                    InstantiateIapPrefab(iapPrefab, activeScene, prefabName);
                    return;
                }

                JisSDKSceneSetupBuilder.CreateInAppPurchaserInScene();
                return;
            }
#endif

            var prefab = TryLoadPrefab(prefabName);
            if (prefab != null)
            {
                var instance = PrefabUtility.InstantiatePrefab(prefab, activeScene) as GameObject;
                if (instance == null)
                {
                    Debug.LogError($"[JIS SDK] Failed to instantiate prefab '{prefabName}'.");
                    return;
                }

                Undo.RegisterCreatedObjectUndo(instance, $"Add {prefabName} Prefab");
                EditorSceneManager.MarkSceneDirty(activeScene);
                Selection.activeObject = instance;
                EditorGUIUtility.PingObject(instance);
                return;
            }

            Debug.LogError(
                $"[JIS SDK] Cannot find prefab '{prefabName}'. " +
                $"Expected under Assets/JisSDKAds/Prefabs/ or use menu to auto-create.");
        }

        public static bool TryFocusExistingManager(out GameObject root)
        {
            root = null;
            var existingAds = UnityEngine.Object.FindFirstObjectByType<AdsManager>(
                FindObjectsInactive.Include);
            if (existingAds == null)
                return false;

            root = existingAds.transform.root.gameObject;
            Debug.LogWarning("[JIS SDK] JisSDK_Manager already exists — selecting existing root.");
            Selection.activeObject = root;
            EditorGUIUtility.PingObject(root);
            return true;
        }

#if UNITY_IAP_ACTIVE
        public static bool TryFocusExistingInAppPurchaser(out GameObject root)
        {
            root = null;
            var existing = UnityEngine.Object.FindFirstObjectByType<JisSDKAds.IAP.InAppPurchaser>(
                FindObjectsInactive.Include);
            if (existing == null)
                return false;

            root = existing.gameObject;
            if (root.name != JisSDKSceneSetupBuilder.IapRootName)
            {
                Undo.RecordObject(root, "Rename JIS SDK IAP root");
                root.name = JisSDKSceneSetupBuilder.IapRootName;
            }

            Debug.LogWarning("[JIS SDK] JisSDK_InAppPurchaser already exists — selecting existing object.");
            Selection.activeObject = root;
            EditorGUIUtility.PingObject(root);
            return true;
        }
#endif

        static bool IsManagerPrefabName(string prefabName) =>
            prefabName.Equals("Manager", StringComparison.OrdinalIgnoreCase)
            || prefabName.Equals(JisSDKSceneSetupBuilder.PrefabAssetName, StringComparison.OrdinalIgnoreCase)
            || prefabName.Equals(JisSDKSceneSetupBuilder.RootName, StringComparison.OrdinalIgnoreCase);

#if UNITY_IAP_ACTIVE
        static bool IsIapPrefabName(string prefabName) =>
            prefabName.Equals("InAppPurchaser", StringComparison.OrdinalIgnoreCase)
            || prefabName.Equals(JisSDKSceneSetupBuilder.IapPrefabAssetName, StringComparison.OrdinalIgnoreCase)
            || prefabName.Equals(JisSDKSceneSetupBuilder.IapRootName, StringComparison.OrdinalIgnoreCase);
#endif

        static void InstantiateManagerPrefab(GameObject prefab, Scene scene, string prefabName)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
            {
                Debug.LogError($"[JIS SDK] Failed to instantiate prefab '{prefabName}'.");
                return;
            }

            var root = instance.transform.root.gameObject;
            if (root.name != JisSDKSceneSetupBuilder.RootName)
            {
                Undo.RecordObject(root, "Rename JIS SDK Manager root");
                root.name = JisSDKSceneSetupBuilder.RootName;
            }

            JisSDKSceneSetupBuilder.EnsurePersistentRootComponent(root);

            Undo.RegisterCreatedObjectUndo(instance, $"Add {prefabName} Prefab");
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = root;
            EditorGUIUtility.PingObject(root);
            TryApplyDefaultSettings("Prefab instantiate");
        }

        static void TryApplyDefaultSettings(string reason)
        {
            var settings = JisSDKAdsSettingsApplier.TryLoadDefaultSettings();
            if (settings == null)
            {
                Debug.LogWarning(
                    $"[JIS SDK] {reason}: No JisSDKAdsSettings asset found — assign manually or create via JIS SDK → Ads → Create Settings.");
                return;
            }

            JisSDKAdsSettingsApplier.Apply(settings, reason);
        }

#if UNITY_IAP_ACTIVE
        static void InstantiateIapPrefab(GameObject prefab, Scene scene, string prefabName)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
            {
                Debug.LogError($"[JIS SDK] Failed to instantiate prefab '{prefabName}'.");
                return;
            }

            var root = instance.transform.root.gameObject;
            if (root.name != JisSDKSceneSetupBuilder.IapRootName)
            {
                Undo.RecordObject(root, "Rename JIS SDK IAP root");
                root.name = JisSDKSceneSetupBuilder.IapRootName;
            }

            Undo.RegisterCreatedObjectUndo(instance, $"Add {prefabName} Prefab");
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeObject = root;
            EditorGUIUtility.PingObject(root);
        }
#endif

        static GameObject ResolveManagerPrefab()
        {
            var structured = TryLoadPrefab(JisSDKSceneSetupBuilder.PrefabAssetName);
            if (structured != null) return structured;

            return TryLoadPrefab("Manager");
        }

#if UNITY_IAP_ACTIVE
        static GameObject ResolveIapPrefab()
        {
            var structured = TryLoadPrefab(JisSDKSceneSetupBuilder.IapPrefabAssetName);
            if (structured != null) return structured;

            return TryLoadPrefab("InAppPurchaser");
        }
#endif

        static GameObject TryLoadPrefab(string prefabName)
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
