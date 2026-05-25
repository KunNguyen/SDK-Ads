using System.Collections.Generic;
using UnityEditor;

namespace JisSDKAds.Common
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
             if (defineSymbols == null || defineSymbols.Count == 0)
                 return;

             var group = EditorUserBuildSettings.selectedBuildTargetGroup;
             var currentDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
             var defineSymbolList = new List<string>(currentDefineSymbols.Split(';'));
             var changed = false;
             foreach (var defineSymbol in defineSymbols)
             {
                 if (string.IsNullOrEmpty(defineSymbol) || defineSymbolList.Contains(defineSymbol))
                     continue;
                 defineSymbolList.Add(defineSymbol);
                 changed = true;
             }

             if (!changed)
                 return;

             PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defineSymbolList));
         }

         public static void RemoveDefineSymbol(string defineSymbol)
         {
             if (string.IsNullOrEmpty(defineSymbol))
                 return;

             var group = EditorUserBuildSettings.selectedBuildTargetGroup;
             var currentDefineSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
             var defineSymbolList = new List<string>(currentDefineSymbols.Split(';'));
             if (!defineSymbolList.Contains(defineSymbol))
                 return;

             defineSymbolList.Remove(defineSymbol);
             PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defineSymbolList));
         } 
#endif
     }
}