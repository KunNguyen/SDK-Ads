using System.Collections;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace SDK
{
    [CreateAssetMenu(fileName = "RewardAdsPlacementConfig", menuName = "SDK/RewardAdsPlacementConfig")]
    public class RewardAdsPlacementConfig : ScriptableObject
    {
        public List<string> placementIds = new List<string>();

#if UNITY_EDITOR
        [Button]
        public void GenerateAdsPlacementIDs()
        {
            string filePathAndName = "Assets/ABIMaxSDKAds/Scripts/Ads/"; //The folder Scripts/Enums/ is expected to exist
            Generate("WatchVideoRewardType", filePathAndName, placementIds.ToArray(), "SDK");
        }
        private static void Generate(string enumName, string path, string[] enumEntries, string namespaceName = "")
        {
            string filePathAndName = path + enumName + ".cs"; //The folder Scripts/Enums/ is expected to exist

            using (StreamWriter streamWriter = new StreamWriter(filePathAndName))
            {
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    streamWriter.WriteLine("namespace " + namespaceName);
                    
                    streamWriter.WriteLine("{");
                }
                streamWriter.WriteLine("public enum " + enumName);
                streamWriter.WriteLine("{");
                foreach (string t in enumEntries)
                {
                    streamWriter.WriteLine("	" + t + ",");
                }
                streamWriter.WriteLine("}");
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    streamWriter.WriteLine("}");
                }
            }
            AssetDatabase.Refresh();
        }
#endif
    }
}