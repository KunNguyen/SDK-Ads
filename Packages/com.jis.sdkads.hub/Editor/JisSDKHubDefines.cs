#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace JisSDKAds.Hub
{
    internal static class JisSDKHubDefines
    {
        public const string Max = "UNITY_AD_MAX";
        public const string AdMob = "UNITY_AD_ADMOB";

        private static readonly BuildTargetGroup[] Groups =
        {
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
            BuildTargetGroup.Standalone
        };

        public static bool HasDefine(string symbol) =>
            Groups.Any(g =>
            {
                var set = new HashSet<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(g)
                    .Split(';').Where(s => !string.IsNullOrEmpty(s)));
                return set.Contains(symbol);
            });

        public static void SetDefine(string symbol, bool enabled)
        {
            foreach (var group in Groups)
            {
                var set = new HashSet<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(group)
                    .Split(';').Where(s => !string.IsNullOrEmpty(s)));
                if (enabled) set.Add(symbol);
                else set.Remove(symbol);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", set));
            }
        }
    }
}
#endif
