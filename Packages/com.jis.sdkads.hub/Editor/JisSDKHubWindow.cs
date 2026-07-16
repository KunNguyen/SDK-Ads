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
        private const string PrefsUseEmbedded = "JisSDKAds.Hub.UseEmbeddedPackages";
        public const string DefaultGitBaseUrl = "https://github.com/KunNguyen/SDK-Ads.git";
        public const string DefaultGitRevision = "main";

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
            _useEmbeddedPackages = EditorPrefs.HasKey(PrefsUseEmbedded)
                ? EditorPrefs.GetBool(PrefsUseEmbedded)
                : JisSDKHubManifest.IsSdkAdsDevRepo();
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

            EditorGUI.BeginChangeCheck();
            _useEmbeddedPackages = EditorGUILayout.Toggle("Use embedded packages (SDK-Ads dev repo)", _useEmbeddedPackages);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(PrefsUseEmbedded, _useEmbeddedPackages);

            if (_useEmbeddedPackages && !JisSDKHubManifest.IsSdkAdsDevRepo())
            {
                EditorGUILayout.HelpBox(
                    "Embedded mode needs full packages under Packages/ (dev repo).\n" +
                    "Game projects: turn OFF — Hub will use Git URLs.",
                    MessageType.Warning);
            }

            if (!_useEmbeddedPackages)
                DrawGitSettings();
            else
                EditorGUILayout.HelpBox(
                    "Embedded mode: installed versions come from Packages/. Version check still compares to Git remote using saved URL/revision below.",
                    MessageType.None);

            DrawVersionCheck();

            EditorGUILayout.Space(6);
            DrawModule("Firebase (required)", "core + common + firebase + hub + EDM (OpenUPM)", ModuleKind.Firebase);
            DrawAdsModule();
            DrawModule("IAP", "Unity Purchasing", ModuleKind.Iap);
            DrawModule("App Review", "Android only — Google Play Review", ModuleKind.AppReview);
            DrawModule("AppsFlyer", "Optional", ModuleKind.AppsFlyer);
            DrawModule("SolarEngine", "Optional", ModuleKind.SolarEngine);
            DrawModule("Facebook", "Optional", ModuleKind.Facebook);
            DrawModule("Local Notifications", "Daily reminders & gameplay timers", ModuleKind.Notifications);
            DrawModule("Editor Tools", "SDK setup & build", ModuleKind.Editor);
            EditorGUILayout.EndScrollView();
        }

        private void DrawVersionCheck()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Package version check", EditorStyles.boldLabel);

            if (_useEmbeddedPackages)
            {
                _gitBaseUrl = EditorGUILayout.TextField("Git URL (remote compare)", _gitBaseUrl);
                _gitRevision = EditorGUILayout.TextField("Git revision", _gitRevision);
                if (GUILayout.Button("Save Git URL & revision"))
                {
                    EditorPrefs.SetString(PrefsGitBaseUrl, _gitBaseUrl);
                    EditorPrefs.SetString(PrefsGitRevision, _gitRevision);
                }
            }
            EditorGUILayout.HelpBox(
                "Compares installed com.jis.sdkads.* versions with package.json on the Git revision above. " +
                "Use Update / Update all to fix manifest revision, clear PackageCache, and Resolve.",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Check for updates", GUILayout.Height(24), GUILayout.Width(140)))
            {
                JisSDKHubVersionCheck.RunCheck(_gitBaseUrl, _gitRevision);
                Repaint();
            }

            var results = JisSDKHubVersionCheck.LastResults;
            var updatable = results?.Count > 0 == true ? JisSDKHubVersionCheck.UpdatableCount : 0;
            GUI.enabled = updatable > 0;
            if (GUILayout.Button($"Update all ({updatable})", GUILayout.Height(24), GUILayout.Width(140)))
                PerformUpdateAllPackages();
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            var last = JisSDKHubVersionCheck.LastCheckUtc;
            if (last.HasValue)
                EditorGUILayout.LabelField($"Last: {last.Value.ToLocalTime():g}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            if (results == null || results.Count == 0)
                return;

            var updates = JisSDKHubVersionCheck.UpdateAvailableCount;
            var revisionDrift = results.Count(r => r.Status == JisPackageUpdateStatus.RevisionMismatch);
            if (updates > 0)
            {
                EditorGUILayout.HelpBox(
                    $"{updates} package(s) have a newer version on #{_gitRevision.Trim().TrimStart('#')}.",
                    MessageType.Warning);

                if (updatable == 0)
                {
                    EditorGUILayout.HelpBox(
                        "Update buttons are disabled: manifest.json entries are not Git UPM URLs " +
                        "(need https://…github.com/…?path=Packages/…#revision). " +
                        "Registry versions (e.g. \"5.1.0\") or file: paths must be edited manually.",
                        MessageType.Warning);
                }
            }
            else if (revisionDrift > 0)
            {
                EditorGUILayout.HelpBox(
                    $"{revisionDrift} package(s) use a different Git revision in manifest.json.",
                    MessageType.Info);
            }
            else if (results.All(r => r.Status == JisPackageUpdateStatus.UpToDate))
            {
                EditorGUILayout.HelpBox("All checked packages match the remote revision.", MessageType.Info);
            }

            foreach (var row in results.OrderBy(r => r.PackageId))
                DrawVersionRow(row);
        }

        private void DrawVersionRow(JisPackageVersionRow row)
        {
            var shortId = row.PackageId.Replace("com.jis.sdkads.", "");
            var canUpdate = JisSDKHubVersionCheck.CanUpdate(row);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(shortId, EditorStyles.boldLabel, GUILayout.Width(88));

            var installed = string.IsNullOrEmpty(row.InstalledVersion) ? "—" : row.InstalledVersion;
            var remote = string.IsNullOrEmpty(row.RemoteVersion) ? "—" : row.RemoteVersion;
            EditorGUILayout.LabelField($"{installed} → {remote}", GUILayout.MinWidth(72));

            var prev = GUI.contentColor;
            GUI.contentColor = row.Status switch
            {
                JisPackageUpdateStatus.UpdateAvailable => new Color(1f, 0.75f, 0.35f),
                JisPackageUpdateStatus.RevisionMismatch => new Color(0.55f, 0.85f, 1f),
                JisPackageUpdateStatus.UpToDate => new Color(0.55f, 0.95f, 0.6f),
                JisPackageUpdateStatus.FetchFailed => new Color(1f, 0.5f, 0.5f),
                _ => Color.gray
            };
            EditorGUILayout.LabelField(row.Status.ToString(), EditorStyles.miniBoldLabel, GUILayout.Width(108));
            GUI.contentColor = prev;

            GUILayout.FlexibleSpace();

            GUI.enabled = canUpdate;
            if (GUILayout.Button("Update", GUILayout.Width(72), GUILayout.Height(22)))
                PerformUpdatePackage(row.PackageId);
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(row.Note))
            {
                EditorGUILayout.LabelField(
                    new GUIContent(row.Note, row.ManifestSource),
                    EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        void PerformUpdatePackage(string packageId)
        {
            if (JisSDKHubVersionCheck.TryApplyPackageUpdate(packageId, _gitRevision, out var message))
            {
                RefreshPackages();
                JisSDKHubVersionCheck.RunCheck(_gitBaseUrl, _gitRevision);
                Repaint();
                EditorUtility.DisplayDialog("JIS SDK Hub", message, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("JIS SDK Hub", message, "OK");
            }
        }

        void PerformUpdateAllPackages()
        {
            if (!JisSDKHubVersionCheck.TryApplyAllUpdates(_gitRevision, out var message))
            {
                EditorUtility.DisplayDialog("JIS SDK Hub", message, "OK");
                return;
            }

            RefreshPackages();
            JisSDKHubVersionCheck.RunCheck(_gitBaseUrl, _gitRevision);
            Repaint();
            EditorUtility.DisplayDialog("JIS SDK Hub", message, "OK");
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

            JisSDKHubPackageHealth.DrawFlushPackageCacheButton();
            JisSDKHubPackageHealth.DrawOdinMigrationWarning();
            JisSDKHubSolarEngineHealth.DrawSolarEngineSetupWarning();
            JisSDKHubPackageHealth.DrawIapCommonMismatchWarning();
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
                RequestRemoveModule(kind);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (showRemove && !string.IsNullOrEmpty(blockReason))
                EditorGUILayout.HelpBox(blockReason, MessageType.Warning);

            if (kind == ModuleKind.Firebase && status == ModuleInstallStatus.Installed)
                EditorGUILayout.LabelField("Remove keeps com.jis.sdkads.hub so you can re-import modules.", EditorStyles.miniLabel);

            if (kind == ModuleKind.Iap)
                JisSDKHubPackageHealth.DrawIapCommonMismatchWarning();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        private void DrawAdsModule()
        {
            const string title = "Ads";
            const string desc = "Ads runtime (JisAds / AdsManager). Enable MAX or AdMob below.";
            var kind = ModuleKind.Ads;
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
                RequestRemoveModule(kind);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (showRemove && !string.IsNullOrEmpty(blockReason))
                EditorGUILayout.HelpBox(blockReason, MessageType.Warning);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Mediation (optional)", EditorStyles.miniBoldLabel);
            DrawMediationToggle(MediationProvider.Max);
            DrawMediationToggle(MediationProvider.AdMob);

            EditorGUILayout.Space(4);
            DrawAdsProjectSetupTools(status != ModuleInstallStatus.NotInstalled);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        private void DrawAdsProjectSetupTools(bool adsReady)
        {
            EditorGUILayout.LabelField("Project setup", EditorStyles.miniBoldLabel);

            if (!adsReady)
            {
                EditorGUILayout.HelpBox("Import Ads module first to create JisSDKAdsSettings.", MessageType.None);
                return;
            }

            EditorGUILayout.HelpBox(JisSDKHubProjectSetup.GetAdsSettingsSummary(), MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create/Repair Ads Settings", GUILayout.Height(22)))
            {
                JisSDKHubProjectSetup.EnsureAdsSettingsAsset();
                JisSDKHubProjectSetup.EnsurePlatformSdkSetupStubs();
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(
                    "JIS SDK Hub",
                    "Ads settings and Android/iOS SDKSetup assets are ready.",
                    "OK");
                Repaint();
            }

            if (GUILayout.Button("Open Ads Settings", GUILayout.Height(22)))
            {
                JisSDKHubProjectSetup.OpenAdsSettingsAsset();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Apply Ads Settings To Scene", GUILayout.Height(22)))
            {
                var ok = JisSDKHubProjectSetup.TryApplyAdsSettingsToScene(out var message);
                EditorUtility.DisplayDialog("JIS SDK Hub", message, "OK");
                if (ok) Repaint();
            }
        }

        private void DrawMediationToggle(MediationProvider provider)
        {
            var label = JisSDKHubModules.GetMediationTitle(provider);
            var status = JisSDKHubModules.GetMediationStatusLabel(provider);
            var isOn = status == "On";
            var adsReady = JisSDKHubModules.GetStatus(ModuleKind.Ads) != ModuleInstallStatus.NotInstalled;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{label}: {status}", GUILayout.Width(180));

            GUI.enabled = adsReady && !isOn;
            if (GUILayout.Button($"Enable {label}", GUILayout.Height(20)))
                EnableMediation(provider);
            GUI.enabled = true;

            GUI.enabled = adsReady && (isOn || status == "Partial");
            if (GUILayout.Button($"Remove {label}", GUILayout.Height(20)))
                RequestDisableMediation(provider);
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            if (!adsReady)
                EditorGUILayout.HelpBox("Import Ads module first.", MessageType.None);
        }

        private void EnableMediation(MediationProvider provider)
        {
            if (JisSDKHubModules.GetStatus(ModuleKind.Ads) == ModuleInstallStatus.NotInstalled)
            {
                EditorUtility.DisplayDialog("JIS SDK Hub", "Import Ads module first.", "OK");
                return;
            }

            if (!_useEmbeddedPackages)
            {
                JisSDKHubManifest.UpdateJisSdkGitRevisions(_gitRevision);
                EnsureMediationRegistries(provider);
            }

            foreach (var (id, folder) in JisSDKHubModules.GetMediationPackages(provider))
                JisSDKHubManifest.AddDependency(id, ResolveSource(folder));

            foreach (var (id, ver) in JisSDKHubModules.GetMediationExternal(provider))
            {
                if (JisSDKHubManifest.HasDependency(id)) continue;
                JisSDKHubManifest.AddDependency(id, ver);
            }

            JisSDKHubDefines.SetDefine(JisSDKHubModules.GetMediationDefine(provider), true);
            RefreshPackages();
            EditorUtility.DisplayDialog("JIS SDK Hub", $"Enabled {JisSDKHubModules.GetMediationTitle(provider)}.", "OK");
            Repaint();
        }

        private void RequestRemoveModule(ModuleKind kind)
        {
            EditorApplication.delayCall += () => RemoveModule(kind);
        }

        private void RequestDisableMediation(MediationProvider provider)
        {
            EditorApplication.delayCall += () => RemoveMediation(provider);
        }

        private static string BuildRemoveDetails(ModuleKind kind)
        {
            var toRemove = JisSDKHubModules.GetPackageIdsToRemove(kind).ToList();
            var external = JisSDKHubModules.GetExternal(kind).Select(e => e.id).ToList();
            var defines = JisSDKHubModules.GetDefines(kind);

            var details = "Packages:\n" + string.Join("\n", toRemove.Concat(external).Select(id => "  • " + id));
            if (defines.Count > 0)
                details += "\n\nScripting defines:\n  • " + string.Join("\n  • ", defines);
            return details;
        }

        private static string BuildMediationRemoveDetails(MediationProvider provider)
        {
            var packages = JisSDKHubModules.GetMediationPackageIdsToRemove(provider).ToList();
            var define = JisSDKHubModules.GetMediationDefine(provider);
            return "Packages:\n" + string.Join("\n", packages.Select(id => "  • " + id)) +
                   $"\n\nScripting define:\n  • {define}";
        }

        private void RemoveModule(ModuleKind kind)
        {
            if (!JisSDKHubModules.CanRemove(kind, out var blockReason))
            {
                EditorUtility.DisplayDialog("JIS SDK Hub", blockReason, "OK");
                return;
            }

            if (!JisSDKHubDialogs.ConfirmRemove(JisSDKHubModules.GetTitle(kind), BuildRemoveDetails(kind)))
                return;

            ExecuteRemoveModule(kind);
        }

        private void RemoveMediation(MediationProvider provider)
        {
            if (!JisSDKHubModules.CanDisableMediation(provider, out var blockReason))
            {
                EditorUtility.DisplayDialog("JIS SDK Hub", blockReason, "OK");
                return;
            }

            var title = JisSDKHubModules.GetMediationTitle(provider);
            if (!JisSDKHubDialogs.ConfirmDisableMediation(title, BuildMediationRemoveDetails(provider)))
                return;

            foreach (var id in JisSDKHubModules.GetMediationPackageIdsToRemove(provider))
                JisSDKHubManifest.RemoveDependency(id);

            JisSDKHubDefines.SetDefine(JisSDKHubModules.GetMediationDefine(provider), false);
            RefreshPackages();
            EditorUtility.DisplayDialog("JIS SDK Hub", $"Removed {title}.", "OK");
            Repaint();
        }

        private void EnsureMediationRegistries(MediationProvider provider)
        {
            switch (provider)
            {
                case MediationProvider.Max:
                    JisSDKHubManifest.EnsureScopedRegistry(
                        "AppLovin MAX Unity", "https://unity.packages.applovin.com/",
                        new[] { "com.applovin.mediation.ads", "com.applovin.mediation.adapters", "com.applovin.mediation.dsp" });
                    break;
                case MediationProvider.AdMob:
                    JisSDKHubManifest.EnsureScopedRegistry(
                        "Game Package Registry by Google", "https://unityregistry-pa.googleapis.com",
                        new[] { "com.google" });
                    JisSDKHubManifest.EnsureOpenUpmScopes(
                        JisSDKHubModules.ExternalDependencyManagerId,
                        "com.google.ads.mobile");
                    break;
            }
        }

        private void Import(ModuleKind kind)
        {
            var migrated = JisSDKHubManifest.MigrateBrokenFileRefsToGit(_gitBaseUrl, _gitRevision);
            if (migrated > 0)
                Debug.Log($"[JIS SDK Hub] Migrated {migrated} file: dependency(ies) to Git URLs.");

            JisSDKHubModules.EnsureRegistriesForImport(kind);

            if (!_useEmbeddedPackages)
                JisSDKHubManifest.UpdateJisSdkGitRevisions(_gitRevision);

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

        private void ExecuteRemoveModule(ModuleKind kind)
        {
            var toRemove = JisSDKHubModules.GetPackageIdsToRemove(kind).ToList();
            var external = JisSDKHubModules.GetExternal(kind).Select(e => e.id).ToList();

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

        private static bool ShouldRemoveExternal(ModuleKind kind, string packageId) =>
            JisSDKHubManifest.HasDependency(packageId) && kind == ModuleKind.Iap;

        private static IEnumerable<(string id, string version)> GetExternalForImport(ModuleKind kind) =>
            JisSDKHubModules.GetExternal(kind);

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
            foreach (var symbol in JisSDKHubModules.GetDefines(kind))
                JisSDKHubDefines.SetDefine(symbol, add);
        }
    }
}
#endif
