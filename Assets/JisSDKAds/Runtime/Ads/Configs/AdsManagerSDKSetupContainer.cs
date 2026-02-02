using Sirenix.OdinInspector;
using UnityEngine;

namespace SDK
{
    [CreateAssetMenu(
        fileName = "AdsManagerSDKSetupContainer",
        menuName = "Tools/Ads/AdsManager SDKSetup Container",
        order = 10)]
    public class AdsManagerSDKSetupContainer : ScriptableObject
    {
        public SDKSetup android;
        public SDKSetup ios;

        private void SetAndroid(SDKSetup setup) => android = setup;
        private void SetIos(SDKSetup setup) => ios = setup;

        public void Setup()
        {
            if(android != null)
            {
                SetAndroid(android);
            }
            else
            {
                Debug.LogError("Android SDKSetup is not set");
            }
            
            
            if(ios != null)
            {
                SetIos(ios);
            }
            else
            {
                Debug.LogError("iOS SDKSetup is not set");
            }
        }
    }
}
