using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace SDK
{
    [CreateAssetMenu(
        fileName = "AdsManagerSDKSetupContainer",
        menuName = "Tools/Ads/AdsManager SDKSetup Container",
        order = 10)]
    public class AdsManagerSDKSetupContainer : ScriptableObject
    {
        public SDKSetup android;
        public SDKSetup ios;
        public AdsManager.AdsInitializationMode adsInitializationMode = AdsManager.AdsInitializationMode.AutoOnStart;

#if UNITY_EDITOR
        /// <summary>
        /// Gán cả Android và iOS setup vào AdsManager, sau đó apply config theo build target hiện tại.
        /// </summary>
        public void Setup()
        {
            var adsManager = Object.FindFirstObjectByType<AdsManager>();
            if (adsManager == null)
            {
                Debug.LogError("[AdsManager] Please add AdsManager Prefab to scene.");
                return;
            }

            adsManager.ApplyFromContainer(this);
            EditorUtility.SetDirty(adsManager);
            EditorSceneManager.MarkSceneDirty(adsManager.gameObject.scene);

            var setupForTarget = GetSetupForActiveBuildTarget();
            if (setupForTarget != null)
            {
                setupForTarget.SetupSymbol();
            }
        }

        private SDKSetup GetSetupForActiveBuildTarget()
        {
            return EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android ? android : ios;
        }
#endif
    }
}
