#if UNITY_EDITOR
using JisSDKAds.Ads;
using JisSDKAds.Ads.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JisSDKAds.Editor
{
    [CustomEditor(typeof(AdsManagerSDKSetupContainer))]
    public class AdsManagerSDKSetupContainerEditor : UnityEditor.Editor
    {
        SerializedProperty unifiedSettingsProp;
        SerializedProperty androidProp;
        SerializedProperty iosProp;
        SerializedProperty adsInitializationModeProp;

        UnityEditor.Editor androidEditor;
        UnityEditor.Editor iosEditor;

        bool showAndroid = true;
        bool showIos = true;

        void OnEnable()
        {
            unifiedSettingsProp = serializedObject.FindProperty("unifiedSettings");
            androidProp = serializedObject.FindProperty("android");
            iosProp = serializedObject.FindProperty("ios");
            adsInitializationModeProp = serializedObject.FindProperty("adsInitializationMode");
        }

        void OnDisable()
        {
            if (androidEditor != null) DestroyImmediate(androidEditor);
            if (iosEditor != null) DestroyImmediate(iosEditor);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var container = (AdsManagerSDKSetupContainer)target;

            EditorGUILayout.HelpBox(
                "Legacy container. Prefer JIS SDK → Create Ads Settings Asset (JisSDKAdsSettings) as single source of truth.",
                MessageType.Info);

            EditorGUILayout.PropertyField(unifiedSettingsProp);

            using (new EditorGUILayout.HorizontalScope())
            {
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
                if (GUILayout.Button("Apply to Scene", GUILayout.Height(28)))
                {
                    if (container.unifiedSettings != null)
                        JisSDKAdsSettingsApplier.Apply(container.unifiedSettings, "Container Apply");
                    else
                        container.Setup();
                }
                GUI.backgroundColor = prev;

                using (new EditorGUI.DisabledScope(container.unifiedSettings == null))
                {
                    if (GUILayout.Button("Sync legacy fields", GUILayout.Width(130), GUILayout.Height(28)))
                        container.SyncLegacyFieldsFromSettings();
                }
            }

            EditorGUILayout.Space(6);

            if (container.unifiedSettings != null)
            {
                EditorGUILayout.HelpBox(
                    "Unified settings assigned — android/ios below are ignored on Apply. Edit profiles on JisSDKAdsSettings.",
                    MessageType.None);
                if (GUILayout.Button("Open JisSDKAdsSettings"))
                {
                    Selection.activeObject = container.unifiedSettings;
                    EditorGUIUtility.PingObject(container.unifiedSettings);
                }
                serializedObject.ApplyModifiedProperties();
                return;
            }

            if (adsInitializationModeProp != null)
                EditorGUILayout.PropertyField(adsInitializationModeProp);

            EditorGUILayout.PropertyField(androidProp);
            DrawEmbeddedSetup(androidProp, ref showAndroid, ref androidEditor, "Android SDKSetup");

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(iosProp);
            DrawEmbeddedSetup(iosProp, ref showIos, ref iosEditor, "iOS SDKSetup");

            serializedObject.ApplyModifiedProperties();
        }

        void DrawEmbeddedSetup(
            SerializedProperty setupProp,
            ref bool foldout,
            ref UnityEditor.Editor cachedEditor,
            string title)
        {
            var setup = setupProp.objectReferenceValue as SDKSetup;
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    foldout = EditorGUILayout.Foldout(foldout, title, true);
                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(setup == null))
                    {
                        if (GUILayout.Button("Ping", GUILayout.Width(50)))
                            EditorGUIUtility.PingObject(setup);
                    }
                }

                if (!foldout || setup == null) return;
                CreateCachedEditor(setup, null, ref cachedEditor);
                cachedEditor?.OnInspectorGUI();
            }
        }
    }
}
#endif
