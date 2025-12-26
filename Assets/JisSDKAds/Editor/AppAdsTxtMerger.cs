#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using System.Linq;

public class AppAdsTxtMergerWindow : OdinEditorWindow
{
    [MenuItem("Tools/App-Ads.txt/Merger (Odin)")]
    private static void OpenWindow()
    {
        GetWindow<AppAdsTxtMergerWindow>("App-Ads.txt Merger");
    }

    // =============================
    // INPUT
    // =============================

    [TitleGroup("Input Files")]
    [InfoBox("Add multiple app-ads.txt files to merge")]
    [ListDrawerSettings(Expanded = true, DraggableItems = false)]
    [Sirenix.OdinInspector.FilePath(Extensions = "txt", AbsolutePath = true)]
    public List<string> inputFiles = new();

    // =============================
    // OPTIONS
    // =============================

    [TitleGroup("Options")]
    [ToggleLeft] public bool removeComments = true;
    [ToggleLeft] public bool ignoreCase = true;
    [ToggleLeft] public bool trimSpaces = true;
    [ToggleLeft] public bool sortAlphabetically = true;

    // =============================
    // PREVIEW
    // =============================

    [TitleGroup("Preview")]
    [ReadOnly, MultiLineProperty(15)]
    public string previewResult;

    // =============================
    // ACTIONS
    // =============================

    [TitleGroup("Actions")]
    [Button(ButtonSizes.Large), GUIColor(0.4f, 0.9f, 0.4f)]
    private void MergeAndPreview()
    {
        var comparer = ignoreCase
            ? System.StringComparer.OrdinalIgnoreCase
            : System.StringComparer.Ordinal;

        HashSet<string> uniqueLines = new(comparer);

        foreach (string file in inputFiles)
        {
            if (!File.Exists(file))
                continue;

            foreach (string rawLine in File.ReadAllLines(file))
            {
                string line = trimSpaces ? rawLine.Trim() : rawLine;

                if (string.IsNullOrEmpty(line))
                    continue;

                if (removeComments && line.StartsWith("#"))
                    continue;

                uniqueLines.Add(line);
            }
        }

        IEnumerable<string> result = uniqueLines;

        if (sortAlphabetically)
            result = result.OrderBy(l => l);

        StringBuilder sb = new();
        sb.AppendLine("# Auto merged app-ads.txt");
        sb.AppendLine($"# Files: {inputFiles.Count}");
        sb.AppendLine($"# Unique lines: {uniqueLines.Count}");
        sb.AppendLine($"# Generated: {System.DateTime.Now}");
        sb.AppendLine();

        foreach (string line in result)
            sb.AppendLine(line);

        previewResult = sb.ToString();
    }

    [Button(ButtonSizes.Large), GUIColor(0.3f, 0.6f, 1f)]
    private void SaveMergedFile()
    {
        if (string.IsNullOrEmpty(previewResult))
        {
            EditorUtility.DisplayDialog(
                "Nothing to save",
                "Please merge files first.",
                "OK"
            );
            return;
        }

        string path = EditorUtility.SaveFilePanel(
            "Save merged app-ads.txt",
            "",
            "app-ads-merged",
            "txt"
        );

        if (string.IsNullOrEmpty(path))
            return;

        File.WriteAllText(path, previewResult, Encoding.UTF8);

        EditorUtility.DisplayDialog(
            "Success",
            $"Merged app-ads.txt saved to:\n{path}",
            "OK"
        );
    }

    [Button(ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.5f)]
    private void ClearAll()
    {
        inputFiles.Clear();
        previewResult = string.Empty;
    }
}
#endif
