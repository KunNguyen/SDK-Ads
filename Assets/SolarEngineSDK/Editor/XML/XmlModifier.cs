using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using SolarEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

public class XmlModifier
{
    // 定义枚举类型，用于表示不同的类型
    enum StrongType
    {
        CN,
        Oversea,
        Default,
    }

    private const string SolorEngine = "[SolorEngine]";

    // 配置常量
    private const string SolarEngineNet = "Assets/SolarEngineNet/";
    private const string DEPENDDANCIDES = "Editor/Dependencies.xml";
    private const string IOS_SDK_XML_FILE_PATH = SolarEngineNet + "iOS/" + DEPENDDANCIDES;
    private const string ANDROID_SDK_XML_FILE_PATH = SolarEngineNet + "Android/" + DEPENDDANCIDES;
    private const string ANDROID_OAID_PATH = SolarEngineNet + "SolarEnginePlugins/Oaid/" + DEPENDDANCIDES;
    private const string IOS_ODMINFO_PATH= "Assets/SolarEngineNet/SolarEnginePlugins/ODMInfo/" + DEPENDDANCIDES;
    

    private const string IOS_POD_NODE_NAME = "iosPod";
    private const string IOS_POD_NAME_ATTR = "name";
    private const string IOS_POD_VERSION_ATTR = "version";

    // <androidPackage spec=
    private const string ANDROID_PACKAGE_NAME = "androidPackages";
    private const string ANDROID_PACKAGE_NAME_INNER = "androidPackage";
    private const string ANDROID_PACKAGE_SPEC_ATTR = "spec";

    //   <repositories>-->
    // <!--        <repository>https://maven-android.solar-engine.com/repository/se_sdk_for_android/</repository>-->
    private const string ANDROID_REPOSITORIES = "repositories";
    // 定义 repository repository大写
    private const string ANDROID_REPOSITORY_NAME = "repository";
    private const string ANDROID_REPOSITORY_URL_ATTR = "https://maven-android.solar-engine.com/repository/se_sdk_for_android/";

    // Oversea
    private const string IOS_POD_OVERSEA_NAME = "SolarEngineSDKiOSInter";

    private const string ANDROID_PACKAGE_OVERSEA_SPEC = "com.reyun.solar.engine.oversea:solar-engine-core:";

    // CN
    private const string IOS_POD_CN_NAME = "SolarEngineSDK";

    private static string ANDROID_PACKAGE_CN_SPEC = "com.reyun.solar.engine.china:solar-engine-core:";
    private const string IOS_ODMInfo_SPEC = "SESDKODMInfo";
    private const string IOS_GoogleUtilities_SPEC = "GoogleUtilities";
    private const string IOS_nanopb_SPEC = "nanopb";
    private const string IOS_GoogleAdsOnDeviceConversion_SPEC = "GoogleAdsOnDeviceConversion";
    
    
    
    private const string ANDROID_OAID_SPEC = "com.reyun.solar.engine:se-plugin-oaid:";
    //  "
    private const string ANDROID_HMS_SPEC = "com.huawei.hms:ads-identifier:3.4.62.300";
    //
    private const string ANDROID_MCS_SPEC = "com.hihonor.mcs:ads-identifier:1.0.2.301";
    // https://developer.huawei.com/repo
    private const string ANDROID_HMS_URL = "https://developer.huawei.com/repo/";
    private const string ANDROID_MCS_URL = "https://developer.hihonor.com/repo";

    private const string ANDROID_PACKAGE_Default = "";
    private const string ANDROID_PACKAGE_Default_VERSION = "+";
    private const string IOS_POD_Default = "";
    private const string IOS_POD_Default_VERSION = ">=0";

    /// <summary>
    /// 加载 XML 文档
    /// </summary>
    /// <param name="_filePath">XML 文件路径</param>
    /// <returns>加载成功返回 XDocument 对象，否则返回 null</returns>
    private static XDocument LoadXmlDocument(string _filePath)
    {
        string filePath = _filePath;
        XDocument doc;

        // 加载 XML 文件并处理可能的加载错误
        if (File.Exists(filePath))
        {
            doc = XDocument.Load(filePath);
        }
        else
        {
            Debug.LogError($"{SolorEngine}文件 {filePath} 不存在");
            return null;
        }

        return doc;
    }

