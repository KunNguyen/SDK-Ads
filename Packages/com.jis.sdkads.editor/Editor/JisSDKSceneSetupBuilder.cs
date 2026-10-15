#if UNITY_EDITOR
using JisSDKAds.Ads;
using JisSDKAds.Ads.Resume;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Common;
using JisSDKAds.Firebase;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JisSDKAds.Editor
{
    /// <summary>
    /// Builds the JIS SDK scene hierarchy: JisSDK_Manager → Firebase (FirebaseManager, AdsTracker)
    /// and JisAds (JisAds, ResumeAdCoordinator). Also strips the pre-6.0 legacy Ads_Runtime subtree.
    /// </summary>
    static class JisSDKSceneSetupBuilder
    {
        const string PrefabFolder = "Assets/JisSDKAds/Prefabs";
        public const string RootName = "JisSDK_Manager";
        public const string PrefabAssetName = "JisSDK_Manager";

        public const string IapRootName = "JisSDK_InAppPurchaser";
        public const string IapPrefabAssetName = "JisSDK_InAppPurchaser";
        const string FirebaseChildName = "Firebase";
        const string JisAdsChildName = "JisAds";
        public const string LegacyAdsRuntimeChildName = "Ads_Runtime";

        public static GameObject CreateManagerInScene()
        {
            if (JisSDKScenePrefabUtility.TryFocusExistingManager(out var existingRoot))
            {
                if (FindLegacyAdsRuntime(existingRoot) != null
                    && EditorUtility.DisplayDialog(
                        "JIS SDK Manager",
                        "This manager still carries the legacy Ads_Runtime subtree (AdsManager, mediation controllers, unit managers). Remove it?",
                        "Remove",
                        "Keep"))
                {
                    RemoveLegacyAdsRuntime(existingRoot);
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }

                TryApplySceneSettings("Scene Setup Builder (existing)");
                return existingRoot;
            }

            var hierarchyRoot = BuildHierarchy();
            TryApplySceneSettings("Scene Setup Builder");

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = hierarchyRoot;
            EditorGUIUtility.PingObject(hierarchyRoot);

            TrySavePrefabAsset(hierarchyRoot, PrefabAssetName);
            Debug.Log("[JIS SDK] Created JisSDK_Manager hierarchy (Firebase + JisAds).");
            return hierarchyRoot;
        }

        static GameObject BuildHierarchy()
        {
            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create JIS SDK Manager");
            EnsurePersistentRootComponent(root);

            var firebaseGo = CreateChild(root.transform, FirebaseChildName);
            var firebase = firebaseGo.AddComponent<FirebaseManager>();
            firebaseGo.AddComponent<AdsTracker>();
            ConfigureFirebaseManualInit(firebase);

            var jisAdsGo = CreateChild(root.transform, JisAdsChildName);
            var jisAds = jisAdsGo.AddComponent<JisAds>();
            WireJisAds(jisAds);

            return root;
        }

        /// <summary>Removes the legacy Ads_Runtime subtree from a prefab asset, an open prefab stage, or a scene object.</summary>
        [MenuItem(JisSDKMenuPaths.AdsSceneRemoveLegacy, false, 202)]
        public static void RemoveLegacyAdsRuntimeMenu()
        {
            // 1) Prefab asset selected in the Project window.
            if (Selection.activeObject is GameObject selectedAsset
                && PrefabUtility.GetPrefabAssetType(selectedAsset) != PrefabAssetType.NotAPrefab
                && !selectedAsset.scene.IsValid())
            {
                var path = AssetDatabase.GetAssetPath(selectedAsset);
                if (!RemoveLegacyAdsRuntimeFromAsset(path))
                    Debug.Log($"[JIS SDK] No legacy Ads_Runtime found in {path}.");
                return;
            }

            // 2) Open prefab stage, or 3) the scene.
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            GameObject root;
            if (stage != null)
                root = stage.prefabContentsRoot;
            else if (Selection.activeGameObject != null)
                root = Selection.activeGameObject.transform.root.gameObject;
            else
            {
                var jisAds = Object.FindFirstObjectByType<JisAds>(FindObjectsInactive.Include);
                root = jisAds != null ? jisAds.transform.root.gameObject : null;
            }

            if (root == null)
            {
                EditorUtility.DisplayDialog("JIS SDK", "Select the JisSDK_Manager object or prefab first.", "OK");
                return;
            }

            if (!RemoveLegacyAdsRuntime(root))
            {
                Debug.Log($"[JIS SDK] No legacy Ads_Runtime found under {root.name}.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(stage != null ? stage.scene : root.scene);
            Debug.Log($"[JIS SDK] Removed legacy Ads_Runtime under {root.name}.");
        }

        /// <summary>
        /// Removes Ads_Runtime under <paramref name="root"/>. A prefab instance in a scene is fixed through its
        /// source asset (the instance follows); a prefab-stage object or a plain scene object is edited in place.
        /// </summary>
        public static bool RemoveLegacyAdsRuntime(GameObject root)
        {
            var legacy = FindLegacyAdsRuntime(root);
            if (legacy == null)
                return false;

            var inPrefabStage = PrefabStageUtility.GetPrefabStage(legacy) != null;
            if (!inPrefabStage && PrefabUtility.IsPartOfPrefabInstance(legacy) && !PrefabUtility.IsPartOfPrefabAsset(legacy))
            {
                var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(legacy);
                if (!string.IsNullOrEmpty(assetPath))
                    return RemoveLegacyAdsRuntimeFromAsset(assetPath);
            }

            Undo.DestroyObjectImmediate(legacy);
            return true;
        }

        /// <summary>Edits the prefab asset at <paramref name="assetPath"/> directly and saves it.</summary>
        public static bool RemoveLegacyAdsRuntimeFromAsset(string assetPath)
        {
            var contents = PrefabUtility.LoadPrefabContents(assetPath);
            try
            {
                var legacy = FindLegacyAdsRuntime(contents);
                if (legacy == null)
                    return false;

                // Preview-scene objects are not undoable: a plain destroy keeps the Undo stack clean.
                Object.DestroyImmediate(legacy);
                PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
                Debug.Log($"[JIS SDK] Removed legacy Ads_Runtime from prefab {assetPath}.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static GameObject FindLegacyAdsRuntime(GameObject root)
        {
            if (root == null)
                return null;

            foreach (Transform child in root.transform)
            {
                if (child.name == LegacyAdsRuntimeChildName)
                    return child.gameObject;
            }

            return null;
        }

        static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            return go;
        }

#if UNITY_IAP_ACTIVE
        public static GameObject CreateInAppPurchaserInScene()
        {
            if (JisSDKScenePrefabUtility.TryFocusExistingInAppPurchaser(out var existingGo))
                return existingGo;

            var root = new GameObject(IapRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create JisSDK InApp Purchaser");
            var purchaser = root.AddComponent<JisSDKAds.IAP.InAppPurchaser>();

            var config = AssetDatabase.LoadAssetAtPath<JisSDKAds.IAP.IAPPackageConfigs>(
                JisSDKIapSettingsMenu.DefaultPackagesConfigPath);
            if (config != null)
            {
                var so = new SerializedObject(purchaser);
                var prop = so.FindProperty("IapProductConfigs");
                if (prop != null)
                {
                    prop.objectReferenceValue = config;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = root;
            TrySavePrefabAsset(root, IapPrefabAssetName);
            Debug.Log($"[JIS SDK] Created {IapRootName} in scene.");
            return root;
        }
#endif

        static void WireJisAds(JisAds jisAds)
        {
            var settings = LoadSettingsAsset();

            if (jisAds.GetComponent<ResumeAdCoordinator>() == null)
                jisAds.gameObject.AddComponent<ResumeAdCoordinator>();

            var so = new SerializedObject(jisAds);
            var prop = so.FindProperty("settings");
            if (prop != null && settings != null)
                prop.objectReferenceValue = settings;

            var resumeProp = so.FindProperty("resumeCoordinator");
            if (resumeProp != null)
                resumeProp.objectReferenceValue = jisAds.GetComponent<ResumeAdCoordinator>();

            var autoInitProp = so.FindProperty("autoInitializeOnStart");
            if (autoInitProp != null && settings != null)
                autoInitProp.boolValue = settings.adsInitializationMode == AdsInitializationMode.AutoOnStart;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(jisAds);
        }

        static void ConfigureFirebaseManualInit(FirebaseManager firebase)
        {
            var firebaseSo = new SerializedObject(firebase);
            var firebaseInit = FindProperty(firebaseSo, "InitializationMode");
            if (firebaseInit != null)
            {
                firebaseInit.enumValueIndex = (int)FirebaseManager.FirebaseInitializationMode.Manual;
                firebaseSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static SerializedProperty FindProperty(SerializedObject so, string propertyName) =>
            so.FindProperty(propertyName) ?? so.FindProperty($"<{propertyName}>k__BackingField");

        static JisSDKAdsSettings LoadSettingsAsset() => JisSDKAdsSettingsApplier.TryLoadDefaultSettings();

        static void TryApplySceneSettings(string reason)
        {
            var adsSettings = LoadSettingsAsset();
            if (adsSettings != null)
                JisSDKAdsSettingsApplier.Apply(adsSettings, reason);
        }

        public static void EnsurePersistentRootComponent(GameObject root)
        {
            if (root == null) return;
            if (root.GetComponent<JisSDKPersistentRoot>() != null
                || root.GetComponent<Manager>() != null)
                return;

#if UNITY_IAP_ACTIVE
            if (root.GetComponent<JisSDKAds.IAP.InAppPurchaser>() != null)
                return;
#endif

            Undo.AddComponent<JisSDKPersistentRoot>(root);
        }

        static void TrySavePrefabAsset(GameObject root, string prefabName)
        {
            if (root == null)
                return;

            if (!AssetDatabase.IsValidFolder("Assets/JisSDKAds"))
                AssetDatabase.CreateFolder("Assets", "JisSDKAds");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/JisSDKAds", "Prefabs");

            var path = $"{PrefabFolder}/{prefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return;

            // Save a scene-detached clone so the template prefab is not tied to the scene instance.
            var clone = Object.Instantiate(root);
            clone.name = prefabName;
            clone.transform.SetParent(null, false);

            try
            {
                using (new EditorLogNoiseFilter())
                {
                    PrefabUtility.SaveAsPrefabAsset(clone, path);
                    AssetDatabase.SaveAssets();
                }

                Debug.Log($"[JIS SDK] Saved prefab template to {path}");
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }
        }
    }
}
#endif
