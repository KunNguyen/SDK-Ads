using System;
using System.Text.RegularExpressions;
using SolarEngineSDK.Editor;
using UnityEditor;
using UnityEngine;

namespace SolarEngine
{
    [CustomEditor(typeof(SolarEngineSettings))]
    public class SolarEngineSettingsEditor : Editor
    {
        
        
        
        bool isshowrc=false;
        
        
        
        // 通用标签间的间距
        private const float COMMON_SPACE = 13f;

        // 最大间距
        private const float MAX_SPACE = 5f;

        // 以下类似的多个布尔值用于记录对应属性之前的旧值，便于处理属性变更逻辑

        private static bool oldDisAndroidValue;
        private static bool oldDisMiniGameValue;
        private static bool oldDisOaidValue;

        // 用于记录之前中国存储区域选择的布尔值，方便对比属性值变化
        private bool oldChinaValue;

        // 用于记录之前海外存储区域选择的布尔值，方便对比属性值变化
        private bool oldOverseaValue;

        private SerializedProperty optionalFeatures;


        private SolarEngineSettings solarEngingSetting;


        private void OnEnable()
        {
            // 获取当前正在编辑的SolarEngineSettings类型的目标对象实例

            if (target == null)
            {
                Debug.LogWarning(ConstString.solorEngineLog + " Target is null in OnEnable.");
                return;
            }

            if (serializedObject == null)
            {
                Debug.LogWarning("SerializedObject is null in OnEnable.");
                return;
            }

            solarEngingSetting = target as SolarEngineSettings;

            #region 获取表示中国、海外存储区域选择的序列化属性

            chinaProperty = serializedObject.FindProperty("_China");
            overseaProperty = serializedObject.FindProperty("_Oversea");

            #endregion

            optionalFeatures = serializedObject.FindProperty("_OptionalFeatures");

            #region 获取iOS平台URL相关的几个序列化属性，如标识符、方案、通用链接域名等

            iOSUrlIdentifier = serializedObject.FindProperty("_iOSUrlIdentifier");
            iOSUrlSchemes = serializedObject.FindProperty("_iOSUrlSchemes");
            iOSUniversalLinksDomains = serializedObject.FindProperty("_iOSUniversalLinksDomains");

            #endregion


            #region 获取Android平台URL相关的几个序列化属性，如方案等

            AndroidUrlSchemes = serializedObject.FindProperty("_AndroidUrlSchemes");

            #endregion

            #region 是否移除iOS or Android

            useiOSSDK = serializedObject.FindProperty("_UseiOSSDK");
            removeAndroidSDK = serializedObject.FindProperty("_RemoveAndroidSDK");

            #endregion

            #region 获取是否使用远程配置、OAID、深度链接、指定版本等相关的序列化属性

            useRemoteConfig = serializedObject.FindProperty("_RemoteConfig");
            useOaid = serializedObject.FindProperty("_Oaid");
            useODMInfo = serializedObject.FindProperty("_ODMInfo");
            useDeepLink = serializedObject.FindProperty("_DeepLink");
            useSpecifyVersion = serializedObject.FindProperty("_SpecifyVersion");

            #endregion

            #region 获取不同平台（iOS、Android、小游戏）远程配置相关的序列化属性

            iOSRemoteConfig = serializedObject.FindProperty("_iOS");
            androidRemoteConfig = serializedObject.FindProperty("_Android");
            miniGameRemoteConfig = serializedObject.FindProperty("_MiniGame");
            openHarmonyRemoteConfig = serializedObject.FindProperty("_OpenHarmony");



            #endregion

            #region 版本

            useSpecifyVersion = serializedObject.FindProperty("_SpecifyVersion");
            iOSVersion = serializedObject.FindProperty("_iOSVersion");
            AndroidVersion = serializedObject.FindProperty("_AndroidVersion");
            OpenHarmonyVersion = serializedObject.FindProperty("_OpenHarmonyVersion");
            // MacOSVersion = serializedObject.FindProperty("_MacOSVersion");

            #endregion

            #region 私有化部署

            // CustomDomainEnable = serializedObject.FindProperty("_CustomDomainEnable");
            // ReceiverDomain = serializedObject.FindProperty("_ReceiverDomain");
            // RuleDomain = serializedObject.FindProperty("_RuleDomain");
            // ReceiverTcpHost = serializedObject.FindProperty("_ReceiverTcpHost");
            // RuleTcpHost = serializedObject.FindProperty("_RuleTcpHost");
            // GatewayTcpHost = serializedObject.FindProperty("_GatewayTcpHost");

            #endregion


            // 记录初始时中国存储区域选择的布尔值
            oldChinaValue = chinaProperty.boolValue;
            // 记录初始时海外存储区域选择的布尔值
            oldOverseaValue = overseaProperty.boolValue;



            oldDisOaidValue = useOaid.boolValue;
            
           
            oldDisAndroidValue = androidRemoteConfig.boolValue;
            
            Debug.LogWarning(
                $"{ConstString.solorEngineLog} -> " +
                $"Android:{androidRemoteConfig.boolValue}, " +
                $"iOS:{iOSRemoteConfig.boolValue}, " +
                $"MiniGame:{miniGameRemoteConfig.boolValue}, " +
                $"OpenHarmony:{openHarmonyRemoteConfig.boolValue}");

        }


