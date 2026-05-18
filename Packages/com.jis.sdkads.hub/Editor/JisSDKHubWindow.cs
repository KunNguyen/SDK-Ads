#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
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
        private bool _useEmbeddedPackages = true;

        [MenuItem("JIS SDK/Hub")]
        public static void Open() => GetWindow<JisSDKHubWindow>("JIS SDK Hub");

        private void OnEnable()
        {
            _gitBaseUrl = EditorPrefs.GetString(PrefsGitBaseUrl, DefaultGitBaseUrl);
            _gitRevision = EditorPrefs.GetString(PrefsGitRevision, DefaultGitRevision);
            _useEmbeddedPackages = Directory.Exists(
                Path.Combine(JisSDKHubManifest.ManifestPath, "..", "com.jis.sdkads.core"));
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.LabelField("JIS SDK Hub", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Import SDK modules into this Unity project.\n" +
                "• Install Firebase from Google before using Firebase module.\n" +
                "• Dev repo: embedded packages under Packages/com.jis.sdkads.*\n" +
                "• Game projects: set Git URL and disable embedded mode.",
                MessageType.Info);

            _useEmbeddedPackages = EditorGUILayout.Toggle("Use embedded packages (SDK-Ads dev repo)", _useEmbeddedPackages);
            if (!_useEmbeddedPackages)
            {
                _gitBaseUrl = EditorGUILayout.TextField("Git UPM base URL", _gitBaseUrl);
                _gitRevision = EditorGUILayout.TextField("Git revision (branch or tag)", _gitRevision);
                EditorGUILayout.HelpBox(
                    "Use main until a Git tag exists (e.g. 4.0.0). Hub appends #revision to package URLs.\n" +
                    "Package version in package.json is still 4.0.0 — only the Git ref changes.",
                    MessageType.None);
                if (GUILayout.Button("Save Git URL & revision"))
                {
                    EditorPrefs.SetString(PrefsGitBaseUrl, _gitBaseUrl);
                    EditorPrefs.SetString(PrefsGitRevision, _gitRevision);
                }

                if (GUILayout.Button("Fix com.jis.sdkads.* revisions in manifest.json"))
                {
                    var n = JisSDKHubManifest.UpdateJisSdkGitRevisions(_gitRevision);
                    AssetDatabase.Refresh();
                    EditorUtility.DisplayDialog("JIS SDK Hub",
                        n > 0
                            ? $"Updated {n} package(s) to #{_gitRevision.Trim().TrimStart('#')}."
                            : "No com.jis.sdkads.* Git entries found to update.",
                        "OK");
                }
            }

            EditorGUILayout.Space(6);
            DrawModule("Firebase (required)", "com.jis.sdkads.core + common + firebase + hub", ModuleKind.Firebase);
            DrawModule("Ads", "Providers MAX/AdMob + full ads runtime", ModuleKind.Ads);
            DrawModule("IAP", "Unity Purchasing", ModuleKind.Iap);
            DrawModule("App Review", "Android only — Google Play Review", ModuleKind.AppReview);
            DrawModule("AppsFlyer", "Optional", ModuleKind.AppsFlyer);
            DrawModule("SolarEngine", "Optional", ModuleKind.SolarEngine);
            DrawModule("Facebook", "Optional", ModuleKind.Facebook);
            DrawModule("Editor Tools", "SDK setup & build", ModuleKind.Editor);
            EditorGUILayout.EndScrollView();
        }

        private enum ModuleKind
        {
            Firebase, Ads, Iap, AppReview, AppsFlyer, SolarEngine, Facebook, Editor
        }

        private void DrawModule(string title, string desc, ModuleKind kind)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(desc, EditorStyles.miniLabel);
            if (GUILayout.Button($"Import {title}"))
                Import(kind);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        private void Import(ModuleKind kind)
        {
            if (!_useEmbeddedPackages)
            {
                JisSDKHubManifest.UpdateJisSdkGitRevisions(_gitRevision);
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

            foreach (var (id, folder) in GetPackages(kind))
                JisSDKHubManifest.AddDependency(id, ResolveSource(folder));

            foreach (var (id, ver) in GetExternal(kind))
                JisSDKHubManifest.AddDependency(id, ver);

            ApplyDefines(kind);
            AssetDatabase.Refresh();

            if (kind == ModuleKind.Ads)
            {
                EditorApplication.delayCall += () =>
                {
                    JisSDKHubProjectSetup.EnsureAdsSettingsAsset();
                    JisSDKHubProjectSetup.EnsurePlatformSdkSetupStubs();
                };
            }

            EditorUtility.DisplayDialog("JIS SDK Hub", $"Imported {kind}. Check Package Manager and Console.", "OK");
        }

        private string ResolveSource(string folder)
        {
            if (_useEmbeddedPackages)
                return $"file:{folder}";
            var revision = string.IsNullOrWhiteSpace(_gitRevision) ? DefaultGitRevision : _gitRevision.Trim().TrimStart('#');
            return $"{_gitBaseUrl.TrimEnd('/')}?path=Packages/{folder}#{revision}";
        }

        private static IEnumerable<(string id, string folder)> GetPackages(ModuleKind kind)
        {
            switch (kind)
            {
                case ModuleKind.Firebase:
                    yield return ("com.jis.sdkads.hub", "com.jis.sdkads.hub");
                    yield return ("com.jis.sdkads.core", "com.jis.sdkads.core");
                    yield return ("com.jis.sdkads.common", "com.jis.sdkads.common");
                    yield return ("com.jis.sdkads.firebase", "com.jis.sdkads.firebase");
                    break;
                case ModuleKind.Ads:
                    yield return ("com.jis.sdkads.providers.max", "com.jis.sdkads.providers.max");
                    yield return ("com.jis.sdkads.providers.admob", "com.jis.sdkads.providers.admob");
                    yield return ("com.jis.sdkads.ads", "com.jis.sdkads.ads");
                    break;
                case ModuleKind.Iap:
                    yield return ("com.jis.sdkads.iap", "com.jis.sdkads.iap");
                    break;
                case ModuleKind.AppReview:
                    yield return ("com.jis.sdkads.appreview", "com.jis.sdkads.appreview");
                    break;
                case ModuleKind.AppsFlyer:
                    yield return ("com.jis.sdkads.analytics.appsflyer", "com.jis.sdkads.analytics.appsflyer");
                    break;
                case ModuleKind.SolarEngine:
                    yield return ("com.jis.sdkads.analytics.solarengine", "com.jis.sdkads.analytics.solarengine");
                    break;
                case ModuleKind.Facebook:
                    yield return ("com.jis.sdkads.analytics.facebook", "com.jis.sdkads.analytics.facebook");
                    break;
                case ModuleKind.Editor:
                    yield return ("com.jis.sdkads.editor", "com.jis.sdkads.editor");
                    break;
            }
        }

        private static IEnumerable<(string id, string version)> GetExternal(ModuleKind kind)
        {
            switch (kind)
            {
                case ModuleKind.Ads:
                    yield return ("com.applovin.mediation.ads", "8.6.3");
                    // Optional: use Assets/GoogleMobileAds in dev repo instead of UPM
                    if (!JisSDKHubManifest.HasDependency("com.google.ads.mobile"))
                        yield return ("com.google.ads.mobile", "9.4.0");
                    break;
                case ModuleKind.Iap:
                    yield return ("com.unity.purchasing", "5.0.4");
                    yield return ("com.unity.services.core", "1.16.0");
                    break;
            }
        }

        private static void ApplyDefines(ModuleKind kind)
        {
            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            var set = new HashSet<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(group)
                .Split(';').Where(s => !string.IsNullOrEmpty(s)));

            switch (kind)
            {
                case ModuleKind.Ads:
                    set.Add("UNITY_AD_MAX");
                    set.Add("UNITY_AD_ADMOB");
                    break;
                case ModuleKind.Iap:
                    set.Add("UNITY_IAP_ACTIVE");
                    break;
                case ModuleKind.AppReview:
                    set.Add("GOOGLE_REVIEW");
                    break;
                case ModuleKind.AppsFlyer:
                    set.Add("UNITY_APPSFLYER");
                    break;
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", set));
        }
    }
}
#endif
