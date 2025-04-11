using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class MeldAppAdsManager : MonoBehaviour
{
    public TextAsset MainAppAds;
    public TextAsset SubAppAds;

    [Button]
    public void Meld()
    {
        string mainAppAdsContent = MainAppAds.text;
        string subAppAdsContent = SubAppAds.text;
        // Add all line which not exist in mainAppAdsContent into mainAppAdsContent
        string[] subAppAdsLines = subAppAdsContent.Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        string addedLines = "";
        foreach (string line in subAppAdsLines)
        {
            if (!mainAppAdsContent.Contains(line))
            {
                mainAppAdsContent += line + "\n";
                addedLines += line + "\n";
            }
        }
        Debug.Log("Found " + addedLines.Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries).Length + " lines to add.\n" + addedLines);
        // Save mainAppAdsContent to MainAppAds
        string pathToMainAppAds = UnityEditor.AssetDatabase.GetAssetPath(MainAppAds);
        System.IO.File.WriteAllText(pathToMainAppAds, mainAppAdsContent);
    }

    [Button]
    public void FindAndRemoveSameLineInMainAppAds()
    {
        // Find all line in MainAppAds which is duplicate in MainAppAds
        string mainAppAdsContent = MainAppAds.text;
        string[] mainAppAdsLines = mainAppAdsContent.Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        List<string> newLines = new List<string>();
        foreach (string line in mainAppAdsLines)
        {
            if (!newLines.Contains(line))
            {
                newLines.Add(line);
            }
        }
        int removedLinesCount = mainAppAdsLines.Length - newLines.Count;
        string newAppAdsContent = string.Join("\n", newLines);
        Debug.Log("Found " + removedLinesCount + " lines to remove.\n" + newAppAdsContent);
        
        
    }
}
