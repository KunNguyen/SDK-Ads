using System.Collections;
using System.Collections.Generic;
using System.IO;
using SolarEngine;
using SolarEngineSDK.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
[InitializeOnLoad]
public class SolorEnginePackageManager : MonoBehaviour
{

    private static readonly string _packageName = "solarengine-unity-sdk";
    static SolorEnginePackageManager()
    {
        AssetDatabase.importPackageCompleted += OnImportFinishHandle;
    }
    static void OnImportFinishHandle(string packageName)
    {
        if (packageName ==_packageName)
        {
            finishHandle(false);
            cleanupOpenHarmonyBridge();
        }

    }
    

    static void finishHandle()
    {
     
        
     
    }

  
    
    static void finishHandle(bool isShow=false)
    {

        ApplySetting._applySetting(isShow);
    }
    


    private static void cleanupOpenHarmonyBridge()
    {
        // 已清理过？直接跳过
        if (File.Exists(OpenHarmonyCleanupFlagPath))
        {
            Debug.LogWarning("[SolarEngine] cleanupOpenHarmonyBridge skipped (already cleaned).");
            return;
        }

        string root = Path.Combine(Application.dataPath, "Plugins/OpenHarmony/SolarEngine");

        string[] deleteTargets =
        {
            "RemoteConfig", // <-- 直接删除整个文件夹
            "RemoteConfig.meta",     
            "SENativeBridge.etslib",
            "SEOpenHarmonyProxy.ets"
        };

        foreach (var target in deleteTargets)
        {
            string fullPath = Path.Combine(root, target).Replace("\\", "/");

            // 如果是目录，则删除目录
            if (Directory.Exists(fullPath))
            {
                try
                {
                    Directory.Delete(fullPath, true);
                    Debug.LogWarning($"[SolarEngine] 🗂️ Deleted folder: {fullPath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SolarEngine] ❌ Failed to delete folder: {fullPath}\n{e}");
                }
            }
            // 如果是文件，则删除文件
            else if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                    Debug.LogWarning($"[SolarEngine] 📄 Deleted file: {fullPath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SolarEngine] ❌ Failed to delete file: {fullPath}\n{e}");
                }
            }
            else
            {
                Debug.LogWarning($"[SolarEngine] ⚠ Not found: {fullPath}");
            }
        }

        // 创建清理标记
        CreateOpenHarmonyCleanupFlag();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 创建 OpenHarmony 清理标记文件
    /// </summary>
    private static void CreateOpenHarmonyCleanupFlag()
    {
        string folder = Path.GetDirectoryName(OpenHarmonyCleanupFlagPath);
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        File.WriteAllText(OpenHarmonyCleanupFlagPath, System.DateTime.Now.ToString());
    }

   
    private const string OpenHarmonyCleanupFlagPath = "Library/SolarEngine/SESDK_OpenHarmony_CLEANED.flag";
    
    [MenuItem("SolarEngineSDK/Documentation/UnityDocumentation", false, 0)]
    static void unityDocumentation()
    {
        Application.OpenURL("https://help.solar-engine.com/cn/docs/51FROeEQ");
    }
    
    [MenuItem(ConstString.MenuItem.iOSChangelog, false, 0)]
    static void solarEngineDocsiOS()
    {
        Application.OpenURL("https://help.solar-engine.com/cn/docs/geng-xin-ri-zhi-RAvv");
    }
    [MenuItem(ConstString.MenuItem.androidChangelog, false, 0)]
    static void solarEngineDocsAndroid()
    {
        Application.OpenURL("https://help.solar-engine.com/cn/docs/geng-xin-ri-zhi");
    }
    
    private const string storageWarning = "You can only choose either China or Overseas！";
    private const string nostorageWarning = "You must choose either China or Overseas!";
  

}

#if UNITY_EDITOR


public static class PackageChecker
{
    private static string packagePath = "";
    public static bool IsUPMPackageInstalled(string packageName="com.solarengine.sdk")
    {
        var listRequest = Client.List(true, false);
        while (!listRequest.IsCompleted) {} // 等待完成

        if (listRequest.Status == StatusCode.Success)
        {
            foreach (var pkg in listRequest.Result)
            {

                if (pkg.name == packageName)
                {
                    packagePath = pkg.resolvedPath;
                    return true;
                }
                   
            }
        }
        return false;
    }

    public static string GetPackagePath()
    {
        return packagePath;
    }
    
}
#endif
