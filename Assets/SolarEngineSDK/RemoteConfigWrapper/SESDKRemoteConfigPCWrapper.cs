#if((UNITY_EDITOR||UNITY_STANDALONE_WIN) &&!SE_DEV&&!SOLORENGINE_DEVELOPEREDITOR)||(SE_DIS_RC&&UNITY_OPENHARMONY)||(SE_DIS_RC&&SE_MINI_DIS_RC)||(SE_DIS_RC&&UNITY_IOS)||(SE_MINI_DIS_RC&&UNITY_WEBGL)||(SE_DIS_RC&&(UNITY_ANDROID&&!SOLARENGINE_BYTEDANCE&&!SOLARENGINE_BYTEDANCE_CLOUD&&!SOLARENGINE_BYTEDANCE_STARK))||(SE_MINI_DIS_RC&&(UNITY_ANDROID&&(SOLARENGINE_BYTEDANCE||SOLARENGINE_BYTEDANCE_CLOUD||SOLARENGINE_BYTEDANCE_STARK)))
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using AOT;

namespace SolarEngine
{
    public partial class SESDKRemoteConfig : MonoBehaviour
    {

        private void SESDKSetRemoteDefaultConfig(Item[] defaultConfig)
        {
            Debug.Log("Unity Editor: SESDKSetRemoteConfigEventProperties");
            
        }


        private void SESDKSetRemoteConfigEventProperties(Dictionary<string, object> properties)
        {

            Debug.Log("Unity Editor: SESDKSetRemoteConfigEventProperties");
        }

        private void SESDKSetRemoteConfigUserProperties(Dictionary<string, object> properties)
        {

            Debug.Log("Unity Editor: SESDKSetRemoteConfigUserProperties");

        }


        private string SESDKFastFetchRemoteConfig(string key)
        {
            Debug.Log("Unity Editor: SESDKFastFetchRemoteConfig");
            return  null;
        }

        private Dictionary<string, object> SESDKFastFetchAllRemoteConfig()
        {

            Debug.Log("Unity Editor: SESDKFastFetchAllRemoteConfig");
            return null;

        }

        private void SESDKAsyncFetchAllRemoteConfig()
        {

            Debug.Log("Unity Editor: SESDKAsyncFetchAllRemoteConfig");

        }

        private void SESDKAsyncFetchRemoteConfig(string key)
        {

            Debug.Log("Unity Editor: SESDKAsyncFetchRemoteConfig ");
        }

    }


}
#endif