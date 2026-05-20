#if UNITY_IAP_ACTIVE
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.IAP.Setup
{
    public static class IAPSetup
    {
#if UNITY_EDITOR
        public const string DefaultFolder = "Assets/JisSDKAds/Settings/IAP";
        public const string DefaultPackagesConfigPath = DefaultFolder + "/IAPPackageConfigs.asset";

        public static IAPPackageConfigs CreateIAPPackageConfigs()
        {
            EnsureFolder();
            var selectedScriptableObject = AssetDatabase.LoadAssetAtPath<IAPPackageConfigs>(DefaultPackagesConfigPath);
            if (selectedScriptableObject == null)
            {
                selectedScriptableObject = ScriptableObject.CreateInstance<IAPPackageConfigs>();
                AssetDatabase.CreateAsset(selectedScriptableObject, DefaultPackagesConfigPath);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = selectedScriptableObject;
            EditorGUIUtility.PingObject(selectedScriptableObject);
            return selectedScriptableObject;
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
#endif
    }
}
#endif
