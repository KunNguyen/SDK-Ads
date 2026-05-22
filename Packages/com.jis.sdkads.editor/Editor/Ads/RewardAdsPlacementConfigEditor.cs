#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using JisSDKAds.Ads;
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.Editor
{
    [CustomEditor(typeof(RewardAdsPlacementConfig))]
    public class RewardAdsPlacementConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(6);

            if (GUILayout.Button("Generate Ads Placement IDs Enum"))
            {
                var config = (RewardAdsPlacementConfig)target;
                GenerateAdsPlacementIds(config);
            }
        }

        static void GenerateAdsPlacementIds(RewardAdsPlacementConfig config)
        {
            const string filePathAndName = "Assets/ABIMaxSDKAds/Scripts/Ads/";
            Generate("WatchVideoRewardType", filePathAndName, config.placementIds.ToArray(), "SDK");
        }

        static void Generate(string enumName, string path, string[] enumEntries, string namespaceName = "")
        {
            var filePath = path + enumName + ".cs";
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? path);

            using var streamWriter = new StreamWriter(filePath);
            if (!string.IsNullOrEmpty(namespaceName))
            {
                streamWriter.WriteLine("namespace " + namespaceName);
                streamWriter.WriteLine("{");
            }

            streamWriter.WriteLine("public enum " + enumName);
            streamWriter.WriteLine("{");
            foreach (var t in enumEntries)
                streamWriter.WriteLine("\t" + t + ",");
            streamWriter.WriteLine("}");

            if (!string.IsNullOrEmpty(namespaceName))
                streamWriter.WriteLine("}");

            AssetDatabase.Refresh();
        }
    }
}
#endif
