#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace JisSDKAds.Hub
{
    internal static class JisSDKHubManifest
    {
        public static string ManifestPath =>
            Path.Combine(Path.GetDirectoryName(UnityEngine.Application.dataPath), "Packages", "manifest.json");

        public static bool HasDependency(string packageName)
        {
            if (!File.Exists(ManifestPath)) return false;
            return File.ReadAllText(ManifestPath).Contains($"\"{packageName}\"");
        }

        public static void AddDependency(string packageName, string versionOrPath)
        {
            var json = File.ReadAllText(ManifestPath);
            if (json.Contains($"\"{packageName}\""))
            {
                json = Regex.Replace(
                    json,
                    $"\"{Regex.Escape(packageName)}\"\\s*:\\s*\"[^\"]*\"",
                    $"\"{packageName}\": \"{versionOrPath}\"");
            }
            else
            {
                var insert = $"        \"{packageName}\": \"{versionOrPath}\",\n";
                json = json.Replace("\"dependencies\": {\n", "\"dependencies\": {\n" + insert);
            }
            File.WriteAllText(ManifestPath, json);
        }

        public static void EnsureScopedRegistry(string name, string url, IReadOnlyList<string> scopes)
        {
            var json = File.ReadAllText(ManifestPath);
            if (json.Contains($"\"name\": \"{name}\""))
                return;

            var scopesJson = string.Join(",\n                ", scopes.Select(s => $"\"{s}\""));
            var block = $@"        {{
            ""name"": ""{name}"",
            ""url"": ""{url}"",
            ""scopes"": [
                {scopesJson}
            ]
        }}";

            if (!json.Contains("\"scopedRegistries\""))
            {
                json = json.TrimEnd().TrimEnd('}') + ",\n    \"scopedRegistries\": [\n" + block + "\n    ]\n}\n";
            }
            else
            {
                json = json.Replace(
                    "\"scopedRegistries\": [",
                    "\"scopedRegistries\": [\n" + block + ",");
            }
            File.WriteAllText(ManifestPath, json);
        }

        /// <summary>
        /// Updates #revision on all com.jis.sdkads.* Git UPM entries (e.g. #4.0.0 → #main when tag missing).
        /// </summary>
        public static int UpdateJisSdkGitRevisions(string revision)
        {
            if (!File.Exists(ManifestPath)) return 0;
            revision = (revision ?? "main").Trim().TrimStart('#');
            if (string.IsNullOrEmpty(revision)) revision = "main";

            var json = File.ReadAllText(ManifestPath);
            var pattern = new Regex(
                @"""(com\.jis\.sdkads\.[^""]+)""\s*:\s*""(https?://[^""]+\?path=Packages/[^""]+)#([^""]*)""",
                RegexOptions.Multiline);
            var count = 0;
            json = pattern.Replace(json, m =>
            {
                if (m.Groups[3].Value == revision) return m.Value;
                count++;
                return $"\"{m.Groups[1].Value}\": \"{m.Groups[2].Value}#{revision}\"";
            });
            if (count > 0)
                File.WriteAllText(ManifestPath, json);
            return count;
        }
    }
}
#endif
