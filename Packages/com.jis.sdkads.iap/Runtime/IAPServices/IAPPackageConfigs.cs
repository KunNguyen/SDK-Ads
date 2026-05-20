#if UNITY_IAP_ACTIVE
using System.Collections.Generic;
using JisSDKAds.IAP;
using Sirenix.OdinInspector;
using UnityEngine;

namespace JisSDKAds.IAP
{
    [CreateAssetMenu(fileName = "IAPPackageConfigs", menuName = "JIS SDK/IAP/Packages Config", order = 0)]
    public class IAPPackageConfigs : ScriptableObject
    {
        [field: SerializeField, TableList] public List<IAPPackage> Packages { get; set; } = new List<IAPPackage>();
        
        
    }
}
#endif
