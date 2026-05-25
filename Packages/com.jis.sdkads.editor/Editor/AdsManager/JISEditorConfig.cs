using System;
using System.Collections.Generic;
using JisSDKAds.Ads;
using JisSDKAds.Common;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace JisSDKAds.Editor
{
    /// <summary>
    /// Applies <see cref="ScriptOrder"/> once after compile. Avoids InitializeOnLoad loops from SetExecutionOrder.
    /// </summary>
    [InitializeOnLoad]
    public static class JISEditorConfig
    {
        static bool _scheduled;

        static JISEditorConfig()
        {
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            ScheduleApplyScriptOrders();
        }

        static void OnCompilationFinished(object _) => ScheduleApplyScriptOrders();

        static void ScheduleApplyScriptOrders()
        {
            if (_scheduled || EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            _scheduled = true;
            EditorApplication.delayCall += () =>
            {
                _scheduled = false;
                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                    return;
                ApplyScriptOrders();
            };
        }

        static void ApplyScriptOrders()
        {
            foreach (var monoScript in MonoImporter.GetAllRuntimeMonoScripts())
            {
                var type = monoScript.GetClass();
                if (type == null)
                    continue;

                var orderAttr = (ScriptOrder)Attribute.GetCustomAttribute(type, typeof(ScriptOrder));
                if (orderAttr == null)
                    continue;

                var currentOrder = MonoImporter.GetExecutionOrder(monoScript);
                var newOrder = orderAttr.order;
                if (currentOrder == newOrder)
                    continue;

                MonoImporter.SetExecutionOrder(monoScript, newOrder);
            }
        }
    }

    public static class SDKSetupConfig
    {
        public static void OpenMaxAdConfig()
        {
            AddDefineSymbol("UNITY_AD_ADMOB");
            var directory = CreateConfigFolder();
            var assetName = "SDKAdsSetup.asset";
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
                assetName = "AndroidSDKAdsSetup.asset";
            else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
                assetName = "IOSSDKAdsSetup.asset";

            var assetPath = $"{directory}{assetName}";
            var selectedScriptableObject = AssetDatabase.LoadAssetAtPath<SDKSetup>(assetPath);
            if (selectedScriptableObject == null)
            {
                selectedScriptableObject = ScriptableObject.CreateInstance<SDKSetup>();
                AssetDatabase.CreateAsset(selectedScriptableObject, assetPath);
                AssetDatabase.SaveAssets();
            }

            Selection.activeObject = selectedScriptableObject;
            EditorGUIUtility.PingObject(selectedScriptableObject);
        }

        [MenuItem(JisSDKMenuPaths.AdsCreateRewardPlacements, false, 105)]
        public static void OpenRewardAdsPlacementConfig()
        {
            var directory = CreateConfigFolder();
            var assetName = "RewardAdsPlacementConfig.asset";
            var assetPath = $"{directory}{assetName}";
            var selectedScriptableObject = AssetDatabase.LoadAssetAtPath<RewardAdsPlacementConfig>(assetPath);
            if (selectedScriptableObject == null)
            {
                selectedScriptableObject = ScriptableObject.CreateInstance<RewardAdsPlacementConfig>();
                AssetDatabase.CreateAsset(selectedScriptableObject, assetPath);
                AssetDatabase.SaveAssets();
            }

            Selection.activeObject = selectedScriptableObject;
            EditorGUIUtility.PingObject(selectedScriptableObject);
        }

        static void AddDefineSymbol(string defineSymbol)
        {
            SymbolHelper.AddDefineSymbol(defineSymbol);
        }

        public static string CreateConfigFolder()
        {
            var directory = "Assets/JisSDKConfigs/";
            if (!AssetDatabase.IsValidFolder(directory))
                AssetDatabase.CreateFolder("Assets", "JisSDKConfigs");
            return directory;
        }
    }
}
