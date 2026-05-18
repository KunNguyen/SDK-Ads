using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using JisSDKAds.Ads;
using JisSDKAds.Common;
#if UNITY_IAP_ACTIVE
using JisSDKAds.IAP;
#endif

namespace JisSDKAds.Editor
{
    [InitializeOnLoad]

    public class JISEditorConfig
    {
        static JISEditorConfig()
        {
            foreach (MonoScript monoScript in MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (monoScript.GetClass() != null)
                {
                    foreach (var a in Attribute.GetCustomAttributes(monoScript.GetClass(), typeof(ScriptOrder)))
                    {
                        var currentOrder = MonoImporter.GetExecutionOrder(monoScript);
                        var newOrder = ((ScriptOrder)a).order;
                        if (currentOrder != newOrder)
                            MonoImporter.SetExecutionOrder(monoScript, newOrder);
                    }
                }
            }
        }
    }

    public static class SDKSetupConfig
    {
        //[MenuItem("SDK Setup/Setup Ads Mediation")]
        public static void OpenMaxAdConfig()
        {
            AddDefineSymbol("UNITY_AD_ADMOB");
            var directory = CreateConfigFolder();
            var assetName = "SDKAdsSetup.asset";
            //Check Android or IOS to set assetName
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                assetName = "AndroidSDKAdsSetup.asset";
            }else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                assetName = "IOSSDKAdsSetup.asset";
            }
            
            var assetPath = $"{directory}{assetName}";
            SDKSetup selectedScriptableObject = AssetDatabase.LoadAssetAtPath<SDKSetup>(assetPath);
            if (selectedScriptableObject == null)
            {
                selectedScriptableObject = ScriptableObject.CreateInstance<SDKSetup>();
                AssetDatabase.CreateAsset(selectedScriptableObject, assetPath);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = selectedScriptableObject;
            EditorGUIUtility.PingObject(selectedScriptableObject);
        }
        
        [MenuItem("SDK Setup/Watch Video Reward Type")]
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
        private static void AddDefineSymbol(string defineSymbol)
        {
            var currentDefineSymbols =
                PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            var defineSymbols = currentDefineSymbols.Split(';');
            var defineSymbolList = new List<string>(defineSymbols);
            currentDefineSymbols = string.Join(";", defineSymbolList.ToArray());
            if (currentDefineSymbols.Contains(defineSymbol)) return;
            currentDefineSymbols += ";" + defineSymbol;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                currentDefineSymbols);
        }
#if UNITY_IAP_ACTIVE
        [MenuItem("SDK Setup/Setup IAP Packages")]
        public static void OpenIAPPackageConfig()
        {
            var directory = CreateConfigFolder();
            var assetName = "IAPPackage.asset";
            var assetPath = $"{directory}{assetName}";
            var selectedScriptableObject = AssetDatabase.LoadAssetAtPath<IAPPackageConfigs>(assetPath);
            if (selectedScriptableObject == null)
            {
                selectedScriptableObject = ScriptableObject.CreateInstance<IAPPackageConfigs>();
                AssetDatabase.CreateAsset(selectedScriptableObject, assetPath);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = selectedScriptableObject;
            EditorGUIUtility.PingObject(selectedScriptableObject);
        }
#endif

        public static string CreateConfigFolder()
        {
            var directory = "Assets/JisSDKConfigs/";
            if (!AssetDatabase.IsValidFolder(directory))
            {
                AssetDatabase.CreateFolder("Assets", "JisSDKConfigs");
            }
            return directory;
        }
    }
}