using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace JisSDKAds.Common
{
    public static class SymbolHelper
    {
#if UNITY_EDITOR
        public static HashSet<string> GetDefineSymbols(BuildTargetGroup group)
        {
            var raw = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            return new HashSet<string>(
                raw.Split(';').Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        public static void AddDefineSymbol(string defineSymbol) =>
            AddDefineSymbol(EditorUserBuildSettings.selectedBuildTargetGroup, defineSymbol);

        public static void AddDefineSymbol(BuildTargetGroup group, string defineSymbol)
        {
            if (string.IsNullOrWhiteSpace(defineSymbol))
                return;

            var set = GetDefineSymbols(group);
            if (set.Contains(defineSymbol))
                return;

            set.Add(defineSymbol);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", set));
        }

        public static void AddDefineSymbols(List<string> defineSymbols) =>
            AddDefineSymbols(EditorUserBuildSettings.selectedBuildTargetGroup, defineSymbols);

        public static void AddDefineSymbols(BuildTargetGroup group, List<string> defineSymbols)
        {
            if (defineSymbols == null || defineSymbols.Count == 0)
                return;

            var set = GetDefineSymbols(group);
            var changed = false;
            foreach (var defineSymbol in defineSymbols)
            {
                if (string.IsNullOrWhiteSpace(defineSymbol) || set.Contains(defineSymbol))
                    continue;
                set.Add(defineSymbol);
                changed = true;
            }

            if (!changed)
                return;

            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", set));
        }

        public static void RemoveDefineSymbol(string defineSymbol) =>
            RemoveDefineSymbol(EditorUserBuildSettings.selectedBuildTargetGroup, defineSymbol);

        public static void RemoveDefineSymbol(BuildTargetGroup group, string defineSymbol)
        {
            if (string.IsNullOrWhiteSpace(defineSymbol))
                return;

            var set = GetDefineSymbols(group);
            if (!set.Remove(defineSymbol))
                return;

            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", set));
        }

        /// <summary>
        /// Ensures mediation symbols match exactly (adds required, removes unused MAX/ADMOB).
        /// </summary>
        public static void SyncMediationDefines(BuildTargetGroup group, IEnumerable<string> mediationSymbols)
        {
            var desired = new HashSet<string>(
                mediationSymbols?.Where(s => !string.IsNullOrWhiteSpace(s)) ?? Enumerable.Empty<string>());

            foreach (var sym in new[] { "UNITY_AD_MAX", "UNITY_AD_ADMOB" })
            {
                if (desired.Contains(sym))
                    AddDefineSymbol(group, sym);
                else
                    RemoveDefineSymbol(group, sym);
            }
        }

        public static void SetOptionalDefine(BuildTargetGroup group, string symbol, bool enabled)
        {
            if (enabled)
                AddDefineSymbol(group, symbol);
            else
                RemoveDefineSymbol(group, symbol);
        }
#endif
    }
}
