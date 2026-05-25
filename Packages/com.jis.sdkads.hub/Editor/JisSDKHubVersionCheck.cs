#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace JisSDKAds.Hub
{
    internal enum JisPackageUpdateStatus
    {
        NotChecked,
        UpToDate,
        UpdateAvailable,
        RevisionMismatch,
        NotInstalled,
        FetchFailed,
        EmbeddedOnly
    }

    internal sealed class JisPackageVersionRow
    {
        public string PackageId;
        public string InstalledVersion;
        public string RemoteVersion;
        public string ManifestSource;
        public string ManifestRevision;
        public JisPackageUpdateStatus Status;
        public string Note;
    }

    /// <summary>
    /// Compares installed com.jis.sdkads.* versions (PackageCache / embedded) with package.json on Git remote.
    /// </summary>
    internal static class JisSDKHubVersionCheck
    {
        const string PrefsLastCheckUtc = "JisSDKAds.Hub.VersionCheck.LastUtc";

        static readonly Regex VersionRegex = new(
            @"""version""\s*:\s*""([^""]+)""",
            RegexOptions.Compiled);

        static readonly Regex ManifestJisDepRegex = new(
            @"""(com\.jis\.sdkads\.[^""]+)""\s*:\s*""([^""]+)""",
            RegexOptions.Compiled);

        static readonly Regex GitHubRepoRegex = new(
            @"github\.com[:/](?<owner>[^/]+)/(?<repo>[^/.]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static IReadOnlyList<JisPackageVersionRow> LastResults { get; private set; } =
            Array.Empty<JisPackageVersionRow>();

        public static DateTime? LastCheckUtc
        {
            get
            {
                var ticks = long.Parse(EditorPrefs.GetString(PrefsLastCheckUtc, "0"));
                return ticks > 0 ? new DateTime(ticks, DateTimeKind.Utc) : null;
            }
            private set => EditorPrefs.SetString(PrefsLastCheckUtc, value?.Ticks.ToString() ?? "0");
        }

        public static int UpdateAvailableCount =>
            LastResults.Count(r => r.Status == JisPackageUpdateStatus.UpdateAvailable);

        public static void RunCheck(string gitBaseUrl, string targetRevision, bool includeNotInManifest = false)
        {
            gitBaseUrl = NormalizeGitBaseUrl(gitBaseUrl);
            targetRevision = NormalizeRevision(targetRevision);

            var rows = new List<JisPackageVersionRow>();
            var manifestDeps = GetJisManifestDependencies();
            var manifestIds = new HashSet<string>(manifestDeps.Select(d => d.id));

            var idsToCheck = includeNotInManifest
                ? GetAllKnownJisPackageIds().ToList()
                : manifestDeps.Select(d => d.id).Distinct().OrderBy(x => x).ToList();

            try
            {
                for (var i = 0; i < idsToCheck.Count; i++)
                {
                    var id = idsToCheck[i];
                    EditorUtility.DisplayProgressBar(
                        "JIS SDK Hub — Version check",
                        id,
                        idsToCheck.Count == 0 ? 1f : (float)i / idsToCheck.Count);

                    var manifestEntry = manifestDeps.FirstOrDefault(d => d.id == id);
                    rows.Add(BuildRow(id, manifestEntry.source, string.IsNullOrEmpty(manifestEntry.id), gitBaseUrl, targetRevision, manifestIds));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            LastResults = rows;
            LastCheckUtc = DateTime.UtcNow;
        }

        static JisPackageVersionRow BuildRow(
            string packageId,
            string manifestSource,
            bool notInManifest,
            string gitBaseUrl,
            string targetRevision,
            HashSet<string> manifestIds)
        {
            var row = new JisPackageVersionRow
            {
                PackageId = packageId,
                ManifestSource = manifestSource ?? "",
                ManifestRevision = ExtractGitRevision(manifestSource)
            };

            if (notInManifest || !manifestIds.Contains(packageId))
            {
                row.Status = JisPackageUpdateStatus.NotInstalled;
                row.Note = "Not in manifest";
                return row;
            }

            if (row.ManifestSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                row.InstalledVersion = ReadVersionFromEmbedded(packageId);
                row.RemoteVersion = FetchRemoteVersion(gitBaseUrl, targetRevision, packageId);
                row.Status = CompareVersions(row.InstalledVersion, row.RemoteVersion);
                if (row.Status == JisPackageUpdateStatus.FetchFailed)
                    row.Note = "Could not read remote (network or private repo)";
                else if (row.Status == JisPackageUpdateStatus.UpdateAvailable)
                    row.Note = "Embedded / file: differs from remote";
                else
                    row.Note = "Embedded / file: package";
                return row;
            }

            row.InstalledVersion = ReadInstalledVersion(packageId);
            row.RemoteVersion = FetchRemoteVersion(gitBaseUrl, targetRevision, packageId);

            if (string.IsNullOrEmpty(row.InstalledVersion) && string.IsNullOrEmpty(row.RemoteVersion))
            {
                row.Status = JisPackageUpdateStatus.FetchFailed;
                row.Note = "Resolve packages in Package Manager first";
                return row;
            }

            var versionStatus = CompareVersions(row.InstalledVersion, row.RemoteVersion);
            var revisionMismatch = !string.IsNullOrEmpty(row.ManifestRevision) &&
                                   !string.Equals(row.ManifestRevision, targetRevision, StringComparison.OrdinalIgnoreCase);

            if (versionStatus == JisPackageUpdateStatus.UpdateAvailable)
            {
                row.Status = JisPackageUpdateStatus.UpdateAvailable;
                row.Note = revisionMismatch
                    ? $"Remote {row.RemoteVersion}; manifest pinned to #{row.ManifestRevision}"
                    : "Newer version on remote — update Git revision and Resolve packages";
            }
            else if (revisionMismatch)
            {
                row.Status = JisPackageUpdateStatus.RevisionMismatch;
                row.Note = $"Manifest #{row.ManifestRevision} → Hub uses #{targetRevision}";
            }
            else if (versionStatus == JisPackageUpdateStatus.FetchFailed)
            {
                row.Status = JisPackageUpdateStatus.FetchFailed;
                row.Note = "Remote fetch failed";
            }
            else
            {
                row.Status = JisPackageUpdateStatus.UpToDate;
                row.Note = "Up to date";
            }

            return row;
        }

        static JisPackageUpdateStatus CompareVersions(string installed, string remote)
        {
            if (string.IsNullOrEmpty(remote))
                return JisPackageUpdateStatus.FetchFailed;
            if (string.IsNullOrEmpty(installed))
                return JisPackageUpdateStatus.UpdateAvailable;

            if (TryParseVersion(installed, out var a) && TryParseVersion(remote, out var b))
                return b > a ? JisPackageUpdateStatus.UpdateAvailable : JisPackageUpdateStatus.UpToDate;

            return string.Equals(installed, remote, StringComparison.OrdinalIgnoreCase)
                ? JisPackageUpdateStatus.UpToDate
                : JisPackageUpdateStatus.UpdateAvailable;
        }

        static bool TryParseVersion(string text, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(text)) return false;
            text = text.Trim();
            if (Version.TryParse(text, out version)) return true;
            var digits = Regex.Match(text, @"(\d+\.\d+\.\d+)");
            return digits.Success && Version.TryParse(digits.Groups[1].Value, out version);
        }

        public static List<(string id, string source)> GetJisManifestDependencies()
        {
            var list = new List<(string id, string source)>();
            if (!File.Exists(JisSDKHubManifest.ManifestPath))
                return list;

            foreach (Match m in ManifestJisDepRegex.Matches(File.ReadAllText(JisSDKHubManifest.ManifestPath)))
                list.Add((m.Groups[1].Value, m.Groups[2].Value));

            return list;
        }

        static IEnumerable<string> GetAllKnownJisPackageIds()
        {
            var ids = new HashSet<string>();
            foreach (var kind in JisSDKHubModules.All)
            {
                foreach (var (id, _) in JisSDKHubModules.GetPackages(kind))
                    ids.Add(id);
            }

            ids.Add("com.jis.sdkads.hub");
            return ids.OrderBy(x => x);
        }

        static string ReadInstalledVersion(string packageId)
        {
            var fromCache = ReadVersionFromPackageCache(packageId);
            if (!string.IsNullOrEmpty(fromCache))
                return fromCache;
            return ReadVersionFromEmbedded(packageId);
        }

        static string ReadVersionFromPackageCache(string packageId)
        {
            var cacheRoot = Path.Combine(GetProjectRoot(), "Library", "PackageCache");
            if (!Directory.Exists(cacheRoot)) return null;

            foreach (var dir in Directory.GetDirectories(cacheRoot, packageId + "@*"))
            {
                var v = ReadPackageJsonVersion(Path.Combine(dir, "package.json"));
                if (!string.IsNullOrEmpty(v)) return v;
            }

            return null;
        }

        static string ReadVersionFromEmbedded(string packageId)
        {
            var path = Path.Combine(JisSDKHubManifest.PackagesRoot, packageId, "package.json");
            return File.Exists(path) ? ReadPackageJsonVersion(path) : null;
        }

        static string ReadPackageJsonVersion(string path)
        {
            if (!File.Exists(path)) return null;
            var m = VersionRegex.Match(File.ReadAllText(path));
            return m.Success ? m.Groups[1].Value : null;
        }

        static string FetchRemoteVersion(string gitBaseUrl, string revision, string packageId)
        {
            if (!TryParseGitHubRepo(gitBaseUrl, out var owner, out var repo))
                return null;

            var url =
                $"https://raw.githubusercontent.com/{owner}/{repo}/{revision}/Packages/{packageId}/package.json";

            try
            {
                using var request = UnityWebRequest.Get(url);
                request.timeout = 12;
                var op = request.SendWebRequest();
                while (!op.isDone) { }

#if UNITY_2020_1_OR_NEWER
                if (request.result != UnityWebRequest.Result.Success)
#else
                if (request.isNetworkError || request.isHttpError)
#endif
                    return null;

                var m = VersionRegex.Match(request.downloadHandler.text);
                return m.Success ? m.Groups[1].Value : null;
            }
            catch
            {
                return null;
            }
        }

        static bool TryParseGitHubRepo(string gitBaseUrl, out string owner, out string repo)
        {
            owner = null;
            repo = null;
            var m = GitHubRepoRegex.Match(gitBaseUrl ?? "");
            if (!m.Success) return false;
            owner = m.Groups["owner"].Value;
            repo = m.Groups["repo"].Value;
            return true;
        }

        static string ExtractGitRevision(string manifestSource)
        {
            if (string.IsNullOrEmpty(manifestSource)) return "";
            var hash = manifestSource.LastIndexOf('#');
            if (hash < 0 || hash >= manifestSource.Length - 1) return "";
            return manifestSource.Substring(hash + 1).Trim();
        }

        static string NormalizeGitBaseUrl(string url)
        {
            url = (url ?? "https://github.com/KunNguyen/SDK-Ads.git").Trim().TrimEnd('/');
            if (url.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                url = url.Substring(0, url.Length - 4);
            return url;
        }

        static string NormalizeRevision(string revision)
        {
            revision = (revision ?? "main").Trim().TrimStart('#');
            return string.IsNullOrEmpty(revision) ? "main" : revision;
        }

        static string GetProjectRoot() => Path.GetDirectoryName(Application.dataPath);
    }
}
#endif
