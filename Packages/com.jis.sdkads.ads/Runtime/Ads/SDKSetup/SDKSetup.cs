using System;
using System.Collections;
using System.Collections.Generic;
using JisSDKAds.Common;
#if UNITY_AD_ADMOB
using GoogleMobileAds.Api;
#endif
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
// using GoogleMobileAds.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif


namespace JisSDKAds.Ads
{
     [CreateAssetMenu(fileName = "SDKAdsSetup", menuName = "JIS SDK/Ads Setup (SDKSetup)", order = 1)]
     public partial class SDKSetup : ScriptableObject
     {
          private const string MAX_MEDIATION_SYMBOL = "UNITY_AD_MAX";
          private const string ADMOB_MEDIATION_SYMBOL = "UNITY_AD_ADMOB";
          private const string FIREBASE_AUTH_SYMBOL = "FIREBASE_AUTH";
          private const string UNITY_IAP_ACTIVE_SYMBOL = "UNITY_IAP_ACTIVE";

          
          
          [HideInInspector] public MaxAdSetup maxAdsSetup;
          [HideInInspector] public AdmobAdSetup admobAdsSetup;
          public AdsMediationType GetAdsMediationType(AdsType adsType)
          {
               return adsType switch
               {
                    AdsType.BANNER => bannerAdsMediationType,
                    AdsType.INTERSTITIAL => interstitialAdsMediationType,
                    AdsType.REWARDED => rewardedAdsMediationType,
                    AdsType.APP_OPEN => appOpenAdsMediationType,
                    _ => AdsMediationType.NONE
               };
          }

          public bool IsActiveAdsType(AdsType adsType)
          {
               return GetAdsMediationType(adsType) != AdsMediationType.NONE;
          }

          public void EnsureMediationSetups()
          {
               maxAdsSetup ??= new MaxAdSetup();
               maxAdsSetup.EnsureInitialized();
               admobAdsSetup ??= new AdmobAdSetup();
               admobAdsSetup.EnsureInitialized();
          }

#if UNITY_EDITOR
          public void Setup()
          {
               AdsManager adsManager = FindFirstObjectByType<AdsManager>();
               if (adsManager != null)
               {
                    adsManager.UpdateAdsMediationConfig(this);
                    EditorUtility.SetDirty(adsManager);
                    EditorSceneManager.MarkSceneDirty(adsManager.gameObject.scene);
               }
               else
               {
                    Debug.LogError("Please add Manager Prefab to scene (Assets/ABIMaxSDKAds/Prefabs/Manager.prefab)");
               }

               ApplyScriptingDefines(EditorUserBuildSettings.selectedBuildTargetGroup);
               if (adsManager != null)
               {
#if UNITY_AD_MAX
            if (adsMediationType == AdsMediationType.MAX)
            {
                string assetPath = "Assets/MaxSdk/Resources/AppLovinSettings.asset";
                UnityEngine.Object applovinSettings = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (applovinSettings == null)
                {
                    Debug.LogWarning($"[JIS SDK] AppLovin settings asset not found at {assetPath}.");
                    return;
                }

                SerializedObject serializedSettings = new SerializedObject(applovinSettings);
                SerializedProperty sdkKeyProperty = serializedSettings.FindProperty("sdkKey");
                if (sdkKeyProperty == null)
                {
                    Debug.LogWarning("[JIS SDK] AppLovin settings asset does not contain a sdkKey field.");
                    return;
                }

                sdkKeyProperty.stringValue = sdkKey_MAX;
                serializedSettings.ApplyModifiedProperties();
                EditorUtility.SetDirty(applovinSettings);
                AssetDatabase.SaveAssets();
            }
#endif
               }
          }

          /// <summary>Legacy name — applies defines for the currently selected build target group only.</summary>
          public void SetupSymbol() =>
               ApplyScriptingDefines(EditorUserBuildSettings.selectedBuildTargetGroup);

          /// <summary>
          /// Sync scripting defines for one platform (Android or iOS group).
          /// Call once per <see cref="SDKSetup"/> asset — do not call Android+iOS setups back-to-back on the selected group.
          /// </summary>
          public void ApplyScriptingDefines(BuildTargetGroup buildTargetGroup) =>
               ApplyScriptingDefines(buildTargetGroup, null);

          public void ApplyScriptingDefines(
               BuildTargetGroup buildTargetGroup,
               IEnumerable<AdsMediationType> additionalMediations)
          {
               var mediationSymbols = CollectMediationDefineSymbols(additionalMediations);
               SymbolHelper.SyncMediationDefines(buildTargetGroup, mediationSymbols);
               SymbolHelper.SetOptionalDefine(buildTargetGroup, "UNITY_APPSFLYER", IsActiveAppsflyer);
               SymbolHelper.SetOptionalDefine(buildTargetGroup, FIREBASE_AUTH_SYMBOL, IsActiveFirebaseAuth);
               SymbolHelper.SetOptionalDefine(buildTargetGroup, UNITY_IAP_ACTIVE_SYMBOL, IsActiveIAP);
          }

          /// <summary>Defines that should be present for this setup (validation).</summary>
          public List<string> GetExpectedScriptingDefineSymbols() =>
               GetExpectedScriptingDefineSymbols(null);

          public List<string> GetExpectedScriptingDefineSymbols(IEnumerable<AdsMediationType> additionalMediations)
          {
               var list = new List<string>(CollectMediationDefineSymbols(additionalMediations));
               if (IsActiveAppsflyer) list.Add("UNITY_APPSFLYER");
               if (IsActiveFirebaseAuth) list.Add(FIREBASE_AUTH_SYMBOL);
               if (IsActiveIAP) list.Add(UNITY_IAP_ACTIVE_SYMBOL);
               return list;
          }

          List<string> CollectMediationDefineSymbols(IEnumerable<AdsMediationType> additionalMediations = null)
          {
               var defineSymbols = new List<string>();
               switch (adsMediationType)
               {
                    case AdsMediationType.MAX:
                         defineSymbols.Add(MAX_MEDIATION_SYMBOL);
                         break;
                    case AdsMediationType.ADMOB:
                         defineSymbols.Add(ADMOB_MEDIATION_SYMBOL);
                         break;
               }

               AddMediationSymbolIfNeeded(defineSymbols, bannerAdsMediationType);
               AddMediationSymbolIfNeeded(defineSymbols, interstitialAdsMediationType);
               AddMediationSymbolIfNeeded(defineSymbols, rewardedAdsMediationType);
               AddMediationSymbolIfNeeded(defineSymbols, appOpenAdsMediationType);

               if (additionalMediations != null)
               {
                    foreach (var mediation in additionalMediations)
                         AddMediationSymbolIfNeeded(defineSymbols, mediation);
               }

               return defineSymbols;
          }

          static void AddMediationSymbolIfNeeded(List<string> defineSymbols, AdsMediationType mediation)
          {
               if (mediation == AdsMediationType.NONE)
                    return;
               var symbol = mediation switch
               {
                    AdsMediationType.MAX => MAX_MEDIATION_SYMBOL,
                    AdsMediationType.ADMOB => ADMOB_MEDIATION_SYMBOL,
                    _ => null
               };
               if (!string.IsNullOrEmpty(symbol) && !defineSymbols.Contains(symbol))
                    defineSymbols.Add(symbol);
          }

#endif
     }
}
