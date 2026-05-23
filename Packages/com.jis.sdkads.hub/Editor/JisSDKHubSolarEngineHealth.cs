#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.Hub
{
    internal static class JisSDKHubSolarEngineHealth
    {
        public static void DrawSolarEngineSetupWarning()
        {
            if (!JisSDKHubManifest.HasDependency("com.jis.sdkads.analytics.solarengine"))
                return;

            var sdkAsmdef = Path.Combine(Application.dataPath, "SolarEngineSDK", "SolarEngineSDK.asmdef");
            if (File.Exists(sdkAsmdef))
                return;

            EditorGUILayout.HelpBox(
                "SolarEngine JIS module is installed but SolarEngine Unity SDK (C#) was not found.\n" +
                "Expected: Assets/SolarEngineSDK/SolarEngineSDK.asmdef\n" +
                "Import the full SolarEngine Unity SDK (not only SolarEngineNet native plugins).\n" +
                "See docs/SOLARENGINE_SETUP.md",
                MessageType.Error);
        }
    }
}
#endif
