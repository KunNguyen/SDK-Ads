// Assets/Editor/SolarEngineSettingsExporter.cs
using UnityEditor;
using UnityEngine;
using System.IO;

namespace SolarEngine
{
    public class SolarEngineSettingsExporter
    {
        private const string RUNTIME_SETTINGS_PATH = "Assets/Resources/SERuntimeSettings.asset";
        
       // [MenuItem("SolarEngineSDK/Export Runtime Settings")]
        public static void ExportRuntimeSettings()
        {
            // 创建或加载运行时配置实例
            SolarRuntimeSettings runtimeSettings;
            
            if (!File.Exists(RUNTIME_SETTINGS_PATH))
            {
                runtimeSettings = ScriptableObject.CreateInstance<SolarRuntimeSettings>();
                
                // 创建Resources目录（如果不存在）
                string resourcesDir = Path.GetDirectoryName(RUNTIME_SETTINGS_PATH);
                if (!Directory.Exists(resourcesDir))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                
                AssetDatabase.CreateAsset(runtimeSettings, RUNTIME_SETTINGS_PATH);
            }
            else
            {
                runtimeSettings = AssetDatabase.LoadAssetAtPath<SolarRuntimeSettings>(RUNTIME_SETTINGS_PATH);
            }
            
            // // 从编辑器配置复制值
            runtimeSettings.isCN = SolarEngineSettings.isCN;
            runtimeSettings.isOversea = SolarEngineSettings.isOversea;
            //
            // runtimeSettings.isUseOaid = SolarEngineSettings.isUseOaid;
            runtimeSettings.isUseODMInfo = SolarEngineSettings.isUseODMInfo;
            // runtimeSettings.isUseAndroidRc = SolarEngineSettings.isUseAndroid;
            // runtimeSettings.isUseiOSRc = SolarEngineSettings.isUseiOS;
            // runtimeSettings.isUseMininRc = SolarEngineSettings.isUseMiniGame;
            // runtimeSettings.isUseMacOSRc = SolarEngineSettings.isUseMacOS;
            // runtimeSettings.isUseOpenHarmonyRc = SolarEngineSettings.isUseOpenHarmony;
            
            // runtimeSettings.customDomainEnable = SolarEngineSettings.CustomDomainEnable;
            // runtimeSettings.receiverDomain = SolarEngineSettings.ReceiverDomain;
            // runtimeSettings.ruleDomain = SolarEngineSettings.RuleDomain;
            // runtimeSettings.receiverTcpHost = SolarEngineSettings.ReceiverTcpHost;
            // runtimeSettings.ruleTcpHost = SolarEngineSettings.RuleTcpHost;
            // runtimeSettings.gatewayTcpHost = SolarEngineSettings.GatewayTcpHost;
           
            
          
            // 保存更改
            EditorUtility.SetDirty(runtimeSettings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("The runtime configuration has been exported to: " + RUNTIME_SETTINGS_PATH);
        }
    }
}