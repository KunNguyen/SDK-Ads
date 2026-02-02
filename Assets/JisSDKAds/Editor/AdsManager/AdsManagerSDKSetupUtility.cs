#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SDK
{
    public static class AdsManagerSDKSetupCreator
    {
        private const string ConfigFolder = "Assets/JisSDKConfigs/";
        private const string ContainerName = "AdsManagerSDKSetupContainer.asset";
        private const string AndroidSetupName = "AndroidSDKAdsSetup.asset";
        private const string IosSetupName = "IOSSDKAdsSetup.asset";

        [MenuItem("SDK Setup/Create or Open SDKSetup Container")]
        public static void CreateOrOpen()
        {
            EnsureFolderExists();

            // 1) Load/Create container A
            var containerPath = ConfigFolder + ContainerName;
            var container = AssetDatabase.LoadAssetAtPath<AdsManagerSDKSetupContainer>(containerPath);
            if (container == null)
            {
                container = ScriptableObject.CreateInstance<AdsManagerSDKSetupContainer>();
                AssetDatabase.CreateAsset(container, containerPath);
            }

            // 2) Load/Create Android SDKSetup
            var androidPath = ConfigFolder + AndroidSetupName;
            var android = AssetDatabase.LoadAssetAtPath<SDKSetup>(androidPath);
            if (android == null)
            {
                android = ScriptableObject.CreateInstance<SDKSetup>();
                AssetDatabase.CreateAsset(android, androidPath);
            }

            // 3) Load/Create iOS SDKSetup
            var iosPath = ConfigFolder + IosSetupName;
            var ios = AssetDatabase.LoadAssetAtPath<SDKSetup>(iosPath);
            if (ios == null)
            {
                ios = ScriptableObject.CreateInstance<SDKSetup>();
                AssetDatabase.CreateAsset(ios, iosPath);
            }

            // 4) Assign refs into container, save
            container.android = android;
            container.ios = ios;

            EditorUtility.SetDirty(container);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 5) Focus in Project
            Selection.activeObject = container;
            EditorGUIUtility.PingObject(container);
        }

        private static void EnsureFolderExists()
        {
            if (AssetDatabase.IsValidFolder(ConfigFolder)) return;

            // ConfigFolder = "Assets/JisSDKConfigs/"
            if (!AssetDatabase.IsValidFolder("Assets/JisSDKConfigs"))
                AssetDatabase.CreateFolder("Assets", "JisSDKConfigs");
        }
    }
}
#endif
