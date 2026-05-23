using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SolarEngine.Build;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;

public class PluginsEdtior : MonoBehaviour
{
   
   private const string SolorEngine = "[SolorEngine]";



   private const string PluginsSolarEnginePath = "Assets/Plugins/SolarEngine/";
   private const string RemoteConfigsCSPath = "Assets/SolarEngineSDK/RemoteConfigWrapper";
   
   private const string SolarEngineNet = "Assets/SolarEngineNet/";
   
   private const string IOS_SDK = SolarEngineNet + "iOS";
   private const string ANDROID_SDK = SolarEngineNet + "Android";

   private const string ANDROID_OAID_PATH = SolarEngineNet + "SolarEnginePlugins/Oaid";
   
   
   
   private const string oaidPath = SolarEngineNet+"SolarEnginePlugins/Oaid";
   private const string odminfoPath = SolarEngineNet+"SolarEnginePlugins/ODMInfo";
   
   //minigamepath
    private const string MiniGameRemoteConfigsPathMiniCS = "Assets/SolarEngineSDK/RemoteConfigWrapper/SESDKRemoteConfigMiniGameWrapper.cs";
   private const string MiniGameRemoteConfigsPathMiniDll = PluginsSolarEnginePath+"MiniGame/MiniGameRemoteConfig.dll";
   
   private const string RemoteConfigsPathOpenHarCS = "Assets/SolarEngineSDK/RemoteConfigWrapper/SESDKRemoteConfigOpenHarmonyWrapper.cs";
   // private const string RemoteConfigsPathOpenHar = "Assets/Plugins/OpenHarmony/SolarEngine/RemoteConfig";

   
   //iospath
  private const string  RemoteConfigsPathiOSCS = "Assets/SolarEngineSDK/RemoteConfigWrapper/SESDKRemoteConfigiOSWrapper.cs";
   private const string RemoteConfigsPathiOSMM = PluginsSolarEnginePath+"iOS/wrappers/SESDKRemoteConfigUnityBridge.mm";
   private const string RemoteConfigsPathiOSH =  PluginsSolarEnginePath+"iOS/wrappers/SESDKRemoteConfigUnityBridge.h";
   //androidpath
    private const string RemoteConfigsPathAndroidCS = "Assets/SolarEngineSDK/RemoteConfigWrapper/SESDKRemoteConfigAndroidWrapper.cs";
   private const string ConfigsPathAndroidJar = PluginsSolarEnginePath+"Android/libs/se_remote_config_unity_bridge.jar";



   public static bool disableiOSSDK()
   {
       DefineSymbolsEditor.add_DISABLE_REMOTECONFIG(BuildTargetGroup.iOS,false);
       return
           HidePath(IOS_SDK);
   }
   public static bool disableAndroidSDK()
   {
     return  HidePath(ANDROID_SDK);
     
   }
   
   
   public static bool enableiOSSDK()
   {

       return ShowPath(IOS_SDK);


   }
   public static bool enableAndroidSDK()
   {
       return  ShowPath(ANDROID_SDK);

   }
   
   
   
  // [MenuItem(DisableAll, false, 0)]
   public static void disableAll ()
   {
      HidePath(RemoteConfigsCSPath);

   }
   public static void showAll ()
   {
      // showMiniGame();
      // showiOS();
      // showAndroid();
      ShowPath(RemoteConfigsCSPath);

   }
  
  // [MenuItem(DisableMiniGame, false, 0)]
   public static bool disableMiniGame ()
   {
       DefineSymbolsEditor.add_DISABLE_REMOTECONFIG(BuildTargetGroup.WebGL,true);
       DefineSymbolsEditor.add_DISABLE_REMOTECONFIG(BuildTargetGroup.Android,true);
#if TUANJIE_2022_3_OR_NEWER
       DefineSymbolsEditor.add_DISABLE_REMOTECONFIG(BuildTargetGroup.WeixinMiniGame,true);
#endif
       return HideFile(MiniGameRemoteConfigsPathMiniDll)
           && HideFile(MiniGameRemoteConfigsPathMiniCS);

   }
   
   public static bool showMiniGame ()
   {
       DefineSymbolsEditor.delete_DISABLE_REMOTECONFIG(BuildTargetGroup.WebGL,true);
       DefineSymbolsEditor.delete_DISABLE_REMOTECONFIG(BuildTargetGroup.Android,true);
#if TUANJIE_2022_3_OR_NEWER
       DefineSymbolsEditor.delete_DISABLE_REMOTECONFIG(BuildTargetGroup.WeixinMiniGame,true);
#endif
       return ShowFile(MiniGameRemoteConfigsPathMiniDll)
        && ShowFile(MiniGameRemoteConfigsPathMiniCS);

   }
   public static bool showOpenHarmony()
   {
#if TUANJIE_2022_3_OR_NEWER

       DefineSymbolsEditor.delete_DISABLE_REMOTECONFIG(BuildTargetGroup.OpenHarmony,false);
#endif
       return ShowFile(RemoteConfigsPathOpenHarCS) ;

   }
   public static bool disableOpenHarmony()
   {
#if TUANJIE_2022_3_OR_NEWER

       DefineSymbolsEditor.add_DISABLE_REMOTECONFIG(BuildTargetGroup.OpenHarmony,false);
#endif
       return HideFile(RemoteConfigsPathOpenHarCS);
    
   }







