#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using JisSDKAds.Ads;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Common;
using JisSDKAds.Ads.UnitAdManagers;
using JisSDKAds.Ads.Resume;
using JisSDKAds.Firebase;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JisSDKAds.Editor
{
    /// <summary>
    /// Builds JIS SDK scene hierarchy (Firebase / JisAds / Ads_Runtime with children).
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
        const string AdsRuntimeChildName = "Ads_Runtime";
        const string UnitManagersChildName = "UnitAdManagers";
        const string MediationChildName = "Mediation";

        public static GameObject CreateManagerInScene()
        {
            if (JisSDKScenePrefabUtility.TryFocusExistingManager(out var existingRoot))
            {
                if (IsFlatLayout(existingRoot)
                    && EditorUtility.DisplayDialog(
                        "JIS SDK Manager",
                        "Manager uses a flat layout (all components on one object). Reorganize into a structured hierarchy?",
                        "Reorganize",
                        "Keep as-is"))
                {
                    ReorganizeFlatHierarchy(existingRoot);
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
            Debug.Log("[JIS SDK] Created structured JisSDK_Manager hierarchy.");
            return hierarchyRoot;
        }

        public static bool ReorganizeFlatHierarchy(GameObject flatRoot)
        {
            if (flatRoot == null || !IsFlatLayout(flatRoot))
                return false;

            Undo.SetCurrentGroupName("Reorganize JIS SDK Manager");
            var undoGroup = Undo.GetCurrentGroup();

            var adsManager = flatRoot.GetComponent<AdsManager>();
            var firebase = flatRoot.GetComponent<FirebaseManager>();
            var tracker = flatRoot.GetComponent<AdsTracker>();
            var jisAds = flatRoot.GetComponent<JisAds>();

            var banner = flatRoot.GetComponent<BannerAdManager>();
            var interstitial = flatRoot.GetComponent<InterstitialAdManager>();
            var rewarded = flatRoot.GetComponent<RewardAdManager>();
            var mediationOnRoot = flatRoot.GetComponents<AdsMediationController>();

            if (flatRoot.name != RootName)
            {
                Undo.RecordObject(flatRoot, "Rename root");
                flatRoot.name = RootName;
            }

            EnsurePersistentRootComponent(flatRoot);

            var firebaseGo = GetOrCreateChild(flatRoot.transform, FirebaseChildName);
            var jisAdsGo = GetOrCreateChild(flatRoot.transform, JisAdsChildName);
            var adsRuntimeGo = GetOrCreateChild(flatRoot.transform, AdsRuntimeChildName);
            var unitRoot = GetOrCreateChild(adsRuntimeGo.transform, UnitManagersChildName);
            var mediationRoot = GetOrCreateChild(adsRuntimeGo.transform, MediationChildName);

            MoveComponent(firebase, firebaseGo);
            MoveComponent(tracker, firebaseGo);
            MoveComponent(jisAds, jisAdsGo);
            MoveComponent(adsManager, adsRuntimeGo);

            MoveComponent(banner, GetOrCreateChild(unitRoot.transform, "Banner"));
            MoveComponent(interstitial, GetOrCreateChild(unitRoot.transform, "Interstitial"));
            MoveComponent(rewarded, GetOrCreateChild(unitRoot.transform, "Rewarded"));
            var mediationControllers = new List<AdsMediationController>();
            foreach (var mediation in mediationOnRoot)
            {
                if (mediation == null) continue;
                var childName = mediation.GetType().Name;
                var target = GetOrCreateChild(mediationRoot.transform, childName);
                MoveComponent(mediation, target);
                mediationControllers.Add(target.GetComponent<AdsMediationController>());
            }

            if (adsManager != null)
            {
                adsManager = adsRuntimeGo.GetComponent<AdsManager>();
                WireAdsManager(
                    adsManager,
                    unitRoot.GetComponentInChildren<BannerAdManager>(true),
                    unitRoot.GetComponentInChildren<InterstitialAdManager>(true),
                    unitRoot.GetComponentInChildren<RewardAdManager>(true),
                    mediationControllers);

                var firebaseComp = firebaseGo.GetComponent<FirebaseManager>();
                if (firebaseComp != null)
                    ConfigureInitModes(firebaseComp, adsManager);
            }

            jisAds = jisAdsGo.GetComponent<JisAds>();
            if (jisAds != null)
                WireJisAds(jisAds);

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("[JIS SDK] Reorganized flat Manager into structured hierarchy.");
            return true;
        }

        static GameObject BuildHierarchy()
        {
            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create JIS SDK Manager");
            EnsurePersistentRootComponent(root);

            var firebaseGo = CreateChild(root.transform, FirebaseChildName);
            var firebase = firebaseGo.AddComponent<FirebaseManager>();
            firebaseGo.AddComponent<AdsTracker>();

            var jisAdsGo = CreateChild(root.transform, JisAdsChildName);
            var jisAds = jisAdsGo.AddComponent<JisAds>();

            var adsRuntimeGo = CreateChild(root.transform, AdsRuntimeChildName);
            var adsManager = adsRuntimeGo.AddComponent<AdsManager>();

            var unitRoot = CreateChild(adsRuntimeGo.transform, UnitManagersChildName);
            var banner = CreateChild(unitRoot.transform, "Banner").AddComponent<BannerAdManager>();
            var interstitial = CreateChild(unitRoot.transform, "Interstitial").AddComponent<InterstitialAdManager>();
            var rewarded = CreateChild(unitRoot.transform, "Rewarded").AddComponent<RewardAdManager>();
            var mediationRoot = CreateChild(adsRuntimeGo.transform, MediationChildName);
            var mediationControllers = new List<AdsMediationController>();
            TryAddMediationController(mediationRoot, mediationControllers,
                "JisSDKAds.Ads.MaxMediationController, JisSDKAds.Providers.Max", "MaxMediation");
            TryAddMediationController(mediationRoot, mediationControllers,
                "JisSDKAds.Ads.AdmobMediationController, JisSDKAds.Providers.AdMob", "AdmobMediation");

            WireAdsManager(adsManager, banner, interstitial, rewarded, mediationControllers);
            WireJisAds(jisAds);
            ConfigureInitModes(firebase, adsManager);

            return root;
        }

        public static bool IsFlatLayout(GameObject root)
        {
            if (root == null) return false;
            return root.GetComponent<AdsManager>() != null
                   && root.GetComponent<BannerAdManager>() != null;
        }

        static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            return go;
        }

        static GameObject GetOrCreateChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child.gameObject;
            }

            return CreateChild(parent, name);
        }

        static void MoveComponent(Component source, GameObject target)
        {
            if (source == null || target == null) return;
            if (source.gameObject == target) return;

            var type = source.GetType();
            var destination = target.GetComponent(type);
            if (destination == null)
                destination = Undo.AddComponent(target, type);

            EditorUtility.CopySerialized(source, destination);
            Undo.DestroyObjectImmediate(source);
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

        static void TryAddMediationController(
            GameObject mediationRoot,
            List<AdsMediationController> list,
            string assemblyQualifiedName,
            string childName)
        {
            var type = Type.GetType(assemblyQualifiedName);
            if (type == null || !typeof(AdsMediationController).IsAssignableFrom(type))
                return;

            var host = CreateChild(mediationRoot.transform, childName);
            var component = host.AddComponent(type) as AdsMediationController;
            if (component != null)
                list.Add(component);
        }

        static void WireAdsManager(
            AdsManager adsManager,
            BannerAdManager banner,
            InterstitialAdManager interstitial,
            RewardAdManager rewarded,
            List<AdsMediationController> mediationControllers)
        {
            var so = new SerializedObject(adsManager);
            AssignRef(so, "BannerAdManager", banner);
            AssignRef(so, "InterstitialAdManager", interstitial);
            AssignRef(so, "RewardAdManager", rewarded);

            var listProp = FindProperty(so, "AdsMediationControllers");
            if (listProp != null)
            {
                listProp.arraySize = mediationControllers.Count;
                for (var i = 0; i < mediationControllers.Count; i++)
                    listProp.GetArrayElementAtIndex(i).objectReferenceValue = mediationControllers[i];
            }

            var settings = LoadSettingsAsset();
            if (settings != null)
            {
                AssignRef(so, "SdkSettings", settings);
                AssignRef(so, "AndroidSdkSetup", settings.android?.sdkSetup);
                AssignRef(so, "IOSSdkSetup", settings.ios?.sdkSetup);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(adsManager);
        }

        static void WireJisAds(JisAds jisAds)
        {
            var settings = LoadSettingsAsset();
            if (settings == null) return;

            if (jisAds.GetComponent<ResumeAdCoordinator>() == null)
                jisAds.gameObject.AddComponent<ResumeAdCoordinator>();

            var so = new SerializedObject(jisAds);
            var prop = so.FindProperty("settings");
            if (prop != null)
                prop.objectReferenceValue = settings;

            var resumeProp = so.FindProperty("resumeCoordinator");
            if (resumeProp != null)
                resumeProp.objectReferenceValue = jisAds.GetComponent<ResumeAdCoordinator>();

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(jisAds);
        }

        static void ConfigureInitModes(FirebaseManager firebase, AdsManager adsManager)
        {
            var settings = LoadSettingsAsset();
            var adsInitMode = settings?.adsInitializationMode ?? AdsManager.AdsInitializationMode.Manual;

            var firebaseSo = new SerializedObject(firebase);
            var firebaseInit = FindProperty(firebaseSo, "InitializationMode");
            if (firebaseInit != null)
            {
                firebaseInit.enumValueIndex = (int)FirebaseManager.FirebaseInitializationMode.Manual;
                firebaseSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var adsSo = new SerializedObject(adsManager);
            var adsInit = FindProperty(adsSo, "InitializationMode");
            if (adsInit != null)
            {
                adsInit.enumValueIndex = (int)adsInitMode;
                adsSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static SerializedProperty FindProperty(SerializedObject so, string propertyName) =>
            so.FindProperty(propertyName) ?? so.FindProperty($"<{propertyName}>k__BackingField");

        static void AssignRef(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            var prop = FindProperty(so, propertyName);
            if (prop != null && prop.propertyType == SerializedPropertyType.ObjectReference)
                prop.objectReferenceValue = value;
        }

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
            if (!AssetDatabase.IsValidFolder("Assets/JisSDKAds"))
                AssetDatabase.CreateFolder("Assets", "JisSDKAds");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/JisSDKAds", "Prefabs");

            var path = $"{PrefabFolder}/{prefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[JIS SDK] Saved prefab template to {path}");
        }
    }
}
#endif
