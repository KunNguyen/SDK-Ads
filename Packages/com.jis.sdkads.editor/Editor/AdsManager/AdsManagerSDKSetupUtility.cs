#if UNITY_EDITOR
using System;
using JisSDKAds.Ads;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JisSDKAds.Editor
{
    public static class AdsManagerSDKSetupCreator
    {
        const string ConfigRootFolder = "Assets/JisSDKConfigs";
        const string PlatformFolder = "Platform";
        const string ContainerFileName = "AdsManagerSDKSetupContainer.asset";
        const string AndroidSetupFileName = "AndroidSDKAdsSetup.asset";
        const string IosSetupFileName = "IOSSDKAdsSetup.asset";

        [MenuItem(JisSDKMenuPaths.AdsLegacyCreateContainer, false, 400)]
        public static void CreateOrOpenLegacyContainer()
        {
            EnsureFolderStructure();

            var container = LoadOrCreateContainer();
            var platformFolder = $"{ConfigRootFolder}/{PlatformFolder}";

            var android = LoadOrCreateSetup<SDKSetup>($"{platformFolder}/{AndroidSetupFileName}");
            var ios = LoadOrCreateSetup<SDKSetup>($"{platformFolder}/{IosSetupFileName}");

            AssignAndSave(container, android, ios);

            Selection.activeObject = container;
            EditorGUIUtility.PingObject(container);
            Debug.LogWarning(
                "[JIS SDK] Legacy Setup Container created. Prefer JIS SDK → Ads → Create Settings Asset.");
        }

        [MenuItem(JisSDKMenuPaths.AdsSceneAddManager, false, 200)]
        public static void AddManagerPrefabToCurrentScene()
        {
            JisSDKScenePrefabUtility.AddPrefabToActiveScene("Manager");
        }

        [MenuItem(JisSDKMenuPaths.AdsSceneReorganizeManager, false, 201)]
        public static void ReorganizeManagerInCurrentScene()
        {
            var adsManager = UnityEngine.Object.FindFirstObjectByType<AdsManager>();
            if (adsManager == null)
            {
                EditorUtility.DisplayDialog("JIS SDK", "No AdsManager in scene.", "OK");
                return;
            }

            var root = adsManager.transform.root.gameObject;
            if (!JisSDKSceneSetupBuilder.IsFlatLayout(root))
            {
                EditorUtility.DisplayDialog(
                    "JIS SDK",
                    "Manager is already using a structured hierarchy (or layout not recognized).",
                    "OK");
                return;
            }

            JisSDKSceneSetupBuilder.ReorganizeFlatHierarchy(root);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        [MenuItem(JisSDKMenuPaths.GameObjectAddManager, false, 10)]
        public static void AddManagerPrefabFromHierarchyContext()
        {
            AddManagerPrefabToCurrentScene();
        }

        static void EnsureFolderStructure()
        {
            if (!AssetDatabase.IsValidFolder("Assets/JisSDKConfigs"))
                AssetDatabase.CreateFolder("Assets", "JisSDKConfigs");

            if (!AssetDatabase.IsValidFolder($"{ConfigRootFolder}/{PlatformFolder}"))
                AssetDatabase.CreateFolder(ConfigRootFolder, PlatformFolder);
        }

        static AdsManagerSDKSetupContainer LoadOrCreateContainer()
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

        static T LoadOrCreateSetup<T>(string path) where T : ScriptableObject
        {
            var setup = AssetDatabase.LoadAssetAtPath<T>(path);
            if (setup == null)
            {
                setup = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(setup, path);
            }

            return setup;
        }

        static void AssignAndSave(AdsManagerSDKSetupContainer container, SDKSetup android, SDKSetup ios)
        {
            container.android = android;
            container.ios = ios;
            EditorUtility.SetDirty(container);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
