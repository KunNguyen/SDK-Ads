using System.Collections;
using System.Collections.Generic;
using SDK;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
        if (Selection.activeObject is not GameObject) return;
        GameObject go = Selection.activeObject as GameObject;
        AdsManager selectedAdsManager = go.GetComponent<AdsManager>();
        if (selectedAdsManager != null)
        {
            selectedAdsManager.UpdateAdsMediationConfig();
            EditorUtility.SetDirty(selectedAdsManager);
            EditorSceneManager.MarkSceneDirty(selectedAdsManager.gameObject.scene);
        }
    }
}
