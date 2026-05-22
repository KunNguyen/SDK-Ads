#if UNITY_EDITOR
using JisSDKAds.Ads;
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.Editor
{
    [CustomEditor(typeof(SDKSetup))]
    public class SDKSetupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var setup = (SDKSetup)target;
            EditorGUILayout.Space(8);

            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("Apply Setup To Scene AdsManager", GUILayout.Height(32)))
            {
                setup.Setup();
                EditorUtility.SetDirty(setup);
            }
            GUI.backgroundColor = prev;
        }
    }
}
#endif
