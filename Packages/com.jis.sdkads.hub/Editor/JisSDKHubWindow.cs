#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace JisSDKAds.Hub
{
    public class JisSDKHubWindow : EditorWindow
    {
        private const string PrefsGitBaseUrl = "JisSDKAds.Hub.GitBaseUrl";
        private const string PrefsGitRevision = "JisSDKAds.Hub.GitRevision";
        private const string DefaultGitBaseUrl = "https://github.com/KunNguyen/SDK-Ads.git";
        private const string DefaultGitRevision = "main";

        private string _gitBaseUrl;
        private string _gitRevision;
        private Vector2 _scroll;
        private bool _useEmbeddedPackages;

        [MenuItem("JIS SDK/Hub")]
        public static void Open() => GetWindow<JisSDKHubWindow>("JIS SDK Hub");

        private void OnEnable()
        {
            _gitBaseUrl = EditorPrefs.GetString(PrefsGitBaseUrl, DefaultGitBaseUrl);
            _gitRevision = EditorPrefs.GetString(PrefsGitRevision, DefaultGitRevision);
            _useEmbeddedPackages = JisSDKHubManifest.IsSdkAdsDevRepo();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("JIS SDK Hub", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Import or remove SDK modules. Status is read from Packages/manifest.json.\n" +
                "• Install Firebase from Google before using Firebase module.\n" +
                "• Game projects: disable embedded packages; use Git revision main.",
                MessageType.Info);

            _useEmbeddedPackages = EditorGUILayout.Toggle("Use embedded packages (SDK-Ads dev repo)", _useEmbeddedPackages);
            if (_useEmbeddedPackages && !JisSDKHubManifest.IsSdkAdsDevRepo())
            {
                EditorGUILayout.HelpBox(
                    "Embedded mode needs full packages under Packages/ (dev repo).\n" +
                    "Game projects: turn OFF — Hub will use Git URLs.",
                    MessageType.Warning);
            }

            if (!_useEmbeddedPackages)
                DrawGitSettings();

            EditorGUILayout.Space(6);
            DrawModule("Firebase (required)", "core + common + firebase + hub", ModuleKind.Firebase);
            DrawModule("Ads", "Providers MAX/AdMob + full ads runtime", ModuleKind.Ads);
            DrawModule("IAP", "Unity Purchasing", ModuleKind.Iap);
            DrawModule("App Review", "Android only — Google Play Review", ModuleKind.AppReview);
            DrawModule("AppsFlyer", "Optional", ModuleKind.AppsFlyer);
            DrawModule("SolarEngine", "Optional", ModuleKind.SolarEngine);
            DrawModule("Facebook", "Optional", ModuleKind.Facebook);
            DrawModule("Editor Tools", "SDK setup & build", ModuleKind.Editor);
            EditorGUILayout.EndScrollView();
        }

        private void DrawGitSettings()
        {
            _gitBaseUrl = EditorGUILayout.TextField("Git UPM base URL", _gitBaseUrl);
            _gitRevision = EditorGUILayout.TextField("Git revision (branch or tag)", _gitRevision);
            EditorGUILayout.HelpBox(
                "Use main until a Git tag exists. Hub appends #revision to package URLs.",
                MessageType.None);
            if (GUILayout.Button("Save Git URL & revision"))
            {
                EditorPrefs.SetString(PrefsGitBaseUrl, _gitBaseUrl);
                EditorPrefs.SetString(PrefsGitRevision, _gitRevision);
            }

            if (GUILayout.Button("Fix com.jis.sdkads.* revisions in manifest.json"))
            {
                var n = JisSDKHubManifest.UpdateJisSdkGitRevisions(_gitRevision);
                RefreshPackages();
                EditorUtility.DisplayDialog("JIS SDK Hub",
                    n > 0 ? $"Updated {n} package(s)." : "No com.jis.sdkads.* Git entries to update.", "OK");
            }

            if (GUILayout.Button("Fix broken file: → Git URLs in manifest.json"))
            {
                var n = JisSDKHubManifest.MigrateBrokenFileRefsToGit(_gitBaseUrl, _gitRevision);
                RefreshPackages();
                EditorUtility.DisplayDialog("JIS SDK Hub",
                    n > 0 ? $"Converted {n} broken file: entries." : "No broken file: entries found.", "OK");
            }
        }

        private void DrawModule(string title, string desc, ModuleKind kind)
        {
            var status = JisSDKHubModules.GetStatus(kind);
            var statusLabel = JisSDKHubModules.GetStatusLabel(kind);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(desc, EditorStyles.miniLabel);

            var prevColor = GUI.contentColor;
            GUI.contentColor = status switch
            {
                ModuleInstallStatus.Installed => new Color(0.5f, 0.95f, 0.55f),
                ModuleInstallStatus.Partial => new Color(1f, 0.85f, 0.4f),
                _ => new Color(0.75f, 0.75f, 0.75f)
            };
            EditorGUILayout.LabelField($"Status: {statusLabel}", EditorStyles.miniBoldLabel);
            GUI.contentColor = prevColor;

            EditorGUILayout.BeginHorizontal();
            var showImport = status != ModuleInstallStatus.Installed;
            var showRemove = status != ModuleInstallStatus.NotInstalled;
            JisSDKHubModules.CanRemove(kind, out var blockReason);

            GUI.enabled = showImport;
            if (GUILayout.Button($"Import {title}", GUILayout.Height(22)))
                Import(kind);
            GUI.enabled = true;

            GUI.enabled = showRemove && string.IsNullOrEmpty(blockReason);
            if (GUILayout.Button($"Remove {title}", GUILayout.Height(22)))
                Remove(kind);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (showRemove && !string.IsNullOrEmpty(blockReason))
                EditorGUILayout.HelpBox(blockReason, MessageType.Warning);

            if (kind == ModuleKind.Firebase && status == ModuleInstallStatus.Installed)
                EditorGUILayout.LabelField("Remove keeps com.jis.sdkads.hub so you can re-import modules.", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        private void Import(ModuleKind kind)
        {
            var migrated = JisSDKHubManifest.MigrateBrokenFileRefsToGit(_gitBaseUrl, _gitRevision);
            if (migrated > 0)
                Debug.Log($"[JIS SDK Hub] Migrated {migrated} file: dependency(ies) to Git URLs.");

            if (!_useEmbeddedPackages)
            {
                JisSDKHubManifest.UpdateJisSdkGitRevisions(_gitRevision);
                EnsureRegistriesForImport(kind);
            }

            foreach (var (id, folder) in JisSDKHubModules.GetPackages(kind))
                JisSDKHubManifest.AddDependency(id, ResolveSource(folder));

            foreach (var (id, ver) in GetExternalForImport(kind))
                JisSDKHubManifest.AddDependency(id, ver);

            ApplyDefines(kind, add: true);
            RefreshPackages();

            if (kind == ModuleKind.Ads)
            {
                EditorApplication.delayCall += () =>
                {
                    JisSDKHubProjectSetup.EnsureAdsSettingsAsset();
                    JisSDKHubProjectSetup.EnsurePlatformSdkSetupStubs();
                };
            }

            EditorUtility.DisplayDialog("JIS SDK Hub", $"Imported {JisSDKHubModules.GetTitle(kind)}.", "OK");
            Repaint();
        }

        private void Remove(ModuleKind kind)
        {
            if (!JisSDKHubModules.CanRemove(kind, out var blockReason))
            {
                EditorUtility.DisplayDialog("JIS SDK Hub", blockReason, "OK");
                return;
            }

            var toRemove = JisSDKHubModules.GetPackageIdsToRemove(kind).ToList();
            var external = JisSDKHubModules.GetExternal(kind).Select(e => e.id).ToList();
            var defines = JisSDKHubModules.GetDefines(kind);

            var message =
                $"Remove {JisSDKHubModules.GetTitle(kind)}?\n\nPackages:\n" +
                string.Join("\n", toRemove.Concat(external).Select(id => "  • " + id));
            if (defines.Count > 0)
                message += "\n\nScripting defines:\n" + string.Join(", ", defines);

            if (!EditorUtility.DisplayDialog("JIS SDK Hub — Remove module", message, "Remove", "Cancel"))
                return;

            foreach (var id in toRemove)
                JisSDKHubManifest.RemoveDependency(id);

            foreach (var id in external)
            {
                if (ShouldRemoveExternal(kind, id))
                    JisSDKHubManifest.RemoveDependency(id);
            }

            ApplyDefines(kind, add: false);
            RefreshPackages();

            EditorUtility.DisplayDialog("JIS SDK Hub", $"Removed {JisSDKHubModules.GetTitle(kind)}.", "OK");
            Repaint();
        }

        private static bool ShouldRemoveExternal(ModuleKind kind, string packageId)
        {
            if (!JisSDKHubManifest.HasDependency(packageId)) return false;

            switch (kind)
            {
                case ModuleKind.Ads:
                    return true;
                case ModuleKind.Iap:
                    return true;
                default:
                    return false;
            }
        }

        private static IEnumerable<(string id, string version)> GetExternalForImport(ModuleKind kind)
        {
            foreach (var entry in JisSDKHubModules.GetExternal(kind))
            {
                if (kind == ModuleKind.Ads && entry.id == "com.google.ads.mobile" &&
                    JisSDKHubManifest.HasDependency("com.google.ads.mobile"))
                    continue;
                yield return entry;
            }
        }

        private void EnsureRegistriesForImport(ModuleKind kind)
        {
            if (kind == ModuleKind.Ads)
            {
                JisSDKHubManifest.EnsureScopedRegistry(
                    "AppLovin MAX Unity", "https://unity.packages.applovin.com/",
                    new[] { "com.applovin.mediation.ads", "com.applovin.mediation.adapters", "com.applovin.mediation.dsp" });
                JisSDKHubManifest.EnsureScopedRegistry(
                    "Game Package Registry by Google", "https://unityregistry-pa.googleapis.com",
                    new[] { "com.google" });
                JisSDKHubManifest.EnsureScopedRegistry(
                    "package.openupm.com", "https://package.openupm.com",
                    new[] { "com.google.ads.mobile" });
            }
        }

        private static void RefreshPackages()
        {
            AssetDatabase.Refresh();
            Client.Resolve();
        }

        private string ResolveSource(string folder)
        {
            if (_useEmbeddedPackages && JisSDKHubManifest.HasEmbeddedPackage(folder))
                return $"file:{folder}";

            if (_useEmbeddedPackages && !JisSDKHubManifest.HasEmbeddedPackage(folder))
                Debug.LogWarning($"[JIS SDK Hub] Missing Packages/{folder}/package.json — using Git URL.");

            var revision = string.IsNullOrWhiteSpace(_gitRevision) ? DefaultGitRevision : _gitRevision.Trim().TrimStart('#');
            return $"{_gitBaseUrl.TrimEnd('/')}?path=Packages/{folder}#{revision}";
        }

        private static void ApplyDefines(ModuleKind kind, bool add)
        {
            var symbols = JisSDKHubModules.GetDefines(kind);
            if (symbols.Count == 0) return;

            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            var set = new HashSet<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(group)
                .Split(';').Where(s => !string.IsNullOrEmpty(s)));

            foreach (var symbol in symbols)
            {
                if (add) set.Add(symbol);
                else set.Remove(symbol);
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", set));
        }
    }
}
#endif
