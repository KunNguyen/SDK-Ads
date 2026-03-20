#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SDK
{
    [CustomEditor(typeof(AdsManagerSDKSetupContainer))]
    public class AdsManagerSDKSetupContainerEditor : Editor
    {
        private SerializedProperty androidProp;
        private SerializedProperty iosProp;
        private SerializedProperty adsInitializationModeProp;

        private Editor androidEditor;
        private Editor iosEditor;

        private bool showAndroid = true;
        private bool showIos = true;

        private void OnEnable()
        {
            androidProp = serializedObject.FindProperty("android");
            iosProp = serializedObject.FindProperty("ios");
            adsInitializationModeProp = serializedObject.FindProperty("adsInitializationMode");
        }

        private void OnDisable()
        {
            if (androidEditor != null) DestroyImmediate(androidEditor);
            if (iosEditor != null) DestroyImmediate(iosEditor);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("AdsManager SDKSetup Container", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Setup button at the top - always visible
            var container = target as AdsManagerSDKSetupContainer;
            if (container != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    // Prominent Setup button with custom styling
                    var originalColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f); // Green color
                    
                    if (GUILayout.Button("⚙ Setup", GUILayout.Height(30), GUILayout.ExpandWidth(true)))
                    {
                        container.Setup();
                    }
                    
                    GUI.backgroundColor = originalColor;
                }
                EditorGUILayout.Space(8);
            }

            if (adsInitializationModeProp != null)
            {
                EditorGUILayout.PropertyField(adsInitializationModeProp);
                EditorGUILayout.HelpBox(
                    "AutoOnStart: chỉ cần add Manager Prefab vào scene là tự init. " +
                    "Manual: game code tự gọi init async theo loading flow.",
                    MessageType.Info);
                EditorGUILayout.Space(8);
            }

            EditorGUILayout.PropertyField(androidProp);
            DrawEmbeddedSetup(androidProp, ref showAndroid, ref androidEditor, "Android SDKSetup");

            EditorGUILayout.Space(8);

            EditorGUILayout.PropertyField(iosProp);
            DrawEmbeddedSetup(iosProp, ref showIos, ref iosEditor, "iOS SDKSetup");

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawEmbeddedSetup(
            SerializedProperty setupProp,
            ref bool foldout,
            ref Editor cachedEditor,
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
                        if (GUILayout.Button("Ping", GUILayout.Width(60)))
                            EditorGUIUtility.PingObject(setup);

                        if (GUILayout.Button("Open", GUILayout.Width(60)))
                            Selection.activeObject = setup;
                    }
                }

                if (!foldout) return;

                if (setup == null)
                {
                    EditorGUILayout.HelpBox("SDKSetup has not been assigned yet. Please select asset SDKSetup or create a new one using the setup menu.", MessageType.Info);
                    return;
                }

                EditorGUILayout.Space(4);

                // Vẽ inspector của SDKSetup ngay trong Container
                CreateCachedEditor(setup, null, ref cachedEditor);
                if (cachedEditor != null)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        cachedEditor.OnInspectorGUI();
                    }
                }
            }
        }
    }
}
#endif