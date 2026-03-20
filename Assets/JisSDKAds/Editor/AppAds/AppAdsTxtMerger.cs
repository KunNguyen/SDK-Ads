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

    [TitleGroup("Options")]
    [ToggleLeft, LabelText("Auto-fix structure (trim, normalize commas, uppercase relationship)")]
    public bool autoFixStructure = true;

    [TitleGroup("Options")]
    [ToggleLeft, LabelText("Strict validation (must be 3 or 4 comma-separated fields)")]
    public bool strictValidation = true;

    // =============================
    // PREVIEW
    // =============================

    [TitleGroup("Preview (Merged Output)")]
    [ReadOnly, MultiLineProperty(15)]
    public string previewResult;

    [TitleGroup("Preview (Report)")]
    [ReadOnly, MultiLineProperty(12)]
    public string previewReport;

    // Keep last generated contents so Save can write both files
    private string _lastMergedText;
    private string _lastReportText;

    // =============================
    // HELPERS
    // =============================

    private static bool TryNormalizeAppAdsLine(
        string input,
        bool trim,
        bool strict,
        out string normalized,
        out string reason,
        out bool wasFixed)
    {
        normalized = input;
        reason = null;
        wasFixed = false;

        if (string.IsNullOrWhiteSpace(input))
        {
            reason = "Empty line";
            return false;
        }

        string line = trim ? input.Trim() : input;

        if (line.StartsWith("#"))
        {
            reason = "Comment";
            return false;
        }

        var parts = line.Split(',')
                        .Select(p => trim ? p.Trim() : p)
                        .ToArray();

        if (strict && (parts.Length < 3 || parts.Length > 4))
        {
            reason = $"Wrong field count: {parts.Length} (expected 3 or 4)";
            return false;
        }

        if (parts.Length >= 3 && !string.IsNullOrEmpty(parts[2]))
        {
            string rel = parts[2];
            string upper = rel.ToUpperInvariant();
            if (!string.Equals(rel, upper, StringComparison.Ordinal))
            {
                parts[2] = upper;
                wasFixed = true;
            }
        }

        string rebuilt = string.Join(", ", parts);

        if (!string.Equals(line, rebuilt, StringComparison.Ordinal))
        {
            wasFixed = true;
        }

        normalized = rebuilt;

        if (parts.Length >= 3)
        {
            if (string.IsNullOrWhiteSpace(parts[0]) ||
                string.IsNullOrWhiteSpace(parts[1]) ||
                string.IsNullOrWhiteSpace(parts[2]))
            {
                reason = "One of required fields (domain, publisher id, relationship) is empty";
                return false;
            }
        }

        return true;
    }

    private static string BuildReportPath(string mergedPath)
    {
        string dir = Path.GetDirectoryName(mergedPath);
        string name = Path.GetFileNameWithoutExtension(mergedPath);
        return Path.Combine(dir, $"{name}.report.txt");
    }

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

        int totalDataLines = 0;
        int duplicateLines = 0;
        int invalidStructureLines = 0;
        int fixedLines = 0;

        const int MaxSamples = 20;
        List<string> invalidSamples = new();
        List<string> duplicateSamples = new();
        List<string> missingFiles = new();

        foreach (string file in inputFiles)
        {
            if (!File.Exists(file))
            {
                if (!string.IsNullOrEmpty(file))
                    missingFiles.Add(file);
                continue;
            }

            foreach (string rawLine in File.ReadAllLines(file))
            {
                string line = trimSpaces ? rawLine.Trim() : rawLine;

                if (string.IsNullOrEmpty(line))
                    continue;

                if (removeComments && line.StartsWith("#"))
                    continue;

                bool wasFixed;
                if (!TryNormalizeAppAdsLine(
                        line,
                        trim: trimSpaces,
                        strict: strictValidation,
                        out string normalized,
                        out string reason,
                        out wasFixed))
                {
                    if (reason != "Comment")
                    {
                        invalidStructureLines++;
                        if (invalidSamples.Count < MaxSamples)
                            invalidSamples.Add($"[{Path.GetFileName(file)}] {line}  =>  {reason}");
                    }
                    continue;
                }

                totalDataLines++;

                if (autoFixStructure && wasFixed)
                    fixedLines++;

                if (!uniqueLines.Add(normalized))
                {
                    duplicateLines++;
                    if (duplicateSamples.Count < MaxSamples)
                        duplicateSamples.Add($"[{Path.GetFileName(file)}] {normalized}");
                }
            }
        }

        IEnumerable<string> result = uniqueLines;
        if (sortAlphabetically)
            result = result.OrderBy(l => l);

        DateTime now = System.DateTime.Now;

        // --- Merged output: keep it clean (only summary + lines) ---
        StringBuilder merged = new();
        merged.AppendLine("# Auto merged app-ads.txt");
        merged.AppendLine($"# Files: {inputFiles.Count}");
        merged.AppendLine($"# Total valid lines: {totalDataLines}");
        merged.AppendLine($"# Unique lines: {uniqueLines.Count}");
        merged.AppendLine($"# Generated: {now}");
        merged.AppendLine();
        foreach (string l in result)
            merged.AppendLine(l);

        // --- Report output: all details, samples, options ---
        StringBuilder report = new();
        report.AppendLine("App-Ads.txt Merge Report");
        report.AppendLine($"Generated: {now}");
        report.AppendLine();
        report.AppendLine("Summary");
        report.AppendLine($"- Input files (listed): {inputFiles.Count}");
        report.AppendLine($"- Total valid data lines (after normalization): {totalDataLines}");
        report.AppendLine($"- Unique lines: {uniqueLines.Count}");
        report.AppendLine($"- Duplicate lines skipped: {duplicateLines}");
        report.AppendLine($"- Invalid structure lines skipped: {invalidStructureLines}");
        report.AppendLine($"- Lines auto-fixed (normalized): {fixedLines}");
        report.AppendLine();
        report.AppendLine("Options");
        report.AppendLine($"- removeComments: {removeComments}");
        report.AppendLine($"- ignoreCase: {ignoreCase}");
        report.AppendLine($"- trimSpaces: {trimSpaces}");
        report.AppendLine($"- sortAlphabetically: {sortAlphabetically}");
        report.AppendLine($"- autoFixStructure: {autoFixStructure}");
        report.AppendLine($"- strictValidation: {strictValidation}");
        report.AppendLine();

        if (missingFiles.Count > 0)
        {
            report.AppendLine("Missing files");
            foreach (var f in missingFiles)
                report.AppendLine($"- {f}");
            report.AppendLine();
        }

        if (duplicateSamples.Count > 0)
        {
            report.AppendLine($"Duplicate samples (first {duplicateSamples.Count})");
            foreach (var s in duplicateSamples)
                report.AppendLine($"- {s}");
            report.AppendLine();
        }

        if (invalidSamples.Count > 0)
        {
            report.AppendLine($"Invalid structure samples (first {invalidSamples.Count})");
            foreach (var s in invalidSamples)
                report.AppendLine($"- {s}");
            report.AppendLine();
        }

        _lastMergedText = merged.ToString();
        _lastReportText = report.ToString();

        previewResult = _lastMergedText;
        previewReport = _lastReportText;
    }

    [Button(ButtonSizes.Large), GUIColor(0.3f, 0.6f, 1f)]
    private void SaveMergedFile()
    {
        if (string.IsNullOrEmpty(_lastMergedText))
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

        File.WriteAllText(path, _lastMergedText, Encoding.UTF8);

        string reportPath = BuildReportPath(path);
        if (!string.IsNullOrEmpty(_lastReportText))
            File.WriteAllText(reportPath, _lastReportText, Encoding.UTF8);

        EditorUtility.DisplayDialog(
            "Success",
            $"Merged saved to:\n{path}\n\nReport saved to:\n{reportPath}",
            "OK"
        );
    }

    [Button(ButtonSizes.Medium), GUIColor(1f, 0.5f, 0.5f)]
    private void ClearAll()
    {
        inputFiles.Clear();
        previewResult = string.Empty;
        previewReport = string.Empty;
        _lastMergedText = string.Empty;
        _lastReportText = string.Empty;
    }
}
#endif