        public override void OnInspectorGUI()
        {
            _GUI();
        }

        #region DrawRemoveAndroidSDKOption

        private void DrawRemoveAndroidSDKOption()
        {
            // EditorGUILayout.PropertyField(removeAndroidSDK, new GUIContent("Remove Android SDK"));
            // if (removeAndroidSDK.boolValue)
            // {
            //     useOaid.boolValue = false;
            // }
            // else
            // {
            //     useOaid.boolValue = oldDisOaidValue;
            // }
        }

        #endregion


        #region DrawOaidOption

        private void DrawOaidOption()
        {
            if (chinaProperty.boolValue)
            {
                EditorGUILayout.HelpBox(ConstString.storageEnableOaidCN, MessageType.Info);
                useOaid.boolValue = true;
            }

            if (useOaid.boolValue && chinaProperty.boolValue)
                EditorGUILayout.LabelField("OAID", EditorStyles.label);
            else
                EditorGUILayout.PropertyField(useOaid, new GUIContent(ConstString.oaid));


            if (overseaProperty.boolValue)
                if (useOaid.boolValue)
                    EditorGUILayout.HelpBox(ConstString.oaidEnable, MessageType.Warning);

            oldDisOaidValue = useOaid.boolValue;
            // Debug.Log( "oldDisOaidValue"+useOaid.boolValue);
        }

        #endregion

        #region DrawODMInfoOption

        private void DrawODMInfoOption()
        {
            if (overseaProperty.boolValue)
            {
                EditorGUILayout.PropertyField(useODMInfo, new GUIContent(ConstString.ODMInfo));
                EditorGUILayout.HelpBox(ConstString.odmInfoEnable, MessageType.Info);
            }
        }

        #endregion

        #region DrawDeepLinkOption

        private void DrawDeepLinkOption(GUIStyle darkerCyanTextFieldStyles)
        {
            EditorGUILayout.PropertyField(useDeepLink, new GUIContent("DeepLink"));
            if (useDeepLink.boolValue)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.LabelField("iOS:", darkerCyanTextFieldStyles);
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(iOSUrlIdentifier,
                    new GUIContent("iOS URL Identifier",
                        "Value of CFBundleURLName property of the root CFBundleURLTypes element. " +
                        "If not needed otherwise, value should be your bundle ID."),
                    true);
                EditorGUILayout.PropertyField(iOSUrlSchemes,
                    new GUIContent("iOS URL Schemes",
                        "URL schemes handled by your app. " +
                        "Make sure to enter just the scheme name without :// part at the end."),
                    true);
                EditorGUILayout.PropertyField(iOSUniversalLinksDomains,
                    new GUIContent("iOS Universal Links Domains",
                        "Associated domains handled by your app. State just the domain part without applinks: part in front."),
                    true);
                EditorGUI.indentLevel -= 1;


                EditorGUILayout.LabelField("Android:", darkerCyanTextFieldStyles);
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(AndroidUrlSchemes);
                EditorGUI.indentLevel -= 1;
                EditorGUI.indentLevel -= 1;
            }
        }

