#if UNITY_ANDROID&&!UNITY_EDITOR&&!SOLARENGINE_BYTEDANCE&&!SOLARENGINE_BYTEDANCE_CLOUD&&!SOLARENGINE_BYTEDANCE_STARK&&!SE_DIS_RC
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
// using SolarEngine.Sample;
using AOT;

namespace SolarEngine
{
    public partial class SESDKRemoteConfig : MonoBehaviour
    {

        protected static AndroidJavaClass SeRemoteConfigAndroidSDK = new AndroidJavaClass("com.reyun.se.remote.config.unity.bridge.UnityAndroidSeRemoteConfigManager");

        private void SESDKSetRemoteDefaultConfig(Item[] defaultConfig)
        {

            if (defaultConfig == null)
            {
                return;
            }

            string defaultConfigJSONString = JsonConvert.SerializeObject(defaultConfig);

            SeRemoteConfigAndroidSDK.CallStatic("setRemoteDefaultConfig",defaultConfigJSONString);


        }


        private void SESDKSetRemoteConfigEventProperties(Dictionary<string, object> properties)
        {

            if (properties == null)
            {
                return;
            }

            string propertiesJSONString = JsonConvert.SerializeObject(properties);

            SeRemoteConfigAndroidSDK.CallStatic("setRemoteConfigEventProperties",propertiesJSONString);


        }

        private void SESDKSetRemoteConfigUserProperties(Dictionary<string, object> properties)
        {

            if (properties == null)
            {
                return;
            }

            string propertiesJSONString = JsonConvert.SerializeObject(properties);

            SeRemoteConfigAndroidSDK.CallStatic("setRemoteConfigUserProperties",propertiesJSONString);


        }


        private string SESDKFastFetchRemoteConfig(string key)
        {
            if (key == null)
            {
                return null;
            }


            string result = SeRemoteConfigAndroidSDK.CallStatic<string>("fastFetchRemoteConfig", key);
            return result;
        }

        private Dictionary<string, object> SESDKFastFetchAllRemoteConfig()
        {

            string result = SeRemoteConfigAndroidSDK.CallStatic<string>("fastFetchRemoteConfig");
            try{
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(result);
            }catch(Exception e){

            }
            return null;

        }

        private void SESDKAsyncFetchAllRemoteConfig()
        {

             SeRemoteConfigAndroidSDK.CallStatic("asyncFetchRemoteConfig",new OnRemoteConfigReceivedAllData());

        }

        private void SESDKAsyncFetchRemoteConfig(string key)
        {

             SeRemoteConfigAndroidSDK.CallStatic("asyncFetchRemoteConfig",key,new OnRemoteConfigReceivedData());

        }

    private sealed class OnRemoteConfigReceivedData: AndroidJavaProxy
    {
        public OnRemoteConfigReceivedData():base("com.reyun.se.remote.config.unity.bridge.OnRemoteConfigReceivedDataForUnity")
        {
            
        }
        public void onResultForUnity(String result)
        {
            OnFetchRemoteConfigCallback(result);
        }
    }



    private sealed class OnRemoteConfigReceivedAllData: AndroidJavaProxy
    {
        public OnRemoteConfigReceivedAllData():base("com.reyun.se.remote.config.unity.bridge.OnRemoteConfigReceivedAllDataForUnity")
        {
            
        }
        public void onResultForUnity(String result)
        {
            Dictionary<string, object> dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(result);
            if(dict != null){
                OnFetchAllRemoteConfigCallback(dict);
            }
        }
    }


    }
}
#endif