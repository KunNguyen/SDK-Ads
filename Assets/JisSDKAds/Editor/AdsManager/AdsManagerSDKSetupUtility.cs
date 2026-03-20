#if UNITY_EDITOR
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
        private const string ManagerPrefabPath = "Assets/JisSDKAds/Prefabs/Manager.prefab";
        private const string InAppPurchaserPrefabPath = "Assets/JisSDKAds/Prefabs/InAppPurchaser.prefab";

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
            AddPrefabToCurrentScene(ManagerPrefabPath, "Manager");
        }

        [MenuItem("GameObject/SDK Setup/Add Manager", false, 10)]
        public static void AddManagerPrefabFromHierarchyContext()
        {
            AddManagerPrefabToCurrentScene();
        }

        [MenuItem("SDK Setup/Add Prefab/InAppPurchaser")]
        public static void AddInAppPurchaserPrefabToCurrentScene()
        {
            AddPrefabToCurrentScene(InAppPurchaserPrefabPath, "InAppPurchaser");
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

        private static void AddPrefabToCurrentScene(string prefabPath, string displayName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[SDK Setup] Cannot find prefab at path: {prefabPath}");
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
                Debug.LogError($"[SDK Setup] Failed to instantiate prefab '{displayName}'.");
                return;
            }

            Undo.RegisterCreatedObjectUndo(instance, $"Add {displayName} Prefab");
            EditorSceneManager.MarkSceneDirty(activeScene);
            Selection.activeObject = instance;
            EditorGUIUtility.PingObject(instance);
        }
    }
}
#endif
