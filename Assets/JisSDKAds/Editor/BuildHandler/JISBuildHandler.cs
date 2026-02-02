using ABIMaxSDKAds.Scripts.Utils;
using UnityEditor;
using UnityEngine;

namespace ABIMaxSDKAds.Editor
{
     public static class JISBuildHandler
     {
          [InitializeOnLoadMethod]
          private static void RegisterBuildHandler()
          {
               BuildPlayerWindow.RegisterBuildPlayerHandler(OnBuildPlayer);
          }

          private static void OnBuildPlayer(BuildPlayerOptions buildPlayerOptions)
          {
               bool isExportingProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;

               if (EditorUtility.DisplayDialog(
                        "Confirm Build",
                        isExportingProject 
                             ? "Please select the type of build to be exported." 
                             : "Please select the build type to be built.",
                        "Release Build",
                        "Debug Build"))
               {
                    ApplyDebugSymbols(false);
                    BuildPipeline.BuildPlayer(buildPlayerOptions);    
               }
               else
               {
                    ApplyDebugSymbols(true);
                    BuildPipeline.BuildPlayer(buildPlayerOptions);
               }
          }
          private static void ApplyDebugSymbols(bool isDebug)
          {
               // Get the current build target group
               BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

               // Set the debug symbols for the current build target group
               if (isDebug)
               {
                    SymbolHelper.AddDefineSymbol("DEBUG_ADS");     
               }
               else
               {
                    SymbolHelper.RemoveDefineSymbol("DEBUG_ADS");
               }
               
          }
     }
}