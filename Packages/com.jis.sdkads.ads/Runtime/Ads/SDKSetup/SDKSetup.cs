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
                    AdsType.COLLAPSIBLE_BANNER => collapsibleBannerAdsMediationType,
                    AdsType.INTERSTITIAL => interstitialAdsMediationType,
                    AdsType.REWARDED => rewardedAdsMediationType,
                    AdsType.MREC => mrecAdsMediationType,
                    AdsType.APP_OPEN => appOpenAdsMediationType,
                    _ => AdsMediationType.NONE
               };
          }

          public bool IsActiveAdsType(AdsType adsType)
          {
               return GetAdsMediationType(adsType) != AdsMediationType.NONE;
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

               SetupSymbol();
               if (adsManager != null)
               {
#if UNITY_AD_MAX
            if (adsMediationType == AdsMediationType.MAX)
            {
                string assetPath = "Assets/MaxSdk/Resources/AppLovinSettings.asset";
                AppLovinSettings applovinSettings = AssetDatabase.LoadAssetAtPath<AppLovinSettings>(assetPath);
                applovinSettings.SdkKey = sdkKey_MAX;
                EditorUtility.SetDirty(applovinSettings);
                AssetDatabase.SaveAssets();
            }
#endif
               }
          }

          public void SetupSymbol()
          {
               List<string> removeSymbols = new List<string>
                    { MAX_MEDIATION_SYMBOL, ADMOB_MEDIATION_SYMBOL };
               List<string> defineSymbols = new List<string>();
               switch (adsMediationType)
               {
                    case AdsMediationType.MAX:
                    {
                         defineSymbols.Add(MAX_MEDIATION_SYMBOL);
                    }
                         break;
                    case AdsMediationType.ADMOB:
                    {
                         defineSymbols.Add(ADMOB_MEDIATION_SYMBOL);
                    }
                         break;
                    case AdsMediationType.NONE:
                    {
                    }
                         break;
               }

               if (bannerAdsMediationType != AdsMediationType.NONE)
               {
                    string symbol = GetSymbolByMediationType(bannerAdsMediationType);
                    if (!defineSymbols.Contains(symbol))
                    {
                         defineSymbols.Add(symbol);
                    }
               }

               if (collapsibleBannerAdsMediationType != AdsMediationType.NONE)
               {
                    string symbol = GetSymbolByMediationType(collapsibleBannerAdsMediationType);
                    if (!defineSymbols.Contains(symbol))
                    {
                         defineSymbols.Add(symbol);
                    }
               }

               if (interstitialAdsMediationType != AdsMediationType.NONE)
               {
                    string symbol = GetSymbolByMediationType(interstitialAdsMediationType);
                    if (!defineSymbols.Contains(symbol))
                    {
                         defineSymbols.Add(symbol);
                    }
               }

               if (rewardedAdsMediationType != AdsMediationType.NONE)
               {
                    string symbol = GetSymbolByMediationType(rewardedAdsMediationType);
                    if (!defineSymbols.Contains(symbol))
                    {
                         defineSymbols.Add(symbol);
                    }
               }

               if (mrecAdsMediationType != AdsMediationType.NONE)
               {
                    string symbol = GetSymbolByMediationType(mrecAdsMediationType);
                    if (!defineSymbols.Contains(symbol))
                    {
                         defineSymbols.Add(symbol);
                    }
               }

               if (appOpenAdsMediationType != AdsMediationType.NONE)
               {
                    string symbol = GetSymbolByMediationType(appOpenAdsMediationType);
                    if (!defineSymbols.Contains(symbol))
                    {
                         defineSymbols.Add(symbol);
                    }
               }

               int num = 0;
               while (num < removeSymbols.Count)
               {
                    if (defineSymbols.Contains(removeSymbols[num]))
                         removeSymbols.RemoveAt(num);
                    else
                         num++;
               }

               foreach (var removeSymbol in removeSymbols)
               {
                    SymbolHelper.RemoveDefineSymbol(removeSymbol);
               }

               SymbolHelper.AddDefineSymbols(defineSymbols);
               string appsflyerDefineSymbol = "UNITY_APPSFLYER";
               if (IsActiveAppsflyer)
               {
                    SymbolHelper.AddDefineSymbol(appsflyerDefineSymbol);
               }
               else
               {
                    SymbolHelper.RemoveDefineSymbol(appsflyerDefineSymbol);
               }

               if (IsActiveFirebaseAuth)
               {
                    SymbolHelper.AddDefineSymbol(FIREBASE_AUTH_SYMBOL);
               }
               else
               {
                    SymbolHelper.RemoveDefineSymbol(FIREBASE_AUTH_SYMBOL);
               }

               if (IsActiveIAP)
               {
                    SymbolHelper.AddDefineSymbol(UNITY_IAP_ACTIVE_SYMBOL);
               }
               else
               {
                    SymbolHelper.RemoveDefineSymbol(UNITY_IAP_ACTIVE_SYMBOL);
               }
          }

          private string GetSymbolByMediationType(AdsMediationType adsMediationType)
          {
               return adsMediationType switch
               {
                    AdsMediationType.MAX => MAX_MEDIATION_SYMBOL,
                    AdsMediationType.ADMOB => ADMOB_MEDIATION_SYMBOL,
                    _ => ""
               };
          }
#endif
     }
}