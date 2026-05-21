#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JisSDKAds.IAP.Editor
{
    public static class IapConfigValidatorMenu
    {
        public const string DefaultPackagesConfigPath = "Assets/JisSDKAds/Settings/IAP/IAPPackageConfigs.asset";

        [MenuItem("JIS SDK/IAP/Validate Packages Config", false, 50)]
        public static void ValidateFromMenu()
        {
            var config = AssetDatabase.LoadAssetAtPath<IAPPackageConfigs>(DefaultPackagesConfigPath);
            if (config == null)
            {
                var guids = AssetDatabase.FindAssets("t:IAPPackageConfigs");
                if (guids.Length > 0)
                {
                    config = AssetDatabase.LoadAssetAtPath<IAPPackageConfigs>(
                        AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }

            if (config == null)
            {
                Debug.LogWarning("[JIS SDK IAP] No IAPPackageConfigs asset found. Create one first.");
                return;
            }

            LogValidation(config);
        }

        public static bool LogValidation(IAPPackageConfigs config)
        {
            if (config == null)
            {
                Debug.LogError("[JIS SDK IAP] Config is null.");
                return false;
            }

            var ok = config.Validate(out var errors);
            if (ok)
            {
                Debug.Log($"[JIS SDK IAP] '{config.name}' is valid ({config.Packages?.Count ?? 0} product(s)).");
                return true;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[JIS SDK IAP] '{config.name}' validation failed:");
            foreach (var err in errors)
                sb.AppendLine("  - " + err);
            Debug.LogError(sb.ToString());
            return false;
        }
    }
}
#endif
