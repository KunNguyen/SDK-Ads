using SolarEngineSDK.Editor;
using UnityEditor;
using UnityEngine;

namespace SolarEngine
{
    public class ApplySetting
    {
        public static bool _applySetting(bool isShowFail = false, bool isShowSuccess = true, bool isDebug = false)
        {
            if (!SolarEngineSettings.isCN && !SolarEngineSettings.isOversea)
            {
                if (isShowFail)
                {
                    EditorUtility.DisplayDialog(ConstString.fail, ConstString.nostorageWarning, ConstString.OK);
                    return false;
                }
                else
                {
                    return false;
                }
            }

            setMainLand();
            SolarEngineSettingsExporter.ExportRuntimeSettings();
            string storage = SolarEngineSettings.isCN ? ConstString.chinaMainland : ConstString.nonChinaMainland;
            bool result = true;
            if (isShowFail)
            {
                result = EditorUtility.DisplayDialog(ConstString.pleaseConfirm,
                    string.Format("{0} {1}\n {2}", ConstString.currentData, storage, ConstString.confirmMessage2),
                    ConstString.confirm,
                    ConstString.cancel);
            }


            if (result)
            {
                if (checkSDK() && checkPlugins() &&
                    checkXmlHandle())
                {
                    AssetDatabase.Refresh();
                    if (isShowSuccess)
                        ShowTips(ConstString.success, "");

                    return true;
                }
                else
                {
                    EditorUtility.DisplayDialog(ConstString.fail, ConstString.applyFail, "OK");
                    return false;
                }
                // SolarEngineSettingsEditor.a
            }
            // 如果用户取消操作，在控制台输出相应的日志信息
            else
            {
                Debug.Log(ConstString.confirmMessage);
                return false;
            }
        }

        // [MenuItem("SolarEngine/SetMainLand")]
        public static void setMainLand()
        {
            if (SolarEngineSettings.isCN || SolarEngineSettings.isOversea)
                SolarEngineGlobalInfo.setMainLand(SolarEngineSettings.isCN
                    ? SolarEngineGlobalInfo.MainLand.China
                    : SolarEngineGlobalInfo.MainLand.Non_China);
            else
            {
                SolarEngineGlobalInfo.setMainLand(SolarEngineGlobalInfo.MainLand.None);
            }
        }

        //  [MenuItem("SolarEngine/CopyMainLand")]
        public static void copyMainLand()
        {
#if UNITY_STANDALONE_WIN
            SolarEngineGlobalInfo.copyMainLand();
#endif
        }

        //  [MenuItem("SolarEngine/DeleteMainLand")]
        public static void deleteMainLand()
        {
#if UNITY_STANDALONE_WIN
            SolarEngineGlobalInfo.deleteMainLand();
#endif
        }


        public static bool checkApplyWithAndroidPackage()
        {
            bool result = false;
            if (SolarEngineSettings.removeAndroidSDK)
                return true;
            if (!XmlModifier.isAndroidPackage())
            {
                if (!SolarEngineSettings.isCN && !SolarEngineSettings.isOversea)
                {
                    EditorUtility.DisplayDialog(ConstString.fail, ConstString.nostorageWarning, ConstString.OK);
                    return false;
                }


                result = checkPlugins() && checkXmlHandle();
                //  EditorApplication.ExecuteMenuItem("Assets/External Dependency Manager/Android Resolver/Force Resolve ");
            }
            else
            {
                result = true;
            }

            return result;
        }


        private static bool checkSDK()
        {
            if (SolarEngineSettings.removeAndroidSDK)
            {
                return PluginsEdtior.disableAndroidSDK();
            }
            else
            {
                return PluginsEdtior.enableAndroidSDK();
            }
        }

        private static bool checkPlugins()
        {
            bool ios = false;
            bool android = false;
            bool miniGame = false;
            bool oaid = false;

            bool openHarmony = false;

            bool odminfo = false;
            bool installed = PackageChecker.IsUPMPackageInstalled("com.solarengine.sdk");
         
            
            
            //
            // if (!SolarEngineSettings.isUseiOS)
            //     ios = PluginsEdtior.disableiOS();
            // else
            // {
            //     ios = PluginsEdtior.showiOS();
            // }
            //
            // if (!SolarEngineSettings.isUseAndroid)
            //     android = PluginsEdtior.disableAndroid();
            // else
            // {
            //     android = PluginsEdtior.showAndroid();
            // }
            //
            // if (!SolarEngineSettings.isUseMiniGame)
            //     miniGame = PluginsEdtior.disableMiniGame();
            // else
            // {
            //     miniGame = PluginsEdtior.showMiniGame();
            // }
            //
            //
            //
            // if (!SolarEngineSettings.isUseOpenHarmony)
            //     openHarmony = PluginsEdtior.disableOpenHarmony();
            // else
            // {
            //     openHarmony = PluginsEdtior.showOpenHarmony();
            // }

            
            
            if (!installed)
            {
                // if (!SolarEngineSettings.isUseiOS)
                //     ios = PluginsEdtior.disableiOS();
                // else
                // {
                    ios = PluginsEdtior.showiOS();
               // }

                // if (!SolarEngineSettings.isUseAndroid)
                //     android = PluginsEdtior.disableAndroid();
                // else
                // {
                    android = PluginsEdtior.showAndroid();
               // }

                PluginsEdtior.removeRC();
                
                if (!SolarEngineSettings.isUseMiniGame)
                    miniGame = PluginsEdtior.disableMiniGame();
                else
                {
                    miniGame = PluginsEdtior.showMiniGame();
                }
                if (!SolarEngineSettings.isUseOpenHarmony)
                    openHarmony = PluginsEdtior.disableOpenHarmony();
                else
                {
                    openHarmony = PluginsEdtior.showOpenHarmony();
                }
                if (SolarEngineSettings.isUseMacOS)
                    PluginsEdtior.showMacOS();
                else
                    PluginsEdtior.disableMacOS();
            }else
            {
                ios = true;
                android = true;
                miniGame = true;

                openHarmony = true;
                Debug.Log("Installed via UPM: not support disable remoteconfig");

            }

            
            
            if(SolarEngineSettings.isCN)
                SolarEngineSettings.isUseOaid=true;
            if (!SolarEngineSettings.isUseOaid)
                oaid = PluginsEdtior.disableOaid();
            else
            {
                oaid = PluginsEdtior.showOaid();
            }

            if (!SolarEngineSettings.isUseODMInfo || SolarEngineSettings.isCN)
                odminfo = PluginsEdtior.disableODMInfo();
            else if (SolarEngineSettings.isOversea && SolarEngineSettings.isUseODMInfo)
            {
                odminfo = PluginsEdtior.showODMInfo();
            }
            else
            {
                odminfo = PluginsEdtior.disableODMInfo();
            }

          
            return ios && android && miniGame && oaid && odminfo && openHarmony;
        }


        private static bool checkXmlHandle()
        {
            if (SolarEngineSettings.isCN)
                return XmlModifier.cnxml(true);
            else if (SolarEngineSettings.isOversea)
                return XmlModifier.Overseaxml(true);
            return false;
        }

        /// <summary>
        /// 展示提示.
        /// </summary>
        /// <param name="title">标题.</param>
        /// <param name="content">具体内容.</param>
        public static void ShowTips(string title, string content)
        {
            // 展示提示信息.
            EditorUtility.DisplayDialog(title, content, "OK");
        }
    }
}