        #endregion


        private void ProcessPropertyChange(SerializedProperty property, ref bool oldValue, string propertyName,
            Action<bool> xmlAction, Action additionalAction = null)
        {
            if (property.boolValue != oldValue)
            {
                oldValue = property.boolValue;
                additionalAction?.Invoke();
            }
        }

        public void _GUI()
        {
            var darkerCyanTextFieldStyles = new GUIStyle(EditorStyles.boldLabel);
            darkerCyanTextFieldStyles.normal.textColor = Color.white;

            DrawH2Title("Quick integration");

            DrawStorageAreaOptions();

            EditorGUILayout.Space();
            var origFontStyle = EditorStyles.label.fontStyle;
            var _color = EditorStyles.label.normal.textColor;
            EditorStyles.label.fontStyle = FontStyle.Bold;
            EditorStyles.label.normal.textColor = new Color(0f / 255f, 190f / 255f, 190f / 255f);

            optionalFeatures.boolValue = EditorGUILayout.Toggle(
                new GUIContent("OPTIONAL FEATURES"),
                optionalFeatures.boolValue
            );

            EditorStyles.label.fontStyle = origFontStyle;
            EditorStyles.label.normal.textColor = _color;

            if (solarEngingSetting._OptionalFeatures)
            {
                DrawVerticalSpace(5f);
                EditorGUI.indentLevel += 1;
                DrawRemoveAndroidSDKOption();
                if(isshowrc)
                 DrawRemoteConfig();
                DrawOaidOption();
                DrawODMInfoOption();

                DrawDeepLinkOption(darkerCyanTextFieldStyles);

                DrawSdkVersionSection();
                EditorGUI.indentLevel -= 1;
            }

          
            ApplyButton();

            serializedObject.ApplyModifiedProperties();
        }


        private void ApplyButton()
        {
            // 创建一个用于按钮样式的GUIStyle对象
            var buttonStyle = new GUIStyle();
            buttonStyle.normal.textColor = Color.white;

            // 创建一个单像素的纹理对象，用于设置按钮的背景颜色等样式
            var backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, Color.white);
            backgroundTexture.Apply();
            buttonStyle.normal.background = backgroundTexture;

            // 设置按钮的固定高度、固定宽度以及文本对齐方式等样式属性
            buttonStyle.fixedHeight = 25;
            buttonStyle.fixedWidth = 100;
            buttonStyle.alignment = TextAnchor.MiddleCenter;


            // 设置绘制按钮边框时的颜色
            GUI.color = new Color(200f / 255f, 200f / 255f, 200f / 255f);


