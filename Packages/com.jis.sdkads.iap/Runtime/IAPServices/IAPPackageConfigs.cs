#if UNITY_IAP_ACTIVE
using System.Collections.Generic;
using JisSDKAds.Common;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Purchasing;

namespace JisSDKAds.IAP
{
    [CreateAssetMenu(fileName = "IAPPackageConfigs", menuName = "JIS SDK/IAP/Packages Config", order = 0)]
    public class IAPPackageConfigs : ScriptableObject
    {
        [field: SerializeField, TableList] public List<IAPPackage> Packages { get; set; } = new List<IAPPackage>();

        public IAPPackage FindPackage(string productId)
        {
            if (string.IsNullOrEmpty(productId) || Packages == null)
                return null;
            return Packages.Find(p => p != null && p.ProductID == productId);
        }

        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();
            if (Packages == null || Packages.Count == 0)
            {
                errors.Add("No IAP packages configured.");
                return false;
            }

            var seen = new HashSet<string>();
            for (var i = 0; i < Packages.Count; i++)
            {
                var pack = Packages[i];
                if (pack == null)
                {
                    errors.Add($"Package at index {i} is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(pack.ProductID))
                    errors.Add($"Package at index {i}: ProductID is empty.");
                else if (!seen.Add(pack.ProductID))
                    errors.Add($"Duplicate ProductID: {pack.ProductID}");

                if (pack.ProductKind == IapProductKind.RemoveAds && pack.ProductType != ProductType.NonConsumable)
                    errors.Add($"{pack.ProductID}: RemoveAds should use ProductType NonConsumable.");

                pack.SyncProductKindFromUnityType();
            }

            return errors.Count == 0;
        }
    }
}
#endif
