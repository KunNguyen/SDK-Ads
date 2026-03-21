#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SDK
{
    /// <summary>
    /// Creates and manages AdsManagerSDKSetupContainer and platform-specific SDKSetup assets.
    /// </summary>
    public static class AdsManagerSDKSetupCreator
    {
        private const string ConfigRootFolder = "Assets/JisSDKConfigs";
        private const string PlatformFolder = "Platform";
        private const string ContainerFileName = "AdsManagerSDKSetupContainer.asset";
        private const string AndroidSetupFileName = "AndroidSDKAdsSetup.asset";
        private const string IosSetupFileName = "IOSSDKAdsSetup.asset";
        private const string AssetPrefabsRoot = "Assets/JisSDKAds/Prefabs";
        private const string PackagePrefabsRoot = "Packages/com.jis.sdkads/Prefabs";

        [MenuItem("SDK Setup/Create or Open SDKSetup Container")]
        public static void CreateOrOpen()
        {
            EnsureFolderStructure();

            var container = LoadOrCreateContainer();
            var platformFolder = $"{ConfigRootFolder}/{PlatformFolder}";

            var android = LoadOrCreateSetup<SDKSetup>($"{platformFolder}/{AndroidSetupFileName}");
            var ios = LoadOrCreateSetup<SDKSetup>($"{platformFolder}/{IosSetupFileName}");

            AssignAndSave(container, android, ios);

            Selection.activeObject = container;
            EditorGUIUtility.PingObject(container);
        }

        [MenuItem("SDK Setup/Add Prefab/Manager")]
        public static void AddManagerPrefabToCurrentScene()
        {
            AddPrefabToCurrentScene("Manager");
        }

        [MenuItem("GameObject/SDK Setup/Add Manager", false, 10)]
        public static void AddManagerPrefabFromHierarchyContext()
        {
            AddManagerPrefabToCurrentScene();
        }

        [MenuItem("SDK Setup/Add Prefab/InAppPurchaser")]
        public static void AddInAppPurchaserPrefabToCurrentScene()
        {
            AddPrefabToCurrentScene("InAppPurchaser");
        }

        [MenuItem("GameObject/SDK Setup/Add InAppPurchaser", false, 11)]
        public static void AddInAppPurchaserPrefabFromHierarchyContext()
        {
            AddInAppPurchaserPrefabToCurrentScene();
        }

        private static void EnsureFolderStructure()
        {
            if (!AssetDatabase.IsValidFolder("Assets/JisSDKConfigs"))
                AssetDatabase.CreateFolder("Assets", "JisSDKConfigs");

            if (!AssetDatabase.IsValidFolder($"{ConfigRootFolder}/{PlatformFolder}"))
                AssetDatabase.CreateFolder(ConfigRootFolder, PlatformFolder);
        }

        private static AdsManagerSDKSetupContainer LoadOrCreateContainer()
        {
            var path = $"{ConfigRootFolder}/{ContainerFileName}";
            var container = AssetDatabase.LoadAssetAtPath<AdsManagerSDKSetupContainer>(path);

            if (container == null)
            {
                container = ScriptableObject.CreateInstance<AdsManagerSDKSetupContainer>();
                AssetDatabase.CreateAsset(container, path);
            }

            return container;
        }

        private static T LoadOrCreateSetup<T>(string path) where T : ScriptableObject
        {
            var setup = AssetDatabase.LoadAssetAtPath<T>(path);

            if (setup == null)
            {
                setup = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(setup, path);
            }

            return setup;
        }

        private static void AssignAndSave(AdsManagerSDKSetupContainer container, SDKSetup android, SDKSetup ios)
        {
            container.android = android;
            container.ios = ios;

            EditorUtility.SetDirty(container);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void AddPrefabToCurrentScene(string prefabName)
        {
            var prefab = ResolvePrefab(prefabName);
            if (prefab == null)
            {
                Debug.LogError($"[SDK Setup] Cannot find prefab '{prefabName}' in Assets or Packages.");
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                Debug.LogError("[SDK Setup] Active scene is not valid.");
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, activeScene) as GameObject;
            if (instance == null)
            {
                Debug.LogError($"[SDK Setup] Failed to instantiate prefab '{prefabName}'.");
                return;
            }

            Undo.RegisterCreatedObjectUndo(instance, $"Add {prefabName} Prefab");
            EditorSceneManager.MarkSceneDirty(activeScene);
            Selection.activeObject = instance;
            EditorGUIUtility.PingObject(instance);
        }

        private static GameObject ResolvePrefab(string prefabName)
        {
            var assetPath = $"{AssetPrefabsRoot}/{prefabName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null) return prefab;

            var packagePath = $"{PackagePrefabsRoot}/{prefabName}.prefab";
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(packagePath);
            if (prefab != null) return prefab;

            var guids = AssetDatabase.FindAssets($"t:Prefab {prefabName}", new[] { "Assets", "Packages" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith($"/{prefabName}.prefab", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!path.Contains("/JisSDKAds/Prefabs/", StringComparison.OrdinalIgnoreCase))
                    continue;

                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) return prefab;
            }

            return null;
        }
    }
}
#endif
