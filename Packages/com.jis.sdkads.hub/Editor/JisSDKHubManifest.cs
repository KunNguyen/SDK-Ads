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
    }
}
#endif
