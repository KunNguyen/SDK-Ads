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
        Editor
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
            ModuleKind.Editor
        };

        public static IEnumerable<(string id, string folder)> GetPackages(ModuleKind kind)
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

        /// <summary>Packages removed with the module (hub is kept when removing Firebase).</summary>
        public static IEnumerable<string> GetPackageIdsToRemove(ModuleKind kind)
        {
            switch (kind)
            {
                case ModuleKind.Firebase:
                    yield return "com.jis.sdkads.core";
                    yield return "com.jis.sdkads.common";
                    yield return "com.jis.sdkads.firebase";
                    break;
                default:
                    foreach (var (id, _) in GetPackages(kind))
                        yield return id;
                    break;
            }
        }

        public static IEnumerable<(string id, string version)> GetExternal(ModuleKind kind)
        {
            switch (kind)
            {
                case ModuleKind.Ads:
                    yield return ("com.applovin.mediation.ads", "8.6.3");
                    yield return ("com.google.ads.mobile", "9.4.0");
                    break;
                case ModuleKind.Iap:
                    yield return ("com.unity.purchasing", "5.0.4");
                    yield return ("com.unity.services.core", "1.16.0");
                    break;
            }
        }

        public static IReadOnlyList<string> GetDefines(ModuleKind kind)
        {
            switch (kind)
            {
                case ModuleKind.Ads:
                    return new[] { "UNITY_AD_MAX", "UNITY_AD_ADMOB" };
                case ModuleKind.Iap:
                    return new[] { "UNITY_IAP_ACTIVE" };
                case ModuleKind.AppReview:
                    return new[] { "GOOGLE_REVIEW" };
                case ModuleKind.AppsFlyer:
                    return new[] { "UNITY_APPSFLYER" };
                default:
                    return System.Array.Empty<string>();
            }
        }

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
            ModuleKind.Editor => "Editor Tools",
            _ => kind.ToString()
        };

        /// <summary>Modules that still need AppLovin / Google registries after removal.</summary>
        public static bool NeedsAdsRegistries() => IsInstalled(ModuleKind.Ads);
    }
}
#endif