    /// <summary>
    /// 保存 XML 文档
    /// </summary>
    /// <param name="doc">要保存的 XDocument 对象</param>
    /// <param name="_filePath">XML 文件路径</param>
    private static void SaveXmlDocument(XDocument doc, string _filePath)
    {
        string filePath = _filePath;

        // 保存修改后的 XML 文件并处理可能的保存错误
        try
        {
            doc.Save(filePath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"保存 XML 文件时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 修改 iOS 相关节点的属性
    /// </summary>
    /// <param name="doc">要修改的 XDocument 对象</param>
    private static bool ModifyIOSNodes(XDocument doc, string _name, StrongType type = StrongType.CN,bool isChangeVersion=true)
    {
        bool nameSetSuccess = false;
        bool versionSetSuccess = false;

        var iosPod = doc.Descendants(IOS_POD_NODE_NAME).FirstOrDefault();
      //  Debug.Log($"{SolorEngine}修改iOS节点"+iosPod);
        if (iosPod!= null)
        {
            var nameAttribute = iosPod.Attribute(IOS_POD_NAME_ATTR);
            if (nameAttribute!= null)
            {
                try
                {
                    nameAttribute.Value = _name;
                    nameSetSuccess = true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"设置 iOS pod 名称时出错: {ex.Message}");
                }
            }

            var versionAttribute = iosPod.Attribute(IOS_POD_VERSION_ATTR);
            if (versionAttribute!= null)
            {
                try
                {
                    if (type == StrongType.Default||!isChangeVersion)
                    {
                        versionAttribute.Value = IOS_POD_Default_VERSION;
                    }
                    else
                    {
                        versionAttribute.Value = string.IsNullOrEmpty(SolarEngineSettings.iOSVersion)
                           ? IOS_POD_Default_VERSION
                            : SolarEngineSettings.iOSVersion;
                    }

                    versionSetSuccess = true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"设置 iOS pod 版本时出错: {ex.Message}");
                }
            }
        }

        return nameSetSuccess && versionSetSuccess;
    }

    private static bool ModifyIOSNodes(XDocument doc,StrongType type = StrongType.Oversea)
    {
        try
        {
           
            // 先删除旧的 iosPod 节点
            doc.Descendants("iosPods").Remove();
            if (type == StrongType.Default)
                return true;
            // 新建固定的 4 个节点
            XElement root = doc.Root;

            if (root == null)
            {
                Debug.LogError("XML 文档没有根节点，无法添加 iosPod 节点");
                return false;
            }
            var iosPods = new XElement("iosPods");

            iosPods.Add(new XElement("iosPod",
                new XAttribute("name", "SESDKODMInfo"),
                new XAttribute("version", "1.0.0.1")));

            iosPods.Add(new XElement("iosPod",
                new XAttribute("name", "GoogleUtilities"),
                new XAttribute("version", "8.1")));

            iosPods.Add(new XElement("iosPod",
                new XAttribute("name", "nanopb"),
                new XAttribute("version", "3.30910.0")));

            iosPods.Add(new XElement("iosPod",
                new XAttribute("name", "GoogleAdsOnDeviceConversion"),
                new XAttribute("version", "2.1.0")));
           root.Add(iosPods);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"修改 iOS 节点时出错: {ex.Message}");
            return false;
        }
    }

    
    private static bool iOSODMINFO(StrongType strongType = StrongType.Oversea)
    {
        bool isModified = false;

        if (!SolarEngineSettings.isUseODMInfo && strongType != StrongType.Default)
        {

            return true;

        }
          
        else
        {
          
            XDocument docRemote = LoadXmlDocument(IOS_ODMINFO_PATH);

            if (docRemote!= null)
            {
                if (strongType == StrongType.Default)
                {

                    isModified = ModifyIOSNodes(docRemote, strongType);
                }
                else
                {
                    isModified = ModifyIOSNodes(docRemote);
                    
                    // isModified = ModifyIOSNodes(docRemote, IOS_GoogleUtilities_SPEC,strongType, false);
                    //
                    // isModified = ModifyIOSNodes(docRemote, IOS_nanopb_SPEC,strongType, false);

                    
                }

                SaveXmlDocument(docRemote, IOS_ODMINFO_PATH);
            }
        }

        return isModified;
    }

