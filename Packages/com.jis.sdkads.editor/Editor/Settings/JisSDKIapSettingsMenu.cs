#if UNITY_EDITOR && UNITY_IAP_ACTIVE
using JisSDKAds.IAP;
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.Editor
{
    public static class JisSDKIapSettingsMenu
    {
        public const string DefaultFolder = "Assets/JisSDKAds/Settings/IAP";
        public const string DefaultPackagesConfigPath = DefaultFolder + "/IAPPackageConfigs.asset";

        [MenuItem(JisSDKMenuPaths.IapCreatePackagesConfig, false, 100)]
        public static void CreatePackagesConfig()
        {
            EnsureFolder();

            var existing = AssetDatabase.LoadAssetAtPath<IAPPackageConfigs>(DefaultPackagesConfigPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            var asset = ScriptableObject.CreateInstance<IAPPackageConfigs>();
            AssetDatabase.CreateAsset(asset, DefaultPackagesConfigPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"[JIS SDK IAP] Created {DefaultPackagesConfigPath}");
        }

        [MenuItem(JisSDKMenuPaths.IapSceneAddPurchaser, false, 110)]
        public static void AddInAppPurchaserToScene()
        {
            JisSDKScenePrefabUtility.AddPrefabToActiveScene(JisSDKSceneSetupBuilder.IapPrefabAssetName);
        }

        [MenuItem(JisSDKMenuPaths.GameObjectAddInAppPurchaser, false, 10)]
        public static void AddInAppPurchaserFromHierarchy()
        {
            AddInAppPurchaserToScene();
        }

        static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/JisSDKAds"))
                AssetDatabase.CreateFolder("Assets", "JisSDKAds");
            if (!AssetDatabase.IsValidFolder("Assets/JisSDKAds/Settings"))
                AssetDatabase.CreateFolder("Assets/JisSDKAds", "Settings");
            if (!AssetDatabase.IsValidFolder(DefaultFolder))
                AssetDatabase.CreateFolder("Assets/JisSDKAds/Settings", "IAP");
        }
    }
}
#endif
