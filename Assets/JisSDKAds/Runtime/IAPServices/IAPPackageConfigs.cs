#if UNITY_IAP_ACTIVE
using System.Collections.Generic;
using SDK.IAP;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ABIMaxSDKAds.Scripts.IAPServices
{
    [CreateAssetMenu(fileName = "IAPPackageConfigs", menuName = "SDK/IAP/IAPPackageConfigs", order = 0)]
    public class IAPPackageConfigs : ScriptableObject
    {
        [field: SerializeField, TableList] public List<IAPPackage> Packages { get; set; } = new List<IAPPackage>();
        
        
    }
}
#endif
