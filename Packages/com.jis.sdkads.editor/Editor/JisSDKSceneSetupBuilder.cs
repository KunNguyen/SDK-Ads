#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using JisSDKAds.Ads;
using JisSDKAds.Ads.Settings;
using JisSDKAds.Ads.UnitAdManagers;
using JisSDKAds.Ads.UnitAdManagers.Interface;
using JisSDKAds.Firebase;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JisSDKAds.Editor
{
    /// <summary>
    /// Builds default JIS SDK scene objects when prefab assets are missing.
    /// </summary>
    static class JisSDKSceneSetupBuilder
    {
        const string PrefabFolder = "Assets/JisSDKAds/Prefabs";

        public static GameObject CreateManagerInScene()
        {
            var existingAds = UnityEngine.Object.FindFirstObjectByType<AdsManager>();
            if (existingAds != null)
            {
                Debug.LogWarning("[JIS SDK] AdsManager already exists in scene — selecting existing object.");
                Selection.activeObject = existingAds.gameObject;
                return existingAds.gameObject;
            }

            var root = new GameObject("JisSDK_Manager");
            Undo.RegisterCreatedObjectUndo(root, "Create JIS SDK Manager");

            var firebase = root.AddComponent<FirebaseManager>();
            root.AddComponent<AdsTracker>();
            var jisAds = root.AddComponent<JisAds>();
            var adsManager = root.AddComponent<AdsManager>();

            var banner = root.AddComponent<BannerAdManager>();
            var interstitial = root.AddComponent<InterstitialAdManager>();
            var rewarded = root.AddComponent<RewardAdManager>();
            var mrec = root.AddComponent<MRecAdManager>();
            var appOpen = root.AddComponent<AppOpenAdManager>();
            var collapsible = root.AddComponent<CollapsibleBannerAdManager>();
            var resume = root.AddComponent<ResumeAdManager>();

            var mediationControllers = new List<AdsMediationController>();
            TryAddMediationController(root, mediationControllers,
                "JisSDKAds.Ads.MaxMediationController, JisSDKAds.Providers.Max");
            TryAddMediationController(root, mediationControllers,
                "JisSDKAds.Ads.AdmobMediationController, JisSDKAds.Providers.AdMob");

            WireAdsManager(adsManager, banner, interstitial, rewarded, mrec, appOpen, collapsible, resume,
                mediationControllers);
            WireJisAds(jisAds);
            ConfigureInitModes(firebase, adsManager);

            var settings = LoadSettingsAsset();
            if (settings != null)
                JisSDKAdsSettingsApplier.Apply(settings, "Scene Setup Builder");

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = root;
            EditorGUIUtility.PingObject(root);

            TrySavePrefabAsset(root, "Manager");
            Debug.Log("[JIS SDK] Created JisSDK_Manager in scene (components wired).");
            return root;
        }

#if UNITY_IAP_ACTIVE
        public static GameObject CreateInAppPurchaserInScene()
        {
            var existing = UnityEngine.Object.FindFirstObjectByType<JisSDKAds.IAP.InAppPurchaser>();
            if (existing != null)
            {
                Debug.LogWarning("[JIS SDK] InAppPurchaser already exists — selecting existing object.");
                Selection.activeObject = existing.gameObject;
                return existing.gameObject;
            }

            var root = new GameObject("InAppPurchaser");
            Undo.RegisterCreatedObjectUndo(root, "Create InApp Purchaser");
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
            TrySavePrefabAsset(root, "InAppPurchaser");
            Debug.Log("[JIS SDK] Created InAppPurchaser in scene.");
            return root;
        }
#endif

        static void TryAddMediationController(
            GameObject root,
            List<AdsMediationController> list,
            string assemblyQualifiedName)
        {
            var type = Type.GetType(assemblyQualifiedName);
            if (type == null || !typeof(AdsMediationController).IsAssignableFrom(type))
                return;

            var component = root.AddComponent(type) as AdsMediationController;
            if (component != null)
                list.Add(component);
        }

        static void WireAdsManager(
            AdsManager adsManager,
            BannerAdManager banner,
            InterstitialAdManager interstitial,
            RewardAdManager rewarded,
            MRecAdManager mrec,
            AppOpenAdManager appOpen,
            CollapsibleBannerAdManager collapsible,
            ResumeAdManager resume,
            List<AdsMediationController> mediationControllers)
        {
            var so = new SerializedObject(adsManager);
            AssignRef(so, "BannerAdManager", banner);
            AssignRef(so, "InterstitialAdManager", interstitial);
            AssignRef(so, "RewardAdManager", rewarded);
            AssignRef(so, "MRecAdManager", mrec);
            AssignRef(so, "AppOpenAdManager", appOpen);
            AssignRef(so, "CollapsibleBannerAdManager", collapsible);
            AssignRef(so, "ResumeAdManager", resume);

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
        }

        static void WireJisAds(JisAds jisAds)
        {
            var settings = LoadSettingsAsset();
            if (settings == null) return;

            var so = new SerializedObject(jisAds);
            var prop = so.FindProperty("settings");
            if (prop != null)
            {
                prop.objectReferenceValue = settings;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void ConfigureInitModes(FirebaseManager firebase, AdsManager adsManager)
        {
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
                adsInit.enumValueIndex = (int)AdsManager.AdsInitializationMode.Manual;
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

        static JisSDKAdsSettings LoadSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<JisSDKAdsSettings>(
                JisSDKAdsSettingsMenu.DefaultSettingsPath);
            if (settings != null) return settings;

            var guids = AssetDatabase.FindAssets("t:JisSDKAdsSettings");
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<JisSDKAdsSettings>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
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