            // 当用户点击按钮区域时执行以下逻辑
            if (GUILayout.Button("Apply"))
            {
                // 点击 Apply 时验证格式


                if (!IsValidVersion(iOSVersion.stringValue) ||
                    !IsValidVersion(AndroidVersion.stringValue)
#if TUANJIE_2022_3_OR_NEWER
               || !IsValidVersion(OpenHarmonyVersion.stringValue)
#endif
                   )
                {
                    EditorUtility.DisplayDialog("format error",
                        "Please enter the correct SDK version format (e.g. 1.0.0.0)）", "OK");
                    return;
                }

                ApplySetting._applySetting(true);
            }
        }

        private bool IsValidVersion(string version)
        {
            return Regex.IsMatch(version, @"^\d{1,3}(\.\d{1,3}){3}$") ||
                   string.IsNullOrEmpty(version);
        }

        //用户应用
        public bool Apply()
        {
            return iOSRemoteConfigValue() &&
                   androidRemoteConfigValue() &&
                   miniGameRemoteConfigValue() &&
                   openHarmonyRemoteConfigValue() &&
                   OaidValue() &&
                   changleStorageValue();
        }


        private bool OaidValue()
        {
            if (useOaid.boolValue)
                return PluginsEdtior.showOaid();
            return PluginsEdtior.disableOaid();
        }

        private bool ODMInfoValue()
        {
            if (useODMInfo.boolValue)
                return PluginsEdtior.showODMInfo();
            return PluginsEdtior.disableODMInfo();
        }

        private bool iOSRemoteConfigValue()
        {
            if (iOSRemoteConfig.boolValue)
                return PluginsEdtior.showiOS();

            return PluginsEdtior.disableiOS();
        }

        private bool androidRemoteConfigValue()
        {
           // if (androidRemoteConfig.boolValue)
                return PluginsEdtior.showAndroid();

            //return PluginsEdtior.disableAndroid();
        }

        private bool miniGameRemoteConfigValue()
        {
     
            if (miniGameRemoteConfig.boolValue)
                return PluginsEdtior.showMiniGame();

            return PluginsEdtior.disableMiniGame();
        }

        private bool openHarmonyRemoteConfigValue()
        {
            Debug.Log($"{ConstString.solorEngineLog} openHarmonyRemoteConfigValue" + openHarmonyRemoteConfig.boolValue);
            if (openHarmonyRemoteConfig.boolValue)
                return PluginsEdtior.showOpenHarmony();
            return PluginsEdtior.disableOpenHarmony();
        }

        protected void DrawH2Title(string title)
        {
            var _title = title.ToUpperInvariant();
            DrawAreaTitle(_title, EditorUtils.GetColor(EditorUtils.EditorColor.Cyan), TextAnchor.MiddleLeft,
                EditorStyles.label.fontSize);
        }

        /// <summary>
        ///     绘制标题区域.
        /// </summary>
        /// <param name="title">标题.</param>
        /// <param name="color">字体颜色.</param>
        /// <param name="textAnchor">对齐方式.</param>
        /// <param name="fontSize">字体大小.</param>
        private static void DrawAreaTitle(string title, Color color, TextAnchor textAnchor, int fontSize)
        {
            EditorGUILayout.BeginVertical();
            DrawVerticalSpace(MAX_SPACE);

            var guiStyle = new GUIStyle();
            guiStyle.fontSize = fontSize;
            guiStyle.normal.textColor = color;
            guiStyle.fontStyle = FontStyle.Bold;
            guiStyle.alignment = textAnchor;
            EditorGUILayout.TextArea(title, guiStyle);
            EditorGUILayout.EndVertical();
            DrawVerticalSpace(COMMON_SPACE);
        }

        /// <summary>
        ///     绘制垂直方向间距.
        /// </summary>
        /// <param name="pixels">间距.</param>
        private static void DrawVerticalSpace(float pixels)
        {
            GUILayout.Space(pixels);
        }


        /// <summary>
        ///     展示提示.
        /// </summary>
        /// <param name="title">标题.</param>
        /// <param name="content">具体内容.</param>
        public static void ShowTips(string title, string content)
        {
            // 展示提示信息.
            EditorUtility.DisplayDialog(title, content, "OK");
        }

        #region 数据存储区域

        //序列化属性，用于表示是否选择中国存储区域的设置，方便在编辑器中操作和获取对应的值
        private SerializedProperty chinaProperty;

        // 序列化属性，用于表示是否选择海外存储区域的设置，方便在编辑器中操作和获取对应的值
        private SerializedProperty overseaProperty;

        #endregion

        #region 远程配置的设置

        // 序列化属性，用于表示是否使用远程配置的设置
        private SerializedProperty useRemoteConfig;

        // 序列化属性，用于表示 iOS 平台远程配置相关的设置
        private SerializedProperty iOSRemoteConfig;

        // 序列化属性，用于表示 Android 平台远程配置相关的设置
        private SerializedProperty androidRemoteConfig;

        // 序列化属性，用于表示小游戏平台远程配置相关的设置
        private SerializedProperty miniGameRemoteConfig;

        // 序列化属性，用于表示 macOS 平台远程配置相关的设置
        private SerializedProperty macosRemoteConfig;
        // 序列化属性，用于表示鸿蒙平台远程配置相关的设置

        private SerializedProperty openHarmonyRemoteConfig;

        #endregion


        #region OAID、ODMInfo、removeAndroidSDK

        // 序列化属性，用于表示是否使用 OAID 的设置
        private SerializedProperty useOaid;

        // 序列化属性，用于表示是否使用 ODMInfo 的设置
        private SerializedProperty useODMInfo;
        private SerializedProperty useiOSSDK;
        private SerializedProperty removeAndroidSDK;

        #endregion

        #region 深度链接

        // 序列化属性，用于表示是否使用深度链接的设置
        private SerializedProperty useDeepLink;

        // 序列化属性，用于表示 iOS 平台 URL 标识符相关的设置
        private SerializedProperty iOSUrlIdentifier;

        // 序列化属性，用于表示 iOS 平台 URL 方案相关的设置
        private SerializedProperty iOSUrlSchemes;

        // 序列化属性，用于表示 iOS 平台通用链接域名相关的设置
        private SerializedProperty iOSUniversalLinksDomains;

        // 序列化属性，用于表示 Android 平台 URL 方案相关的设置
        private SerializedProperty AndroidUrlSchemes;

        #endregion

        #region SDK版本设置

        // 序列化属性，用于表示是否使用指定版本的设置
        private SerializedProperty useSpecifyVersion;

        // 序列化属性，用于表示 iOS 平台版本相关的设置
        private SerializedProperty iOSVersion;
        private SerializedProperty OpenHarmonyVersion;

        // private SerializedProperty MacOSVersion;

        // 序列化属性，用于表示 Android 平台版本相关的设置
        private SerializedProperty AndroidVersion;

        #endregion

        #region 私有化部署

        private SerializedProperty CustomDomainEnable;
        private SerializedProperty ReceiverDomain;
        private SerializedProperty RuleDomain;
        private SerializedProperty ReceiverTcpHost;
        private SerializedProperty RuleTcpHost;
        private SerializedProperty GatewayTcpHost;

        #endregion

