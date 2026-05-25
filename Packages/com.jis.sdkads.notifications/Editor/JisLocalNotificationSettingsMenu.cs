#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.Notifications.Editor
{
    public static class JisLocalNotificationSettingsMenu
    {
        private const string DefaultResourcesPath = "Assets/Resources/JisLocalNotificationSettings.asset";

        [MenuItem("JIS SDK/Notifications/Create Settings Asset")]
        public static void CreateSettingsAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<JisLocalNotificationSettings>(DefaultResourcesPath);
            if (existing != null)
            {
                Selection.activeObject = existing;
                EditorGUIUtility.PingObject(existing);
                return;
            }

            var dir = Path.GetDirectoryName(DefaultResourcesPath);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var asset = ScriptableObject.CreateInstance<JisLocalNotificationSettings>();
            AssetDatabase.CreateAsset(asset, DefaultResourcesPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log("[JIS SDK] Created JisLocalNotificationSettings at " + DefaultResourcesPath);
        }

        [MenuItem("JIS SDK/Notifications/Open Documentation")]
        public static void OpenDocs()
        {
            var path = Path.GetFullPath("Packages/com.jis.sdkads.notifications/Documentation~/LOCAL_NOTIFICATIONS.md");
            if (!File.Exists(path))
                path = Path.GetFullPath("Packages/com.jis.sdkads.notifications/../../docs/LOCAL_NOTIFICATIONS.md");
            if (File.Exists(path))
                Application.OpenURL("file://" + path.Replace("\\", "/"));
            else
                Debug.LogWarning("[JIS SDK] LOCAL_NOTIFICATIONS.md not found.");
        }
    }
}
#endif
