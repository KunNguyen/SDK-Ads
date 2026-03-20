#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

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
    }
}
#endif