#if UNITY_EDITOR
        [MenuItem("SolarEngineSDK/MiniGame/RemoveAndroid", false, 0)]
        public static void RemoveAndroidSDK()
        {
            if (PackageChecker.IsUPMPackageInstalled())
            {
                ShowTips ("warn", "UPM access is not supported");
                return;
            }
            
#if SOLARENGINE_BYTEDANCE_CLOUD
            SolarEngineSettings.removeAndroidSDK = true;
       
            bool result = PluginsEdtior.disableAndroidSDK() && PluginsEdtior.disableAndroid() &&
                          PluginsEdtior.disableOaid();

#else
            ShowTips("warn", "Only Tiktok Cloud Game takes effect");
#endif
        }

        [MenuItem("SolarEngineSDK/MiniGame/AddAndroid", false, 0)]
        public static void AddAndroidSDK()
        {
            if (PackageChecker.IsUPMPackageInstalled())
            {
                ShowTips ("warn", "UPM access is not supported");
                return;
            }
            SolarEngineSettings.removeAndroidSDK = false;
            SolarEngineSettings.isUseAndroid = oldDisAndroidValue;
            SolarEngineSettings.isUseOaid = oldDisOaidValue;
            PluginsEdtior.enableAndroidSDK();
            Debug.Log($"{ConstString.solorEngineLog} isUseAndroid: {SolarEngineSettings.isUseAndroid}, isUseOaid: {SolarEngineSettings.isUseOaid}");
            if (SolarEngineSettings.isUseOaid) PluginsEdtior.showOaid();

            if (SolarEngineSettings.isUseAndroid)
                PluginsEdtior.showAndroid();
        }