    private static bool AndroidOaid(StrongType strongType = StrongType.CN)
    {
        bool isModified = false;

        if (!SolarEngineSettings.isUseOaid && strongType!= StrongType.Default)
            return true;
        else
        {
            try
            {
                XDocument docOaid = LoadXmlDocument(ANDROID_OAID_PATH);

                if (docOaid!= null)
                {
                    XElement dependenciesElement = docOaid.Descendants("dependencies").FirstOrDefault();

                    if (dependenciesElement!= null)
                    {
                        dependenciesElement.RemoveAll();

                        if (strongType == StrongType.Default)
                        {
                            isModified = true;
                        }
                        else
                        {
                            string version = string.IsNullOrEmpty(SolarEngineSettings.AndroidVersion)
                               ? ANDROID_PACKAGE_Default_VERSION
                                : SolarEngineSettings.AndroidVersion;
                            isModified = ModifyAndroidNodeWithAndroidPackages(dependenciesElement, ANDROID_OAID_SPEC + version, false, null, true);
                        }
                    }
                    else
                    {
                        Debug.LogError("dependenciesElement is null.");
                    }

                    SaveXmlDocument(docOaid, ANDROID_OAID_PATH);
                }
                else
                {
                    Debug.LogError("Android OAID document is null.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in AndroidOaid method: {ex.Message}");
            }
        }

        return isModified;
    }

    private static XElement dependenciesElement(XDocument doc)
    {
        // 修改 androidPackages 下节点的 spec 属性
        var dependenciesElement = doc.Descendants("dependencies").FirstOrDefault();
        return dependenciesElement;
    }

    private static bool ModifyAndroidNodeWithAndroidPackages(XElement dependenciesElement, string _spec, bool includeRepository = false, string _repositoryUrl = null, bool isOaid = false)
    {
        try
        {
            XElement newContent = createTargetPackageElement(_spec, includeRepository, _repositoryUrl);
            dependenciesElement.Add(newContent);

            if (isOaid)
            {
                XElement newContent2 = createTargetPackageElement(ANDROID_HMS_SPEC, true, ANDROID_HMS_URL);
                dependenciesElement.Add(newContent2);
                XElement newContent3 = createTargetPackageElement(ANDROID_MCS_SPEC, true, ANDROID_MCS_URL);
                dependenciesElement.Add(newContent3);
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"{SolorEngine}Error modifying XML file (Android) {e.Message}");
            return false;
        }
    }

   
    public static void defaultXml()
    {
        sdkSetting(StrongType.Default);
        // AndroidRC(StrongType.Default);
        AndroidOaid(StrongType.Default);
       // iOSRC(StrongType.Default);
        iOSODMINFO(StrongType.Default);
    }

   
    /// <summary>
    /// 整体执行修改 XML 文件操作（针对 CN 情况），包括加载、省略号)
    /// </summary>
    /// <param name="boolVale">控制是否执行修改操作的布尔值</param>
    public static bool cnxml(bool boolVale)
    {
        try
        {
            if (sdkSetting(StrongType.CN) && AndroidOaid())
            {
                Debug.Log($"{SolorEngine}set SDK to CN");
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError( $"{SolorEngine }Error modifying XML file (CN) {ex.Message}");
            return false;
        }
    }

    public static bool iOSDefault()
    {
      return  iOSSetting(StrongType.Default);
    //   &&
    //     iOSRC(StrongType.Default);
    }
    public  static bool AndroidDefault( )
    {
        return androidSetting(StrongType.Default) &&
               // AndroidRC(StrongType.Default) &&
               AndroidOaid(StrongType.Default);


    }

    /// <summary>
    /// 整体执行修改 XML 文件操作（针对 Oversea 情况），包括加载、修改和保存
    /// </summary>
    /// <param name="boolVale">控制是否执行修改操作的布尔值</param>
    public static bool Overseaxml(bool boolVale)
    {
        try
        {
          
            if (sdkSetting(StrongType.Oversea) && AndroidOaid() && iOSODMINFO())
            {
                Debug.Log($"{SolorEngine}set SDK to Oversea");
                return true;
            }
            else
                return false;
        }
        catch (Exception ex)
        {
            Debug.LogError(SolorEngine + "Error modifying XML file (oversea) "+ex.Message);
            return false;
        }
    }

    public static bool isAndroidPackage()
    {
        var androidDoc = LoadXmlDocument(ANDROID_SDK_XML_FILE_PATH);
        return androidDoc.Descendants(ANDROID_PACKAGE_NAME).Any();
    }
  
    static bool sdkSetting(StrongType type)
    {
         return iOSSetting(type)&& androidSetting(type);
      
    }


    static bool iOSSetting(StrongType type)
    {
        bool isIosSetSuccess = false;
        var iosDoc = LoadXmlDocument(IOS_SDK_XML_FILE_PATH);
        if (iosDoc!= null)
        {
            try
            {
                switch (type)
                {
                    case StrongType.CN:
                        isIosSetSuccess = ModifyIOSNodes(iosDoc, IOS_POD_CN_NAME);
                        break;
                    case StrongType.Oversea:
                        isIosSetSuccess = ModifyIOSNodes(iosDoc, IOS_POD_OVERSEA_NAME);
                        break;
                    case StrongType.Default:
                        isIosSetSuccess = ModifyIOSNodes(iosDoc, IOS_POD_Default,type);
                        break;
                }

                SaveXmlDocument(iosDoc, IOS_SDK_XML_FILE_PATH);
            }
            catch (Exception ex)
            {
                Debug.LogError($"iOS error: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("iOS doc null.");
        }

        return isIosSetSuccess;
        
    }

    static bool androidSetting(StrongType type)
    {
        if (SolarEngineSettings.removeAndroidSDK)
            return true;
        bool isAndroidSetSuccess = false;

       
        var androidDoc = LoadXmlDocument(ANDROID_SDK_XML_FILE_PATH);

        
        if (androidDoc!= null)
        {
            try
            {
                var androidDepElement = dependenciesElement(androidDoc);
                if (androidDepElement!= null)
                {
                    androidDepElement.RemoveAll();
                    var androidVersion = string.IsNullOrEmpty(SolarEngineSettings.AndroidVersion)
                        ? ANDROID_PACKAGE_Default_VERSION
                        : SolarEngineSettings.AndroidVersion;

                    switch (type)
                    {
                        case StrongType.CN:
                            isAndroidSetSuccess = ModifyAndroidNodeWithAndroidPackages(androidDepElement, ANDROID_PACKAGE_CN_SPEC + androidVersion,true,ANDROID_REPOSITORY_URL_ATTR);
                            break;
                        case StrongType.Oversea:
                            isAndroidSetSuccess = ModifyAndroidNodeWithAndroidPackages(androidDepElement, ANDROID_PACKAGE_OVERSEA_SPEC + androidVersion,true,ANDROID_REPOSITORY_URL_ATTR);
                            break;
                        case StrongType.Default:
                            isAndroidSetSuccess = true;
                            break;
                    }
                }

                SaveXmlDocument(androidDoc, ANDROID_SDK_XML_FILE_PATH);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Android error: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError("Android doc null.");
        }

        return isAndroidSetSuccess;
    }
    
    
    // [MenuItem("SDk/Test")]
    public static void test()
    {
        AndroidOaid(StrongType.CN);
    }

    public static XElement createTargetPackageElement(string packageSpec, bool includeRepository = false, string repositoryUrl = null)
    {
        XElement innerPackageElement = new XElement(ANDROID_PACKAGE_NAME_INNER, new XAttribute(ANDROID_PACKAGE_SPEC_ATTR, packageSpec));
        XElement packageElement = new XElement(ANDROID_PACKAGE_NAME, innerPackageElement);


       if (includeRepository)
       {
           XElement repositoriesElement = new XElement(ANDROID_REPOSITORIES, new XElement(ANDROID_REPOSITORY_NAME, repositoryUrl));
           innerPackageElement.Add(repositoriesElement);
       }

       return packageElement;
   }


  

}
