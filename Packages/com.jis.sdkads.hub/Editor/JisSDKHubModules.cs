#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;

namespace JisSDKAds.Hub
{
    internal enum ModuleKind
    {
        Firebase,
        Ads,
        Iap,
        AppReview,
        AppsFlyer,
        SolarEngine,
        Facebook,
        Notifications,
        Editor
    }

    internal enum MediationProvider
    {
        Max,
        AdMob
    }

    internal enum ModuleInstallStatus
    {
        NotInstalled,
        Partial,
        Installed
    }

    internal static class JisSDKHubModules
    {
        public static readonly ModuleKind[] All =
        {
            ModuleKind.Firebase,
            ModuleKind.Ads,
            ModuleKind.Iap,
            ModuleKind.AppReview,
            ModuleKind.AppsFlyer,
            ModuleKind.SolarEngine,
            ModuleKind.Facebook,
            ModuleKind.Notifications,
            ModuleKind.Editor
        };

        public static IEnumerable<(string id, string folder)> GetPackages(ModuleKind kind)
        {
            switch (kind)
            {
                case ModuleKind.Firebase:
                    yield return ("com.jis.sdkads.hub", "com.jis.sdkads.hub");
                    yield return ("com.jis.sdkads.odin", "com.jis.sdkads.odin");
                    yield return ("com.jis.sdkads.core", "com.jis.sdkads.core");
                    yield return ("com.jis.sdkads.common", "com.jis.sdkads.common");
                    yield return ("com.jis.sdkads.firebase", "com.jis.sdkads.firebase");
                    break;
                case ModuleKind.Ads:
                    yield return ("com.jis.sdkads.ads", "com.jis.sdkads.ads");
                    break;
                case ModuleKind.Iap:
                    // Pin common/core so Git UPM resolves the same revision as IAP (IapProductKind lives in common).
                    yield return ("com.jis.sdkads.core", "com.jis.sdkads.core");
                    yield return ("com.jis.sdkads.common", "com.jis.sdkads.common");
                    yield return ("com.jis.sdkads.iap", "com.jis.sdkads.iap");
                    break;
                case ModuleKind.AppReview:
                    yield return ("com.jis.sdkads.appreview", "com.jis.sdkads.appreview");
                    break;
                case ModuleKind.AppsFlyer:
                    yield return ("com.jis.sdkads.analytics.appsflyer", "com.jis.sdkads.analytics.appsflyer");
                    break;
                case ModuleKind.SolarEngine:
                    yield return ("com.jis.sdkads.common", "com.jis.sdkads.common");
                    yield return ("com.jis.sdkads.ads", "com.jis.sdkads.ads");
                    yield return ("com.jis.sdkads.analytics.solarengine", "com.jis.sdkads.analytics.solarengine");
                    break;
                case ModuleKind.Facebook:
                    yield return ("com.jis.sdkads.analytics.facebook", "com.jis.sdkads.analytics.facebook");
                    break;
                case ModuleKind.Notifications:
                    yield return ("com.jis.sdkads.common", "com.jis.sdkads.common");
                    yield return ("com.jis.sdkads.notifications", "com.jis.sdkads.notifications");
                    break;
                case ModuleKind.Editor:
                    yield return ("com.jis.sdkads.odin", "com.jis.sdkads.odin");
                    yield return ("com.jis.sdkads.editor", "com.jis.sdkads.editor");
                    break;
            }
        }

        public static IEnumerable<(string id, string folder)> GetMediationPackages(MediationProvider provider)
        {
            switch (provider)
            {
                case MediationProvider.Max:
                    yield return ("com.jis.sdkads.providers.max", "com.jis.sdkads.providers.max");
                    break;
                case MediationProvider.AdMob:
                    yield return ("com.jis.sdkads.providers.admob", "com.jis.sdkads.providers.admob");
                    break;
            }
        }

        public static IEnumerable<(string id, string version)> GetMediationExternal(MediationProvider provider)
        {
            switch (provider)
            {
                case MediationProvider.Max:
                    yield return ("com.applovin.mediation.ads", "8.6.3");
                    break;
                case MediationProvider.AdMob:
                    yield return ("com.google.ads.mobile", "9.4.0");
                    break;
            }
        }

        public static string GetMediationDefine(MediationProvider provider) => provider switch
        {
            MediationProvider.Max => JisSDKHubDefines.Max,
            MediationProvider.AdMob => JisSDKHubDefines.AdMob,
            _ => null
        };

        public static bool IsMediationEnabled(MediationProvider provider) =>
            JisSDKHubDefines.HasDefine(GetMediationDefine(provider));

        public static bool IsMediationInstalled(MediationProvider provider)
        {
            foreach (var (id, _) in GetMediationPackages(provider))
            {
                if (!JisSDKHubManifest.HasDependency(id))
                    return false;
            }
            return true;
        }

        public static string GetMediationStatusLabel(MediationProvider provider)
        {
            var defineOn = IsMediationEnabled(provider);
            var packagesOn = IsMediationInstalled(provider);
            if (defineOn && packagesOn) return "On";
            if (defineOn || packagesOn) return "Partial";
            return "Off";
        }

        /// <summary>Packages removed with the module (hub is kept when removing Firebase).</summary>
        public static IEnumerable<string> GetPackageIdsToRemove(ModuleKind kind)
        {
            switch (kind)
            {
                case ModuleKind.Firebase:
                    yield return "com.jis.sdkads.odin";
                    yield return "com.jis.sdkads.core";
                    yield return "com.jis.sdkads.common";
                    yield return "com.jis.sdkads.firebase";
                    break;
                case ModuleKind.Ads:
                    yield return "com.jis.sdkads.ads";
                    break;
                default:
                    foreach (var (id, _) in GetPackages(kind))
                        yield return id;
                    break;
            }
        }

