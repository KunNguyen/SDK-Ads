#if UNITY_IAP_ACTIVE
using UnityEditor;
using UnityEngine;

namespace ABIMaxSDKAds.Scripts.IAPServices.Setup
{
    public static class IAPSetup
    {
#if UNITY_EDITOR
        public static IAPPackageConfigs CreateIAPPackageConfigs()
        {
            var directory = CreateConfigFolder();
            var assetName = "IAPPackageConfigs.asset";
            var assetPath = $"{directory}{assetName}";
            var selectedScriptableObject = AssetDatabase.LoadAssetAtPath<IAPPackageConfigs>(assetPath);
            if (selectedScriptableObject == null)
            {
                selectedScriptableObject = ScriptableObject.CreateInstance<IAPPackageConfigs>();
                AssetDatabase.CreateAsset(selectedScriptableObject, assetPath);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = selectedScriptableObject;
            EditorGUIUtility.PingObject(selectedScriptableObject);
            return selectedScriptableObject;
        }
        private static string CreateConfigFolder()
        {
            var directory = "Assets/JisSDKConfigs/";
            if (!AssetDatabase.IsValidFolder(directory))
            {
                AssetDatabase.CreateFolder("Assets", "JisSDKConfigs");
            }
            return directory;
        }
#endif
    }
}
#endif
