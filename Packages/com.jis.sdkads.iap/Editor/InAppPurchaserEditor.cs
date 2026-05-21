#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.IAP.Editor
{
    [CustomEditor(typeof(InAppPurchaser))]
    public class InAppPurchaserEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!GUILayout.Button("Create IAP Package Configs"))
                return;

            var purchaser = (InAppPurchaser)target;
            purchaser.CreateIAPPackageConfigs();
            EditorUtility.SetDirty(purchaser);
        }
    }
}
#endif
