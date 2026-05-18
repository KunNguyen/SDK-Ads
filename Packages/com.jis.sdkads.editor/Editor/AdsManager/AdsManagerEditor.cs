#if UNITY_EDITOR
using JisSDKAds.Ads;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JisSDKAds.Editor
{
    [CustomEditor(typeof(AdsManager))]
    public class AdsManagerEditor : OdinEditor
    {
        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            if (Selection.activeObject is not GameObject go) return;

            var selectedAdsManager = go.GetComponent<AdsManager>();
            if (selectedAdsManager == null) return;

            selectedAdsManager.UpdateAdsMediationConfig();
            EditorUtility.SetDirty(selectedAdsManager);
            EditorSceneManager.MarkSceneDirty(selectedAdsManager.gameObject.scene);
        }
    }
}
#endif