#endif

        #region DrawStorageAreaOptions

        private void DrawStorageAreaOptions()
        {
            EditorGUI.indentLevel += 1;
            EditorGUILayout.PropertyField(chinaProperty, new GUIContent(ConstString.chinaMainland));
            EditorGUILayout.PropertyField(overseaProperty, new GUIContent(ConstString.nonChinaMainland));
            EditorGUI.indentLevel -= 1;

            if (!chinaProperty.boolValue && !overseaProperty.boolValue)
                EditorGUILayout.HelpBox(ConstString.storageAreaMessage, MessageType.Info);

            if (serializedObject.ApplyModifiedProperties())
            {
                // 处理 China 值变化
                ProcessPropertyChange(chinaProperty, ref oldChinaValue, "_China", null, () =>
                {
                    overseaProperty.boolValue = false;
                    oldOverseaValue = overseaProperty.boolValue;
                    if (chinaProperty.boolValue) useOaid.boolValue = true;
                    
                });

                // 处理 Oversea 值变化
                ProcessPropertyChange(overseaProperty, ref oldOverseaValue, "_Oversea", null, () =>
                {
                    chinaProperty.boolValue = false;
                    oldChinaValue = chinaProperty.boolValue;
                    if (overseaProperty.boolValue) useOaid.boolValue = false;
                });
            }
        }

        private bool changleStorageValue()
        {
            if (chinaProperty.boolValue)
                return XmlModifier.cnxml(true);
            if (overseaProperty.boolValue) return XmlModifier.Overseaxml(true);

            return false;
        }

        #endregion


        #region DrawRemoteConfig

        private bool _useRemoteConfig = false;

        private void DrawRemoteConfig()
        {
            _useRemoteConfig = EditorGUILayout.Foldout(_useRemoteConfig, "Remote Config");
            if (_useRemoteConfig)
            {
                EditorGUI.indentLevel += 1;
                EditorGUILayout.PropertyField(iOSRemoteConfig);
                EditorGUILayout.PropertyField(androidRemoteConfig);
                EditorGUILayout.PropertyField(miniGameRemoteConfig);
#if TUANJIE_2022_3_OR_NEWER
                EditorGUILayout.PropertyField(openHarmonyRemoteConfig);
#endif
                EditorGUI.indentLevel -= 1;
                EditorGUILayout.HelpBox(ConstString.remoteConfigMsg, MessageType.Info);
            }

            // if (removeAndroidSDK.boolValue)
            // {
            //     androidRemoteConfig.boolValue = false;
            // }
            // else
            // {
            //     androidRemoteConfig.boolValue = true;
            // }


        }

        #endregion

        #region DrawSdkVersionSection

        //   private bool _useSpecifyVersion = false;

        private void DrawSdkVersionSection()
        {
            EditorGUILayout.PropertyField(useSpecifyVersion, new GUIContent("SDK Version"));

            //   _useSpecifyVersion = EditorGUILayout.Foldout(_useSpecifyVersion, "SDK Version");
            if (useSpecifyVersion.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(iOSVersion, new GUIContent("iOS Version"));
                // CheckVersionFormat(iOSVersion);
                EditorGUILayout.PropertyField(AndroidVersion);
                // CheckVersionFormat(AndroidVersion);
#if TUANJIE_2022_3_OR_NEWER
                EditorGUILayout.PropertyField(OpenHarmonyVersion);
                // CheckVersionFormat(OpenHarmonyVersion);
#endif
                // EditorGUILayout.PropertyField(MacOSVersion);
                // CheckVersionFormat(MacOSVersion);
                EditorGUI.indentLevel--;

                if (!iOSVersion.stringValue.Equals(SolarEngineSettings.iOSVersion))
                    SolarEngineSettings.iOSVersion = iOSVersion.stringValue;
                if (!AndroidVersion.stringValue.Equals(SolarEngineSettings.AndroidVersion))
                    SolarEngineSettings.AndroidVersion = AndroidVersion.stringValue;

                if (!OpenHarmonyVersion.stringValue.Equals(SolarEngineSettings.OpenHarmonyVersion))
                    SolarEngineSettings.OpenHarmonyVersion = OpenHarmonyVersion.stringValue;
                // if (!MacOSVersion.stringValue.Equals(SolarEngineSettings.MacOSVersion))
                //     SolarEngineSettings.MacOSVersion = MacOSVersion.stringValue;


                EditorGUILayout.HelpBox(ConstString.confirmVersion, MessageType.Warning);
            }
        }

        private void CheckVersionFormat(SerializedProperty prop)
        {
            var version = prop.stringValue;
            if (!Regex.IsMatch(version, @"^\d{1,3}(\.\d{1,3}){3}$") &&
                !string.IsNullOrEmpty(version))
                EditorGUILayout.HelpBox($"{prop.displayName} 格式错误，应为类似 1.x.x.x 的四段数字", MessageType.Error);
        }

        #endregion


        #region 私有化部署

        // private void DrawCustomDomainOption(GUIStyle darkerCyanTextFieldStyles)
        // {
        //     showDomain = EditorGUILayout.Foldout(showDomain, "Custom Domain", true);
        //
        //     if (showDomain)
        //     {
        //         EditorGUI.indentLevel += 1;
        //         EditorGUILayout.PropertyField(CustomDomainEnable, new GUIContent("Enable", "Set up Custom Domain"));
        //
        //         if (SolarEngineSettings.CustomDomainEnable != CustomDomainEnable.boolValue)
        //         {
        //             SolarEngineSettings.CustomDomainEnable = CustomDomainEnable.boolValue;
        //         }
        //
        //         if (CustomDomainEnable.boolValue)
        //         {
        //             EditorGUI.indentLevel += 1;
        //             EditorGUILayout.PropertyField(ReceiverDomain,
        //                 new GUIContent("Receiver Domain",
        //                     "receiver https domain name, including the following services:\nEvent reporting, debug mode event reporting, attribution, delayed deeplink"));
        //             EditorGUILayout.PropertyField(RuleDomain,
        //                 new GUIContent("Rule Domain",
        //                     "rule https domain includes the following services:\nRemote Config"));
        //             EditorGUILayout.PropertyField(ReceiverTcpHost,
        //                 new GUIContent("Receiver Tcp Host",
        //                     "receiver tcp host， Including the following businesses:\nAttribution and debug mode event reportingt"));
        //             EditorGUILayout.PropertyField(RuleTcpHost,
        //                 new GUIContent("Rule Tcp Host",
        //                     "rule tcp host， Including the following businesses:\nRemote Config"));
        //             EditorGUILayout.PropertyField(GatewayTcpHost,
        //                 new GUIContent("Gateway TcpHost",
        //                     "gateway  tcp host， Including the following businesses:\nEvent reporting"));
        //
        //             if (SolarEngineSettings.ReceiverDomain != ReceiverDomain.stringValue)
        //                 SolarEngineSettings.ReceiverDomain = ReceiverDomain.stringValue;
        //             if (SolarEngineSettings.RuleDomain != RuleDomain.stringValue)
        //                 SolarEngineSettings.RuleDomain = RuleDomain.stringValue;
        //             if (SolarEngineSettings.ReceiverTcpHost != ReceiverTcpHost.stringValue)
        //                 SolarEngineSettings.ReceiverTcpHost = ReceiverTcpHost.stringValue;
        //             if (SolarEngineSettings.RuleTcpHost != RuleTcpHost.stringValue)
        //                 SolarEngineSettings.RuleTcpHost = RuleTcpHost.stringValue;
        //             if (SolarEngineSettings.GatewayTcpHost != GatewayTcpHost.stringValue)
        //                 SolarEngineSettings.GatewayTcpHost = GatewayTcpHost.stringValue;
        //             
        //             EditorGUI.indentLevel -= 1;
        //           
        //         }
        //     }
        // }

        #endregion
    }
}