        public static IEnumerable<string> GetMediationPackageIdsToRemove(MediationProvider provider)
        {
            foreach (var (id, _) in GetMediationPackages(provider))
                yield return id;
            foreach (var (id, _) in GetMediationExternal(provider))
                yield return id;
        }

        public const string ExternalDependencyManagerId = "com.google.external-dependency-manager";
        public const string ExternalDependencyManagerVersion = "1.2.187";

        public static IEnumerable<(string id, string version)> GetExternal(ModuleKind kind)
        {
            switch (kind)
            {
                case ModuleKind.Firebase:
                    if (!JisSDKHubManifest.HasDependency(ExternalDependencyManagerId) &&
                        !JisSDKHubManifest.HasEmbeddedEdmInAssets())
                    {
                        yield return (ExternalDependencyManagerId, ExternalDependencyManagerVersion);
                    }
                    break;
                case ModuleKind.Iap:
                    yield return ("com.unity.purchasing", "5.0.4");
                    yield return ("com.unity.services.core", "1.16.0");
                    break;
                case ModuleKind.AppReview:
                    JisSDKHubManifest.EnsureOpenUpmScopes(
                        "com.google.play.common",
                        "com.google.play.core",
                        "com.google.play.review");
                    yield return ("com.google.play.review", "1.8.4");
                    break;
                case ModuleKind.Notifications:
                    yield return ("com.unity.mobile.notifications", "2.4.3");
                    break;
            }
        }

        public static void EnsureRegistriesForImport(ModuleKind kind)
        {
            switch (kind)
            {
                case ModuleKind.Firebase:
                    JisSDKHubManifest.EnsureOpenUpmScopes(ExternalDependencyManagerId);
                    break;
            }
        }

        public static IReadOnlyList<string> GetDefines(ModuleKind kind) => kind switch
        {
            ModuleKind.Iap => new[] { "UNITY_IAP_ACTIVE" },
            ModuleKind.AppReview => new[] { "GOOGLE_REVIEW" },
            ModuleKind.AppsFlyer => new[] { "UNITY_APPSFLYER" },
            ModuleKind.SolarEngine => new[] { "UNITY_SOLAR_ENGINE" },
            _ => System.Array.Empty<string>()
        };

        public static ModuleInstallStatus GetStatus(ModuleKind kind)
        {
            var packages = GetPackages(kind).ToList();
            if (packages.Count == 0) return ModuleInstallStatus.NotInstalled;

            var present = packages.Count(p => JisSDKHubManifest.HasDependency(p.id));
            if (present == 0) return ModuleInstallStatus.NotInstalled;
            if (present >= packages.Count) return ModuleInstallStatus.Installed;
            return ModuleInstallStatus.Partial;
        }

        public static bool IsInstalled(ModuleKind kind) => GetStatus(kind) == ModuleInstallStatus.Installed;

        public static bool HasAnyPackage(ModuleKind kind) => GetStatus(kind) != ModuleInstallStatus.NotInstalled;

        public static string GetStatusLabel(ModuleKind kind)
        {
            var status = GetStatus(kind);
            var packages = GetPackages(kind).ToList();
            var present = packages.Count(p => JisSDKHubManifest.HasDependency(p.id));
            return status switch
            {
                ModuleInstallStatus.Installed => "Installed",
                ModuleInstallStatus.Partial => $"Partial ({present}/{packages.Count})",
                _ => "Not installed"
            };
        }

        public static bool CanRemove(ModuleKind kind, out string blockReason)
        {
            blockReason = null;
            if (!HasAnyPackage(kind))
            {
                blockReason = "Module is not in manifest.";
                return false;
            }

            if (kind == ModuleKind.Firebase)
            {
                foreach (var other in All)
                {
                    if (other == ModuleKind.Firebase) continue;
                    if (IsInstalled(other) || GetStatus(other) == ModuleInstallStatus.Partial)
                    {
                        blockReason = $"Remove {GetTitle(other)} (and other modules) first — they depend on Core/Common/Firebase.";
                        return false;
                    }
                }
            }

            if (kind == ModuleKind.Ads)
            {
                if (IsMediationEnabled(MediationProvider.Max) || IsMediationEnabled(MediationProvider.AdMob))
                {
                    blockReason = "Disable MAX and AdMob below before removing Ads runtime.";
                    return false;
                }
            }

            return true;
        }

        public static bool CanDisableMediation(MediationProvider provider, out string blockReason)
        {
            blockReason = null;
            if (GetStatus(ModuleKind.Ads) == ModuleInstallStatus.NotInstalled)
            {
                blockReason = "Import Ads module first.";
                return false;
            }
            return true;
        }

        public static string GetTitle(ModuleKind kind) => kind switch
        {
            ModuleKind.Firebase => "Firebase",
            ModuleKind.Ads => "Ads",
            ModuleKind.Iap => "IAP",
            ModuleKind.AppReview => "App Review",
            ModuleKind.AppsFlyer => "AppsFlyer",
            ModuleKind.SolarEngine => "SolarEngine",
            ModuleKind.Facebook => "Facebook",
            ModuleKind.Notifications => "Local Notifications",
            ModuleKind.Editor => "Editor Tools",
            _ => kind.ToString()
        };

        public static string GetMediationTitle(MediationProvider provider) => provider switch
        {
            MediationProvider.Max => "AppLovin MAX",
            MediationProvider.AdMob => "Google AdMob",
            _ => provider.ToString()
        };
    }
}
#endif
