#if (UNITY_5 && (UNITY_IOS||UNITY_STANDALONE_OSX)) || (UNITY_IPHONE||UNITY_STANDALONE_OSX)&&!UNITY_EDITOR

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

            if (defaultConfig == null)
            {
                return;
            }

            string defaultConfigJSONString = JsonConvert.SerializeObject(defaultConfig);

            __iOSSESDKSetDefaultConfig(defaultConfigJSONString);


        }


        private void SESDKSetRemoteConfigEventProperties(Dictionary<string, object> properties)
        {

            if (properties == null)
            {
                return;
            }

            string propertiesJSONString = JsonConvert.SerializeObject(properties);

            __iOSSESDKSetRemoteConfigEventProperties(propertiesJSONString);


        }

        private void SESDKSetRemoteConfigUserProperties(Dictionary<string, object> properties)
        {

            if (properties == null)
            {
                return;
            }

            string propertiesJSONString = JsonConvert.SerializeObject(properties);

            __iOSSESDKSetRemoteConfigUserProperties(propertiesJSONString);


        }

       
        private string SESDKFastFetchRemoteConfig(string key)
        {
            if (key == null)
            {
                return null;
            }

            return __iOSSESDKFastFetchRemoteConfig(key);

        }

        private Dictionary<string, object> SESDKFastFetchAllRemoteConfig()
        {


            string result = __iOSSESDKFastFetchAllRemoteConfig();
            try{
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(result);
            }catch(Exception e){

            }
            return null;

        }

        private void SESDKAsyncFetchAllRemoteConfig()
        {

            __iOSSESDKAsyncFetchAllRemoteConfig(OnRemoteConfigiOSReceivedAllData);

        }

        private void SESDKAsyncFetchRemoteConfig(string key)
        {
            if (key == null)
            {
                return;
            }

            __iOSSESDKAsyncFetchRemoteConfig(key,OnRemoteConfigiOSReceivedData);

        }


     //回调函数，必须MonoPInvokeCallback并且是static
    [MonoPInvokeCallback(typeof(FetchRemoteConfigCallback))]
    private static void OnRemoteConfigiOSReceivedData(string result)
    {
        OnFetchRemoteConfigCallback(result);
    }

    //回调函数，必须MonoPInvokeCallback并且是static
    [MonoPInvokeCallback(typeof(FetchRemoteConfigCallback))]
    private static void OnRemoteConfigiOSReceivedAllData(string result)
    {

            Dictionary<string, object> dict = null;
            if (result != null) { 
                dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(result);
            }
            OnFetchAllRemoteConfigCallback(dict);

    }




#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
    [DllImport("SEUnityBridgeMacOSBundle")]
#endif
        private static extern void __iOSSESDKSetDefaultConfig(string config);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif
        private static extern void __iOSSESDKSetRemoteConfigEventProperties(string properties);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif
        private static extern void __iOSSESDKSetRemoteConfigUserProperties(string properties);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif
        private static extern string __iOSSESDKFastFetchRemoteConfig(string key);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif
        private static extern string __iOSSESDKFastFetchAllRemoteConfig();

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif
        private static extern void __iOSSESDKAsyncFetchRemoteConfig(string key, FetchRemoteConfigCallback callBack);

#if UNITY_IOS||UNITY_IPHONE
        [DllImport("__Internal")]
#elif UNITY_STANDALONE_OSX
        [DllImport("SEUnityBridgeMacOSBundle")]
#endif
        private static extern void __iOSSESDKAsyncFetchAllRemoteConfig(FetchRemoteConfigCallback callBack);


    }
}
#endif