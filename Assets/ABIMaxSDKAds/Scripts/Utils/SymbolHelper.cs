using System.Collections.Generic;
using UnityEditor;

namespace ABIMaxSDKAds.Scripts.Utils
{
     public static class SymbolHelper
     {
#if UNITY_EDITOR
         public static void AddDefineSymbol(string defineSymbol)
         {
             string currentDefineSymbols =
                 PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
             string[] defineSymbols = currentDefineSymbols.Split(';');
             List<string> defineSymbolList = new List<string>(defineSymbols);
             currentDefineSymbols = string.Join(";", defineSymbolList.ToArray());
             if (currentDefineSymbols.Contains(defineSymbol)) return;
             currentDefineSymbols += ";" + defineSymbol;
             PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                 currentDefineSymbols);
         }

         public static void AddDefineSymbols(List<string> defineSymbols)
         {
             string currentDefineSymbols =
                 PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
             string[] currentDefineSymbolList = currentDefineSymbols.Split(';');
             List<string> defineSymbolList = new List<string>(currentDefineSymbolList);
             foreach (var defineSymbol in defineSymbols)
             {
                 if (!defineSymbolList.Contains(defineSymbol))
                 {
                     defineSymbolList.Add(defineSymbol);
                 }
             }

             currentDefineSymbols = string.Join(";", defineSymbolList.ToArray());
             PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                 currentDefineSymbols);

         }

         public static void RemoveDefineSymbol(string defineSymbol)
         {
             string currentDefineSymbols =
                 PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
             string[] defineSymbols = currentDefineSymbols.Split(';');
             List<string> defineSymbolList = new List<string>(defineSymbols);
             if (defineSymbolList.Contains(defineSymbol))
             {
                 defineSymbolList.Remove(defineSymbol);
             }

             currentDefineSymbols = string.Join(";", defineSymbolList.ToArray());
             PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                 currentDefineSymbols);
         } 
#endif
     }
}