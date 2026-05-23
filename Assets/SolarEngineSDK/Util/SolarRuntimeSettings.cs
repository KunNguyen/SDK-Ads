// Assets/Scripts/SolarEngine/RuntimeSettings.cs
using UnityEngine;

namespace SolarEngine
{
    [CreateAssetMenu(fileName = "SERuntimeSettings", menuName = "SolarEngine/Runtime Settings")]
    public class SolarRuntimeSettings : ScriptableObject
    {
        [HideInInspector]
        public  bool isCN;
        [HideInInspector]
        public bool isOversea;
        // [HideInInspector]
        // public bool isUseOaid;
        [HideInInspector]
        public bool isUseODMInfo;

        // public bool isUseAndroidRc;
        // public bool isUseiOSRc;
        //
        // public bool isUseMininRc;
        // public bool isUseMacOSRc;
        // public bool isUseOpenHarmonyRc;
        //
        
        // public bool   customDomainEnable;
        // public string receiverDomain;
        // public string ruleDomain;
        // public string receiverTcpHost;
        // public string ruleTcpHost;
        // public string gatewayTcpHost;
        
        // 单例访问（可选）
        private static SolarRuntimeSettings _instance;
        public static SolarRuntimeSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<SolarRuntimeSettings>("SERuntimeSettings");
              
                return _instance;
            }
        }

        public static bool InstanceNotNull
        {
            get
            {
                return  SolarRuntimeSettings.Instance!=null;
                
            }
        }

      
    }
}