   // [MenuItem(DisableiOS, false, 0)]
   public static bool disableiOS ()
   {
       DefineSymbolsEditor.add_DISABLE_REMOTECONFIG(BuildTargetGroup.iOS,false);
       return  
        HideFile(RemoteConfigsPathiOSCS)&&
       HideFile(RemoteConfigsPathiOSMM)&&
       HideFile(RemoteConfigsPathiOSH);
   }
   public static bool showiOS ()
   {
       DefineSymbolsEditor.delete_DISABLE_REMOTECONFIG(BuildTargetGroup.iOS,false);
      return
       ShowFile(RemoteConfigsPathiOSCS)&&
       ShowFile(RemoteConfigsPathiOSMM)&&
       ShowFile(RemoteConfigsPathiOSH);
   }

   public static void removeRC()
   {
       string legacyRemoteConfigPath = System.IO.Path.Combine(
           "Assets",
           "SolarEngineNet",
           "SolarEnginePlugins",
           "RemoteConfigSDK");

       if (System.IO.Directory.Exists(legacyRemoteConfigPath))
       {
           Debug.Log($"{SolorEngine}Removing legacy RemoteConfigSDK directory...");
           System.IO.Directory.Delete(legacyRemoteConfigPath, true);
       }

       string legacyRemoteConfigMetaPath = legacyRemoteConfigPath + ".meta";
       if (System.IO.File.Exists(legacyRemoteConfigMetaPath))
       {
           System.IO.File.Delete(legacyRemoteConfigMetaPath);
       }
   }

   public static void disableMacOS()
   {
       DefineSymbolsEditor.add_DISABLE_REMOTECONFIG(BuildTargetGroup.Standalone,false);

   }
   
   public static void showMacOS ()
   {
       DefineSymbolsEditor.delete_DISABLE_REMOTECONFIG(BuildTargetGroup.Standalone,false);
     
   }
   
  //[MenuItem(DisableAndroid, false, 0)]
   public static bool disableAndroid ()
   { 
       DefineSymbolsEditor.add_DISABLE_REMOTECONFIG(BuildTargetGroup.Android,false);
       return HideFile(ConfigsPathAndroidJar) &&
              HideFile(RemoteConfigsPathAndroidCS);

   }
   public static bool showAndroid ()
   {
       DefineSymbolsEditor.delete_DISABLE_REMOTECONFIG(BuildTargetGroup.Android,false);

       return
           ShowFile(ConfigsPathAndroidJar) &&
           ShowFile(RemoteConfigsPathAndroidCS);
   }


   public static bool disableOaid()
   {
       return HidePath(oaidPath);
   }
   public static bool showOaid()
   {
     return  ShowPath(oaidPath);
   }
   
   

   public static bool disableODMInfo()
   {
       return HidePath(odminfoPath);
   }
   public static bool showODMInfo()
   {
       return  ShowPath(odminfoPath);
   }

 
   public static bool HideFile(string path)
   {
      
       if (File.Exists(path))
       {

           if (File.Exists(path + "~"))
           {
               System.IO. File.Delete(path + "~");
           }
           System.IO. File.Move(path, path + "~");

           return true;
           //  Debug.LogWarning($"{SolorEngine} The file at path '{path}' is successfully hidden.");
       }
        if (File.Exists(path + "~"))
       {
           return true;
       }
       else
       {
           Debug.LogError(($"{SolorEngine}The file at path '{path}' does not exist"));
       }
      
       return false;
   }
   
   
   static bool IsDirectoryEmpty(string directoryPath)
   {
       return! System.IO.Directory.EnumerateFileSystemEntries(directoryPath).Any();
   }

   public static bool HidePath(string path)
   {
    
           if (System.IO.Directory.Exists(path))
           {
              
               if( System.IO.Directory.Exists(path + "~"))
                   System.IO.Directory.Delete(path + "~",true);
               System.IO.Directory.Move(path, path + "~");
               System.IO.File.Delete(path+".meta");
               return true;
           
         
           }

           if (System.IO.Directory.Exists(path + "~"))
               return true;
         
            
           else
           {
               Debug.LogError(($"{SolorEngine}The file at path '{path}' does not exist"));
           }
   

       return false;


   }

   public static bool ShowFile(string path)
   {
       try
       {
           if (File.Exists(path))
           {
               return true;
           }
           else if (File.Exists(path + "~"))
           {
               System.IO. File.Move(path + "~", path);
               return true;
           }
           else
           {
               
               Debug.LogError(($"{SolorEngine}The file at path '{path}' does not exist"));

           }
       }
       catch (Exception e)
       {
           Debug.LogError(SolorEngine+e);

           return false;
       }
    
     

       return false;
   }
 

   public static bool ShowPath(string path)
   {
       try
       {
           if (System.IO.Directory.Exists(path + "~"))
           {
             
               if (System.IO.Directory.Exists(path))
               {
                   System.IO.Directory.Delete(path,true) ;
               }
               System.IO.Directory.Move(path + "~", path);

               return true;
           }
           else if( System.IO.Directory.Exists(path))
           {
               return true;
           }
           else
           {
               Debug.LogError(($"{SolorEngine}The file at path '{path}' does not exist"));

               return false;
           }
      }
       catch (Exception e)
       {
          Debug.LogError(SolorEngine+e);
           return false;
          
   }
    
      
   }
  
   
   
   
}
