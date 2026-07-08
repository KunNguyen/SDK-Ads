#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace JisSDKAds.IAP.Editor
{
    /// <summary>
    /// Fails the build if IAP is enabled (this asmdef only compiles under UNITY_IAP_ACTIVE)
    /// but the configured <see cref="IAPPackageConfigs"/> is missing or invalid — duplicate
    /// or empty product ids, RemoveAds misconfigured, missing GooglePlayTangle/AppleTangle
    /// for receipt validation, etc. Without this gate an invalid config only surfaces at
    /// runtime, or worse in front of a store reviewer, instead of at build time where it's
    /// cheap to fix.
    /// </summary>
    public class IapBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var config = FindConfig();
            if (config == null)
            {
                throw new BuildFailedException(
                    "[JIS SDK IAP] UNITY_IAP_ACTIVE is enabled but no IAPPackageConfigs asset was " +
                    "found. Create one via JIS SDK > IAP > Create Packages Config before building.");
            }

            if (!IapConfigValidatorMenu.LogValidation(config))
            {
                throw new BuildFailedException(
                    $"[JIS SDK IAP] '{config.name}' failed validation — see the Console above for " +
                    "details. Fix the IAP config before building.");
            }
        }

        static IAPPackageConfigs FindConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<IAPPackageConfigs>(
                IapConfigValidatorMenu.DefaultPackagesConfigPath);
            if (config != null)
                return config;

            var guids = AssetDatabase.FindAssets("t:IAPPackageConfigs");
            return guids.Length > 0
                ? AssetDatabase.LoadAssetAtPath<IAPPackageConfigs>(AssetDatabase.GUIDToAssetPath(guids[0]))
                : null;
        }
    }
}
#endif
